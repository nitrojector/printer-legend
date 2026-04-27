using System;
using System.Collections.Generic;
using System.IO;
using Data;
using SFB;
using UnityEngine;
using Utility;

namespace Gallery
{
	/// <summary>
	/// Manages user-imported reference images.
	/// Imported files are copied to references/ under Application.persistentDataPath
	/// and tracked in <see cref="UserSave.References"/>.
	/// </summary>
	public static class UserReferenceManager
	{
		private static readonly string[] ImageExtensions = { "png", "jpg", "jpeg" };

		private static string ReferencesDir =>
			Path.Combine(Application.persistentDataPath, "references");

		// ── Access ────────────────────────────────────────────────────────────

		/// <summary>Read-only view of all imported references from the current save.</summary>
		public static IReadOnlyList<ReferenceEntry> GetReferences() =>
			UserSave.Instance.References;

		// ── Add ───────────────────────────────────────────────────────────────

		/// <summary>
		/// Opens an OS native file picker, copies the selected image to references/,
		/// appends the entry to <see cref="UserSave"/>, and saves immediately.
		/// Calls <paramref name="onAdded"/> with the new entry on success, or null if cancelled.
		/// Requires the StandaloneFileBrowser package (com.github.gkngkc.unitystandalonefilebrowser).
		/// </summary>
		public static void AddReference(Action<ReferenceEntry> onAdded = null)
		{
			var filters = new[] { new ExtensionFilter("Image Files", ImageExtensions) };
			var paths = StandaloneFileBrowser.OpenFilePanel("Import Reference Image", "", filters, false);

			if (paths == null || paths.Length == 0)
			{
				onAdded?.Invoke(null);
				return;
			}

			var sourcePath = paths[0];
			var entry = ImportFile(sourcePath);
			onAdded?.Invoke(entry);
		}

		/// <summary>
		/// Imports a reference image from <paramref name="sourcePath"/> without opening a file dialog.
		/// Copies it to references/, appends the entry to <see cref="UserSave"/>, and saves immediately.
		/// Returns the new entry, or null on failure.
		/// </summary>
		public static ReferenceEntry ImportFile(string sourcePath)
		{
			if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
			{
				Logr.Error($"UserReferenceManager: source file not found at '{sourcePath}'.");
				return null;
			}

			Directory.CreateDirectory(ReferencesDir);

			var stem = Path.GetFileNameWithoutExtension(sourcePath);
			var ext  = Path.GetExtension(sourcePath); // includes the dot
			var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
			var destFilename = $"{timestamp}__{stem}{ext}";
			var destFullPath = Path.Combine(ReferencesDir, destFilename);

			try
			{
				File.Copy(sourcePath, destFullPath, overwrite: false);
			}
			catch (Exception e)
			{
				Logr.Error($"UserReferenceManager: failed to copy '{sourcePath}' → '{destFullPath}': {e.Message}");
				return null;
			}

			var entry = new ReferenceEntry
			{
				Path = "references/" + destFilename,
				Name = stem,
			};

			UserSave.Instance.References.Add(entry);
			UserSave.Save();
			return entry;
		}

		// ── Remove ────────────────────────────────────────────────────────────

		/// <summary>
		/// Removes the reference at <paramref name="index"/> from <see cref="UserSave"/>,
		/// deletes the copied image from disk, and saves immediately.
		/// Returns false if the index is out of range.
		/// </summary>
		public static bool RemoveReference(int index)
		{
			var refs = UserSave.Instance.References;
			if (index < 0 || index >= refs.Count) return false;

			var entry = refs[index];
			refs.RemoveAt(index);

			var fullPath = Path.Combine(Application.persistentDataPath, entry.Path);
			if (File.Exists(fullPath))
			{
				try { File.Delete(fullPath); }
				catch (Exception e) { Logr.Error($"UserReferenceManager: failed to delete '{fullPath}': {e.Message}"); }
			}

			UserSave.Save();
			return true;
		}

		// ── Image loading ─────────────────────────────────────────────────────

		/// <summary>
		/// Loads and returns the Texture2D for <paramref name="entry"/> from disk.
		/// Not cached — caller is responsible for managing the texture lifetime.
		/// Returns null if the file is missing or unreadable.
		/// </summary>
		public static Texture2D LoadTexture(ReferenceEntry entry)
		{
			if (string.IsNullOrEmpty(entry?.Path)) return null;

			var fullPath = Path.Combine(Application.persistentDataPath, entry.Path);
			if (!File.Exists(fullPath))
			{
				Logr.Warn($"UserReferenceManager: reference image not found at '{fullPath}'.");
				return null;
			}

			byte[] bytes;
			try { bytes = File.ReadAllBytes(fullPath); }
			catch (Exception e)
			{
				Logr.Error($"UserReferenceManager: failed to read '{fullPath}': {e.Message}");
				return null;
			}

			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (tex.LoadImage(bytes)) return tex;

			UnityEngine.Object.Destroy(tex);
			Logr.Error($"UserReferenceManager: failed to decode image at '{fullPath}'");
			return null;
		}
	}
}
