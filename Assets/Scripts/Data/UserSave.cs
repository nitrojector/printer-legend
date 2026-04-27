using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Utility;

namespace Data
{
	public class UserSave : PersistentConfig<UserSave>
	{
		public override string ConfigPath =>
			Path.Combine(Application.persistentDataPath, "save.json");

		[JsonProperty("progression_next_print_idx")]
		public int ProgressionNextPrintIdx = 0;

		[JsonProperty("references")]
		public List<ReferenceEntry> References { get; set; } = new();
	}

	[JsonObject]
	public class ReferenceEntry
	{
		/// <summary>
		/// Path to the reference image, relative to Application.persistentDataPath.
		/// Always under references/ and uses forward slashes.
		/// </summary>
		[JsonProperty("path")]
		public string Path { get; set; } = string.Empty;

		/// <summary>
		/// Display name derived from the original filename stem at import time.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; } = string.Empty;
	}
}
