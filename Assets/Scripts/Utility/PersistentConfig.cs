using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Abstract class for a persistent configuration object. Configurations that
    /// need to be saved and loaded can inherit from this class and implement the
    /// abstract members. The config will be automatically loaded when <see cref="Instance"/>
    /// is accessed for the first time, and will be automatically saved when the
    /// application quits if <see cref="SaveOnApplicationQuit"/> is true (default).
    /// Configurations are saved in JSON format at the path specified by <see cref="ConfigPath"/>.
    /// </summary>
    /// <typeparam name="T">config object type by CRTP</typeparam>
    [JsonObject]
    public abstract class PersistentConfig<T> where T : PersistentConfig<T>, new()
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
        public abstract string ConfigPath { get; }

        /// <summary>
        /// Whether the config should be automatically saved when the application quits.
        /// Defaults to true, override to disable auto-saving on application quit.
        /// </summary>
        [JsonIgnore]
        public virtual bool SaveOnApplicationQuit => true;

        /// <summary>
        /// Whether the quitting handler has been added. Used to prevent multiple
        /// handlers being added.
        /// </summary>
        private static bool quittingHandlerAdded = false;

        /// <summary>
        /// Loads config from <see cref="ConfigPath"/>, if non-existent, creates
        /// the config with default constructed config object <see cref="T"/>.
        /// </summary>
        public static void Load()
        {
            var temp = new T();
            var filePath = temp.ConfigPath;

            if (!File.Exists(filePath))
            {
                instance = temp;
                Save();
                return;
            }

            var json = File.ReadAllText(filePath);
            JToken token = JToken.Parse(json);
            instance = token.ToObject<T>() ?? new T();

            if (!instance.SaveOnApplicationQuit || quittingHandlerAdded) return;
            Application.quitting += Save;
            quittingHandlerAdded = true;
        }

        /// <summary>
        /// Saves the config to <see cref="ConfigPath"/>. If the config is not
        /// loaded, creates a new config with default constructed config object
        /// <see cref="T"/> and saves it.
        /// <exception cref="InvalidDataException">thrown if directory cannot be resolved</exception>
        /// </summary>
        public static void Save()
        {
            instance ??= new T();

            var json = JsonConvert.SerializeObject(instance, Formatting.Indented);
            var filePath = instance.ConfigPath;
            var directory = Path.GetDirectoryName(filePath);

            if (directory == null)
            {
                throw new InvalidDataException($"Invalid config path: {filePath}");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);

            if (!instance.SaveOnApplicationQuit || quittingHandlerAdded) return;
            Application.quitting += Save;
            quittingHandlerAdded = true;
        }
    }
}
