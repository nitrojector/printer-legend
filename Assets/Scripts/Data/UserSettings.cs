using System.Collections.Generic;
using System.IO;
using AudioSystem;
using Newtonsoft.Json;
using UnityEngine;
using Utility;

namespace Data
{
	public class UserSettings : PersistentConfig<UserSettings>
	{
		public override string ConfigPath =>
			Path.Combine(Application.persistentDataPath, "settings.json");
		
		[JsonProperty("volumes")]
		public Dictionary<AudioBus, float> Volumes = new()
		{
			[AudioBus.Master] = 1.0f,
			[AudioBus.Music] = 1.0f,
			[AudioBus.SFX] = 1.0f,
		};
	}
}