using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Utility;

namespace Gallery
{
	public class GallerySave : PersistentConfig<GallerySave>
	{
		public override string ConfigPath =>
			Path.Combine(Application.persistentDataPath, "gallery.json");
		
		[JsonIgnore]
		public string ImageSaveDirectory =>
			Path.Combine(Application.persistentDataPath, "gallery_images");
		
		[JsonProperty("entries")]
		public GalleryEntry[] GalleryEntries { get; set; } = Array.Empty<GalleryEntry>();
	}
	
	public class GalleryEntry
	{
		[JsonProperty("date")]
		public DateTime Date { get; set; } = DateTime.Now;
		
		[JsonProperty("image_path")]
		public string ImagePath { get; set; } = string.Empty;
	}
}