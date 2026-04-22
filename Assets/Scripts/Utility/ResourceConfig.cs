using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Abstract class for a read-only, resource referenced, configuration object.
    /// </summary>
    /// <typeparam name="T">config object type by CRTP</typeparam>
    [JsonObject]
    public abstract class ResourceConfig<T> where T : ResourceConfig<T>, new()
    {
        /// <summary>
        /// Singleton instance of the config. Should not be accessed directly,
        /// use <see cref="Instance"/> instead.
        /// </summary>
        private static T instance;

        /// <summary>
        /// Instance of the config. If config is not yet loaded, loads automatically.
        /// </summary>
        [JsonIgnore]
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    Load();
                }

                return instance;
            }
        }

        /// <summary>
        /// If the Config is loaded
        /// </summary>
        [JsonIgnore]
        public static bool Loaded => instance != null;

        /// <summary>
        /// Path where the configuration should exist at.
        /// </summary>
        [JsonIgnore]
        public abstract string ResourcePath { get; }

        /// <summary>
        /// Loads config from <see cref="ResourcePath"/>, if non-existent, creates
        /// the config with default constructed config object <see cref="T"/>.
        /// </summary>
        public static void Load()
        {
            var temp = new T();
            TextAsset asset = Resources.Load<TextAsset>(temp.ResourcePath);
            JToken token = JToken.Parse(asset.text);
            instance = token.ToObject<T>() ?? new T();
        }

    }
}
