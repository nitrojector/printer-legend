using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Utility;
using Data;

namespace Gallery
{
	/// <summary>
	/// Manages gallery entries and handles buffered Texture2D loading.
	/// Wraps <see cref="GallerySave"/> for persistence.
	/// </summary>
	public class GalleryManager
	{
		/// <summary>Prefix that marks a reference image path as a Unity Resources path.</summary>
		public const string InternalPrefix = "internal:";

		/// <summary>Maximum number of disk-loaded textures kept in memory at once.</summary>
		public const int ImageBufferSize = 20;

		private static GalleryManager _instance;
		public static GalleryManager Instance => _instance ??= new GalleryManager();

		private readonly GalleryImageBuffer _imageBuffer = new(ImageBufferSize);
		private readonly GalleryImageBuffer _refImageBuffer = new(ImageBufferSize);

		private List<GalleryEntry> Entries => GallerySave.Instance.Entries;

		/// <summary>Number of entries currently in the gallery.</summary>
		public int Count => Entries.Count;

		/// <summary>Read-only view of all gallery entries.</summary>
		public IReadOnlyList<GalleryEntry> AllEntries => Entries;

		private GalleryManager() { }

		// ── Entry operations ──────────────────────────────────────────────────

		/// <summary>Appends a new entry and saves the gallery.</summary>
		public void AddEntry(GalleryEntry entry)
		{
			Entries.Add(entry);
			GallerySave.Save();
		}

		/// <summary>
		/// Removes the entry at the given index, unloads its cached images, and saves.
		/// Returns false if index is out of range.
		/// </summary>
		public bool RemoveAt(int index)
		{
			if (index < 0 || index >= Entries.Count) return false;

			var entry = Entries[index];
			EvictEntry(entry);
			Entries.RemoveAt(index);
			GallerySave.Save();
			return true;
		}

		/// <summary>
		/// Removes a specific entry by reference, unloads its cached images, and saves.
		/// Returns false if the entry is not in the gallery.
		/// </summary>
		public bool Remove(GalleryEntry entry)
		{
			int index = Entries.IndexOf(entry);
			return index >= 0 && RemoveAt(index);
		}

		/// <summary>Returns the entry at the given index.</summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public GalleryEntry GetEntry(int index)
		{
			if (index < 0 || index >= Entries.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			return Entries[index];
		}

		// ── Image loading ─────────────────────────────────────────────────────

		/// <summary>
		/// Returns the print image for the given entry, loading it from disk if not cached.
		/// The oldest loaded texture is evicted when the buffer is full.
		/// Returns null if the file is missing or unreadable.
		/// </summary>
		public Texture2D LoadImage(GalleryEntry entry)
		{
			if (string.IsNullOrEmpty(entry.ImagePath)) return null;

			var fullPath = ToFullPath(entry.ImagePath);
			return _imageBuffer.Get(fullPath) ?? LoadAndCache(fullPath, _imageBuffer);
		}

		/// <inheritdoc cref="LoadImage(GalleryEntry)"/>
		public Texture2D LoadImage(int index) => LoadImage(GetEntry(index));

		/// <summary>
		/// Returns the reference image for the given entry.
		/// Internal paths ("internal:…") are loaded via Resources.Load and are Unity-managed.
		/// External relative paths are disk-loaded and buffered like print images.
		/// Returns null if missing or unreadable.
		/// </summary>
		public Texture2D LoadReferenceImage(GalleryEntry entry)
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
			return _refImageBuffer.Get(fullPath) ?? LoadAndCache(fullPath, _refImageBuffer);
		}

