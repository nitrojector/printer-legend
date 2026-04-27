using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Printer;
using Utility;

namespace Config
{
    [JsonObject]
    public class LevelSequenceConfig : ResourceConfig<LevelSequenceConfig>
    {
        public override string ResourcePath => "LevelSequence";

        [JsonProperty("levels")]
        public List<LevelEntry> Levels { get; set; } = new();
    }

    [JsonObject]
    public class LevelEntry
    {
        /// <summary>Resource path relative to Resources/, e.g. "PrintRefs/abc123".</summary>
        [JsonProperty("image_path")]
        public string ImagePath { get; set; } = "";

        [JsonProperty("abilities")]
        public List<string> Abilities { get; set; } = new();

        [JsonProperty("obstacles")]
        public List<string> Obstacles { get; set; } = new();

        public IEnumerable<PrinterAbility> GetAbilities()
        {
            foreach (var s in Abilities)
                if (Enum.TryParse<PrinterAbility>(s, out var a)) yield return a;
        }

        public IEnumerable<PrinterObstacle> GetObstacles()
        {
            foreach (var s in Obstacles)
                if (Enum.TryParse<PrinterObstacle>(s, out var o)) yield return o;
        }
    }
}