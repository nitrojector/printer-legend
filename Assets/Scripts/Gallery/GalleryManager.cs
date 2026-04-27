using System;
using System.Collections.Generic;
using System.IO;
using Data;
using UnityEngine;
using Utility;

namespace Gallery
{
	/// <summary>
	/// Manages gallery entries and handles buffered Texture2D loading.
	/// All image paths are relative to Application.persistentDataPath unless noted.
	/// Creation images live under creations/, reference images under references/.
	/// </summary>
	public static class GalleryManager
	{
		/// <summary>Prefix that marks a reference image path as a Unity Resources path.</summary>
		public const string InternalPrefix = "internal:";

		/// <summary>Maximum number of disk-loaded textures kept in memory at once.</summary>
		public const int ImageBufferSize = 20;

		private static readonly GalleryImageBuffer ImageBuffer = new(ImageBufferSize);
		private static readonly GalleryImageBuffer RefImageBuffer = new(ImageBufferSize);

		// ── Entry access ──────────────────────────────────────────────────────

		/// <summary>
		/// Returns all valid gallery entries from the on-disk config.
		/// Entries whose creation image file is missing or unreadable are skipped with an error log.
		/// </summary>
		public static IReadOnlyList<GalleryEntry> GetEntries()
		{
			var all = GallerySave.Instance.Entries;
			var valid = new List<GalleryEntry>(all.Count);
			foreach (var entry in all)
			{
				var fullPath = ToFullPath(entry.ImagePath);
				if (!File.Exists(fullPath))
				{
					Logr.Error($"Gallery: creation image missing at '{fullPath}', skipping entry.");
					continue;
				}
				valid.Add(entry);
			}
			return valid;
		}

		// ── Save / Remove ─────────────────────────────────────────────────────

		/// <summary>
		/// Encodes <paramref name="creation"/> as PNG, saves it to creations/ named by UTC datetime,
		/// creates a <see cref="GalleryEntry"/>, appends it to the config, and writes to disk immediately.
		/// </summary>
		/// <param name="creation">Texture drawn by the user. Must be readable.</param>
		/// <param name="referencePath">
		/// Either an internal Resources path ("internal:PrintRefs/foo") or a relative path under
		/// references/ (e.g. "references/20240101_120000__cat.jpg").
		/// </param>
		/// <param name="similarityScore">Score in [0..1].</param>
		/// <param name="resetCount">Number of resets before this print.</param>
		/// <param name="printDuration">Duration of the final print pass in seconds.</param>
		/// <returns>The newly created entry, or null on failure.</returns>
		public static GalleryEntry SaveEntry(
			Texture2D creation,
			string referencePath,
			float similarityScore,
			int resetCount,
			float printDuration)
		{
			if (creation == null)
			{
				Logr.Error("Gallery: cannot save a null texture.");
				return null;
			}

			byte[] png = creation.EncodeToPNG();
			if (png == null)
			{
				Logr.Error("Gallery: EncodeToPNG failed — texture may not be readable.");
				return null;
			}

			var dir = GallerySave.Instance.ImageSaveDirectory;
			Directory.CreateDirectory(dir);

			var filename = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".png";
			var fullPath = Path.Combine(dir, filename);

			try
			{
				File.WriteAllBytes(fullPath, png);
			}
			catch (Exception e)
			{
				Logr.Error($"Gallery: failed to write creation image to '{fullPath}': {e.Message}");
				return null;
			}

			var entry = new GalleryEntry
			{
				ImagePath          = "creations/" + filename,
				ReferenceImagePath = referencePath ?? string.Empty,
				Date               = DateTime.UtcNow,
				SimilarityScore    = similarityScore,
				ResetCount         = resetCount,
				PrintDuration      = printDuration,
			};

			GallerySave.Instance.Entries.Add(entry);
			GallerySave.Save();
			
			Logr.Info($"Gallery: saved new entry with creation at '{fullPath}' and reference '{referencePath}'");
			
			return entry;
		}

		/// <summary>
		/// Removes <paramref name="entry"/> from the config and deletes its creation image from disk.
		/// The reference image file is NOT touched.
		/// Returns false if the entry was not found in the config.
		/// </summary>
		public static bool RemoveEntry(GalleryEntry entry)
		{
			var entries = GallerySave.Instance.Entries;
			if (!entries.Remove(entry)) return false;

			var fullPath = ToFullPath(entry.ImagePath);
			ImageBuffer.Evict(fullPath);

			if (File.Exists(fullPath))
			{
				try { File.Delete(fullPath); }
				catch (Exception e) { Logr.Error($"Gallery: failed to delete '{fullPath}': {e.Message}"); }
			}

			GallerySave.Save();
			return true;
		}

		// ── Image loading ─────────────────────────────────────────────────────

		/// <summary>
		/// Returns the creation image for <paramref name="entry"/>, loading from disk if not cached.
		/// Returns null if the file is missing or unreadable.
		/// </summary>
		public static Texture2D LoadImage(GalleryEntry entry)
		{
			if (string.IsNullOrEmpty(entry.ImagePath)) return null;
			var fullPath = ToFullPath(entry.ImagePath);
			return ImageBuffer.Get(fullPath) ?? LoadAndCache(fullPath, ImageBuffer);
		}

