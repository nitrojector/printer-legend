using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Config
{
    [JsonObject]
    public class GameConfig
    {
        [JsonIgnore]
        private static GameConfig instance;
        
        [JsonIgnore]
        private bool usedDefaults;

        /// <summary>
        /// Singleton instance
        /// </summary>
        [JsonIgnore]
        public static GameConfig Instance => instance ??= LoadGameConfig();
        
        /// <summary>
        /// Config values for magic
        /// </summary>
        [JsonProperty("magic")]
        public MagicConfig Magic { get; set; } = new();

        /// <summary>
        /// Loads GameConfig from JSON file
        /// </summary>
        private static GameConfig LoadGameConfig()
        {
            var filePath = Path.Combine(Application.dataPath, "config.json");
            GameConfig config;
            bool fileExists = File.Exists(filePath);
            
            if (!fileExists)
            {
#if DEBUG
                Debug.LogWarning($"GameConfig: config.json not found at '{filePath}'. Using defaults and will save on quit.");
#endif
                config = new GameConfig();
                config.usedDefaults = true;
            }
            else
            {
                var json = File.ReadAllText(filePath);
                try
                {
                    JToken token = JToken.Parse(json);
                    config = token.ToObject<GameConfig>() ?? new GameConfig();
                    bool missingFields = !HasAllKnownFields(token);
                    bool defaultedRefs = config.ApplyDefaults();
                    config.usedDefaults = missingFields || defaultedRefs;
#if DEBUG
                    if (missingFields)
                        Debug.LogWarning("GameConfig: config.json is missing known fields. Missing values will be filled and saved on quit.");
#endif
                }
                catch
                {
#if DEBUG
                    Debug.LogWarning($"GameConfig: Failed to parse config.json. Using defaults and will save on quit.");
#endif
                    config = new GameConfig();
                    config.usedDefaults = true;
                }
            }
            
            Application.quitting += () => config.SaveIfNeeded();
            return config;
        }

        private bool ApplyDefaults()
        {
            bool changed = false;
            if (Magic == null)
            {
                Magic = new MagicConfig();
                changed = true;
            }

            return changed;
        }

        private static bool HasAllKnownFields(JToken token)
        {
            string[] requiredPaths =
            {
                "magic",
                "magic.print_speed_slow",
                "magic.print_speed_normal",
                "magic.print_speed_fast",
                "magic.paperjam_chance",
                "magic.paperjam_line_count",
                "magic.paperjam_shuffle_count",
                "magic.paperjam_respect_print_size",
                "magic.internet_disconnect_chance",
                "magic.internet_disconnect_line_count",
                "magic.internet_disconnect_blackout_seconds",
                "magic.internet_disconnect_fadein_seconds",
                "magic.motor_malfunction_chance",
                "magic.motor_malfunction_line_count",
            };

            foreach (string path in requiredPaths)
            {
                if (token.SelectToken(path) == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Saves config to file if defaults were used
        /// </summary>
        private void SaveIfNeeded()
        {
            if (!usedDefaults)
                return;

            SaveGameConfig();
        }

        /// <summary>
        /// Saves GameConfig to JSON file
        /// </summary>
        private void SaveGameConfig()
        {
            var filePath = Path.Combine(Application.dataPath, "config.json");
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            
            try
            {
                File.WriteAllText(filePath, json);
#if DEBUG
                Debug.Log($"GameConfig: Saved config.json to '{filePath}'");
#endif
            }
            catch (System.Exception ex)
            {
#if DEBUG
                Debug.LogError($"GameConfig: Failed to save config.json - {ex.Message}");
#endif
            }
        }
    }
}