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
        public float PrintSpeedSlow { get; set; } = 5f;
        
        /// <summary>
        /// Slow print speed in units per second.
        /// </summary>
        [JsonProperty("print_speed_normal")]
        public float PrintSpeedNormal { get; set; } = 10f;
        
        /// <summary>
        /// Fast print speed in units per second.
        /// </summary>
        [JsonProperty("print_speed_fast")]
        public float PrintSpeedFast { get; set; } = 20f;
        
        // =====================================================================
        // Paper Jam
        // =====================================================================
        
        [JsonProperty("paperjam_chance")]
        public float PaperJamChance { get; set; } = 0.1f;

        [JsonProperty("paperjam_line_count")]
        public int PaperJamLineCount { get; set; } = 2;

        [JsonProperty("paperjam_shuffle_count")]
        public int PaperJamShuffleCount { get; set; } = 12;

        [JsonProperty("paperjam_respect_print_size")]
        public bool PaperJamRespectPrintSize { get; set; } = true;

        // =====================================================================
        // Internet Disconnect
        // =====================================================================

        [JsonProperty("internet_disconnect_chance")]
        public float InternetDisconnectChance { get; set; } = 0.1f;

        [JsonProperty("internet_disconnect_line_count")]
        public int InternetDisconnectLineCount { get; set; } = 4;

        [JsonProperty("internet_disconnect_blackout_seconds")]
        public float InternetDisconnectBlackoutSeconds { get; set; } = 1.5f;

        [JsonProperty("internet_disconnect_fadein_seconds")]
        public float InternetDisconnectFadeInSeconds { get; set; } = 0.5f;

        // =====================================================================
        // Motor Malfunction
        // =====================================================================

        [JsonProperty("motor_malfunction_chance")]
        public float MotorMalfunctionChance { get; set; } = 0.1f;

        [JsonProperty("motor_malfunction_line_count")]
        public int MotorMalfunctionLineCount { get; set; } = 2;
    }
}