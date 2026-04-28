using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Utility;

namespace Data
{
	public class DeveloperSettings : PersistentConfig<DeveloperSettings>
	{
		public override string ConfigPath =>
			Path.Combine(Application.persistentDataPath, "devopts.json");
		
		[JsonProperty("enableGameMaster")]
		public bool EnableGameMaster { get; set; } = false;
	}
}