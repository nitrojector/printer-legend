using Newtonsoft.Json;
using Utility;

public class GameSettings : ResourceConfig<GameSettings>
{
	public override string ResourcePath => "GameSettings";
	
	[JsonProperty("printhead_speeds")]
	public PrintHeadSpeeds PrintHeadSpeeds { get; private set; }
}

[JsonObject]
public class PrintHeadSpeeds
{
	[JsonProperty("slow")] public float Slow;
	[JsonProperty("normal")] public float Normal;
	[JsonProperty("fast")] public float Fast;
}