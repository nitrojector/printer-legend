using Newtonsoft.Json;

namespace Config
{
    [JsonObject]
    public class MagicConfig
    {
        // =====================================================================
        // Speed Adjust
        // =====================================================================
        
        /// <summary>
        /// Slow print speed in units per second.
        /// </summary>
        [JsonProperty("print_speed_slow")]
        public float PrintSpeedSlow { get; set; }
        
        /// <summary>
        /// Slow print speed in units per second.
        /// </summary>
        [JsonProperty("print_speed_normal")]
        public float PrintSpeedNormal { get; set; }
        
        /// <summary>
        /// Fast print speed in units per second.
        /// </summary>
        [JsonProperty("print_speed_fast")]
        public float PrintSpeedFast { get; set; }
        
        // =====================================================================
        // Paper Jam
        // =====================================================================
    }
}