		/// <summary>Explicitly drops the print image for this entry from the buffer.</summary>
		public void UnloadImage(GalleryEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.ImagePath))
				_imageBuffer.Evict(ToFullPath(entry.ImagePath));
		}

		/// <inheritdoc cref="UnloadImage(GalleryEntry)"/>
		public void UnloadImage(int index) => UnloadImage(GetEntry(index));

		/// <summary>
		/// Explicitly drops the external reference image for this entry from the buffer.
		/// No-op for internal references (Unity-managed).
		/// </summary>
		public void UnloadReferenceImage(GalleryEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.ReferenceImagePath) && !IsInternalReference(entry.ReferenceImagePath))
				_refImageBuffer.Evict(ToFullPath(entry.ReferenceImagePath));
		}

		/// <summary>Destroys all buffered textures and clears both caches.</summary>
		public void UnloadAllImages()
		{
			_imageBuffer.Clear();
			_refImageBuffer.Clear();
		}

		// ── Path helpers ──────────────────────────────────────────────────────

		/// <summary>Returns true if the given path is an internal (Resources) reference.</summary>
		public static bool IsInternalReference(string path) =>
			path != null && path.StartsWith(InternalPrefix, StringComparison.Ordinal);

		/// <summary>
		/// Builds an internal reference path from a Resources-relative path.
		/// Example: "PrintRefs/foo" → "internal:PrintRefs/foo"
		/// </summary>
		public static string MakeInternalPath(string resourcePath) =>
			InternalPrefix + resourcePath;

		/// <summary>
		/// Converts an absolute path to a forward-slash relative path under
		/// Application.persistentDataPath, ready to store in a GalleryEntry.
		/// Paths already outside persistentDataPath are returned as-is (with slashes normalised).
		/// </summary>
		public static string MakeRelativePath(string absolutePath)
		{
			var basePath = Application.persistentDataPath;
			if (absolutePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
				return absolutePath.Substring(basePath.Length).TrimStart('/', '\\').Replace('\\', '/');
			return absolutePath.Replace('\\', '/');
		}

		// ── Internals ─────────────────────────────────────────────────────────

		private static string ToFullPath(string relativePath) =>
			Path.Combine(Application.persistentDataPath, relativePath);

		private void EvictEntry(GalleryEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.ImagePath))
				_imageBuffer.Evict(ToFullPath(entry.ImagePath));

			if (!string.IsNullOrEmpty(entry.ReferenceImagePath) && !IsInternalReference(entry.ReferenceImagePath))
				_refImageBuffer.Evict(ToFullPath(entry.ReferenceImagePath));
		}

		private static Texture2D LoadAndCache(string fullPath, GalleryImageBuffer buffer)
		{
			var tex = LoadTextureFromDisk(fullPath);
			if (tex != null) buffer.Put(fullPath, tex);
			return tex;
		}

		private static Texture2D LoadTextureFromDisk(string fullPath)
		{
			if (!File.Exists(fullPath))
			{
				Logr.Warn($"Gallery: image not found at '{fullPath}'");
				return null;
			}

			byte[] bytes;
			try
			{
				bytes = File.ReadAllBytes(fullPath);
			}
			catch (Exception e)
			{
				Logr.Error($"Gallery: failed to read '{fullPath}': {e.Message}");
				return null;
			}

			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (tex.LoadImage(bytes)) return tex;

			UnityEngine.Object.Destroy(tex);
			Logr.Error($"Gallery: failed to decode image at '{fullPath}'");
			return null;
		}
	}

	/// <summary>
	/// Fixed-capacity LRU cache for Texture2D instances, keyed by resolved file path.
	/// The least recently used entry is evicted (and its texture destroyed) when capacity is exceeded.
	/// </summary>
	internal sealed class GalleryImageBuffer
	{
		private readonly int _capacity;

		// value = (texture, its node in the LRU list)
		private readonly Dictionary<string, (Texture2D tex, LinkedListNode<string> node)> _cache = new();

		// front = most recently used, back = least recently used
		private readonly LinkedList<string> _lru = new();

		public int Count => _cache.Count;

		public GalleryImageBuffer(int capacity)
		{
			_capacity = capacity;
		}

		/// <summary>
		/// Returns the cached texture for key and promotes it to most-recently-used.
		/// Returns null if not cached.
		/// </summary>
		public Texture2D Get(string key)
		{
			if (!_cache.TryGetValue(key, out var entry)) return null;

			_lru.Remove(entry.node);
			var node = _lru.AddFirst(key);
			_cache[key] = (entry.tex, node);
			return entry.tex;
		}

		/// <summary>
		/// Inserts or updates a texture. Evicts the LRU entry if the buffer is at capacity.
		/// If the key already exists with a different texture, the old texture is destroyed.
		/// </summary>
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

		/// <summary>Removes the entry for key from the cache and destroys its texture.</summary>
		public void Evict(string key)
		{
			if (!_cache.Remove(key, out var entry)) return;
			_lru.Remove(entry.node);
			if (entry.tex != null) UnityEngine.Object.Destroy(entry.tex);
		}

		/// <summary>Destroys all cached textures and resets the buffer.</summary>
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
