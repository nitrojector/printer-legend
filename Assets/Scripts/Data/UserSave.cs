using Newtonsoft.Json;
using Utility;

namespace Data
{
	public class UserSave : PersistentConfig<UserSave>
	{
		public override string ConfigPath => "save.json";
		
		[JsonProperty("progression_next_print_idx")]
		public int ProgressionNextPrintIdx = 0;
	}
}