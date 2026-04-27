using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Utility;

namespace Config
{
    public class GameConfig : ResourceConfig<GameConfig>
    {
        public override string ResourcePath => "GameConfig";
        
        /// <summary>
        /// Config values for magic
        /// </summary>
        [JsonProperty("magic")]
        public MagicConfig Magic { get; set; } = new();
    }
}