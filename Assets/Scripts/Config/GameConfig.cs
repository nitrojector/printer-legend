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
        public MagicConfig Magic { get; set; }

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
                    config.usedDefaults = false;
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