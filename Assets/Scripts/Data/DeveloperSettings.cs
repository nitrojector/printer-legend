using Newtonsoft.Json;
using Utility;

namespace Data
{
	public class DeveloperSettings : PersistentConfig<DeveloperSettings>
	{
		public override string ConfigPath => "devopts.json";
		
		[JsonProperty("enableGameMaster")]
		public bool EnableGameMaster { get; set; } = false;
	}
}