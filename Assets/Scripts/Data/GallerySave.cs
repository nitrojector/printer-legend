using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Utility;

namespace Data
{
	public class GallerySave : PersistentConfig<GallerySave>
	{
		public override string ConfigPath =>
			Path.Combine(Application.persistentDataPath, "gallery.json");

		/// <summary>Absolute path to the directory where print images are stored.</summary>
		[JsonIgnore]
		public string ImageSaveDirectory =>
			Path.Combine(Application.persistentDataPath, "gallery_images");

		[JsonProperty("entries")]
		public List<GalleryEntry> Entries { get; set; } = new();
	}

	public class GalleryEntry
	{
		/// <summary>
		/// Path to the saved print image, relative to Application.persistentDataPath.
		/// Use forward slashes.
		/// </summary>
		[JsonProperty("image_path")]
		public string ImagePath { get; set; } = string.Empty;

		/// <summary>
		/// Path to the reference image used during printing.
		/// Prefix with "internal:" followed by the Resources-relative path for bundled assets
		/// (e.g. "internal:PrintRefs/foo"). Otherwise a path relative to
		/// Application.persistentDataPath using forward slashes.
		/// </summary>
		[JsonProperty("reference_image_path")]
		public string ReferenceImagePath { get; set; } = string.Empty;

		/// <summary>UTC time at which this print was completed.</summary>
		[JsonProperty("date")]
		public DateTime Date { get; set; } = DateTime.UtcNow;

		/// <summary>Similarity score between the print and the reference image [0..1].</summary>
		[JsonProperty("similarity_score")]
		public float SimilarityScore { get; set; }

		/// <summary>Number of times the player reset before this final print.</summary>
		[JsonProperty("reset_count")]
		public int ResetCount { get; set; }

		/// <summary>Time in seconds taken for the final print pass.</summary>
		[JsonProperty("print_duration")]
		public float PrintDuration { get; set; }
	}
}