		/// <summary>
		/// Returns the reference image for <paramref name="entry"/>.
		/// Internal paths are loaded via Resources.Load; external paths are disk-loaded and buffered.
		/// Returns null if missing or unreadable.
		/// </summary>
		public static Texture2D LoadReferenceImage(GalleryEntry entry)
		{
			var path = entry.ReferenceImagePath;
			if (string.IsNullOrEmpty(path)) return null;

			if (IsInternalReference(path))
			{
				var resourcePath = path.Substring(InternalPrefix.Length);
				var sprite = Resources.Load<Sprite>(resourcePath);
				if (sprite == null)
					Logr.Warn($"Gallery: internal reference not found at Resources/{resourcePath}");
				return sprite != null ? sprite.texture : null;
			}

			var fullPath = ToFullPath(path);
			return RefImageBuffer.Get(fullPath) ?? LoadAndCache(fullPath, RefImageBuffer);
		}

		/// <summary>Drops the creation image for this entry from the in-memory buffer.</summary>
		public static void UnloadImage(GalleryEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.ImagePath))
				ImageBuffer.Evict(ToFullPath(entry.ImagePath));
		}

		/// <summary>Drops the external reference image for this entry from the in-memory buffer.</summary>
		public static void UnloadReferenceImage(GalleryEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.ReferenceImagePath) && !IsInternalReference(entry.ReferenceImagePath))
				RefImageBuffer.Evict(ToFullPath(entry.ReferenceImagePath));
		}

		/// <summary>Destroys all buffered textures and clears both caches.</summary>
		public static void UnloadAllImages()
		{
			ImageBuffer.Clear();
			RefImageBuffer.Clear();
		}

		// ── Path helpers ──────────────────────────────────────────────────────

		public static bool IsInternalReference(string path) =>
			path != null && path.StartsWith(InternalPrefix, StringComparison.Ordinal);

		public static string MakeInternalPath(string resourcePath) =>
			InternalPrefix + resourcePath;

		// ── Internals ─────────────────────────────────────────────────────────

		private static string ToFullPath(string relativePath) =>
			Path.Combine(Application.persistentDataPath, relativePath);

		private static Texture2D LoadAndCache(string fullPath, GalleryImageBuffer buffer)
		{
			if (!File.Exists(fullPath))
			{
				Logr.Warn($"Gallery: image not found at '{fullPath}'");
				return null;
			}

			byte[] bytes;
			try { bytes = File.ReadAllBytes(fullPath); }
			catch (Exception e)
			{
				Logr.Error($"Gallery: failed to read '{fullPath}': {e.Message}");
				return null;
			}

			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (tex.LoadImage(bytes))
			{
				buffer.Put(fullPath, tex);
				return tex;
			}

			UnityEngine.Object.Destroy(tex);
			Logr.Error($"Gallery: failed to decode image at '{fullPath}'");
			return null;
		}
	}

	/// <summary>
	/// Fixed-capacity LRU cache for Texture2D instances keyed by resolved file path.
	/// The least recently used entry is evicted (and its texture destroyed) when capacity is exceeded.
	/// </summary>
	internal sealed class GalleryImageBuffer
	{
		private readonly int _capacity;
		private readonly Dictionary<string, (Texture2D tex, LinkedListNode<string> node)> _cache = new();
		private readonly LinkedList<string> _lru = new(); // front = MRU, back = LRU

		public GalleryImageBuffer(int capacity) => _capacity = capacity;

		public Texture2D Get(string key)
		{
			if (!_cache.TryGetValue(key, out var entry)) return null;
			_lru.Remove(entry.node);
			var node = _lru.AddFirst(key);
			_cache[key] = (entry.tex, node);
			return entry.tex;
		}

		public void Put(string key, Texture2D tex)
		{
			if (_cache.TryGetValue(key, out var existing))
			{
				_lru.Remove(existing.node);
				if (existing.tex != null && existing.tex != tex)
					UnityEngine.Object.Destroy(existing.tex);
			}
			else if (_cache.Count >= _capacity)
			{
				EvictLru();
			}

			var node = _lru.AddFirst(key);
			_cache[key] = (tex, node);
		}

		public void Evict(string key)
		{
			if (!_cache.Remove(key, out var entry)) return;
			_lru.Remove(entry.node);
			if (entry.tex != null) UnityEngine.Object.Destroy(entry.tex);
		}

		public void Clear()
		{
			foreach (var (tex, _) in _cache.Values)
				if (tex != null) UnityEngine.Object.Destroy(tex);
			_cache.Clear();
			_lru.Clear();
		}

		private void EvictLru()
		{
			if (_lru.Last == null) return;
			var key = _lru.Last.Value;
			_lru.RemoveLast();
			if (_cache.Remove(key, out var entry) && entry.tex != null)
				UnityEngine.Object.Destroy(entry.tex);
		}
	}
}
