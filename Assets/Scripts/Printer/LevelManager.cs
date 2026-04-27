using System;
using Config;
using UnityEngine;

namespace Printer
{
    /// <summary>
    /// Applies the current level from <see cref="LevelSequenceConfig"/> to the scene:
    /// enables the correct abilities/obstacles on <see cref="PrinterMagic"/> and loads
    /// the matching reference image into <see cref="PrinterReference"/>.
    ///
    /// Place this MonoBehaviour in the Printing scene. Wire magic and reference in the Inspector.
    /// Call <see cref="AdvanceLevel"/> when the player completes a level.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        private const string LevelIndexKey = "LevelIndex";

        [SerializeField] private PrinterMagic magic;
        [SerializeField] private PrinterReference reference;

        public static int CurrentLevelIndex
        {
            get => PlayerPrefs.GetInt(LevelIndexKey, 0);
            private set
            {
                PlayerPrefs.SetInt(LevelIndexKey, value);
                PlayerPrefs.Save();
            }
        }

        private void Start()
        {
            ApplyLevel(CurrentLevelIndex);
        }

        /// <summary>Advance to the next level and apply it. No-op if already at the last level.</summary>
        public void AdvanceLevel()
        {
            int next = CurrentLevelIndex + 1;
            if (next >= LevelSequenceConfig.Instance.Levels.Count) return;
            CurrentLevelIndex = next;
            ApplyLevel(next);
        }

        /// <summary>Advance the saved level index by one. Safe to call from any scene.</summary>
        public static void AdvanceLevelIndex()
        {
            int next = CurrentLevelIndex + 1;
            if (next < LevelSequenceConfig.Instance.Levels.Count)
                CurrentLevelIndex = next;
        }

        /// <summary>Reset the saved level index to 0. Safe to call from any scene.</summary>
        public static void ResetLevelIndex()
        {
            PlayerPrefs.DeleteKey(LevelIndexKey);
            PlayerPrefs.Save();
        }

        /// <summary>Reset progression back to level 0.</summary>
        public void ResetProgress()
        {
            CurrentLevelIndex = 0;
            ApplyLevel(0);
        }

        private void ApplyLevel(int index)
        {
            var config = LevelSequenceConfig.Instance;
            if (config.Levels.Count == 0)
            {
                Debug.LogWarning("LevelManager: LevelSequence.json has no levels.");
                return;
            }

            index = Mathf.Clamp(index, 0, config.Levels.Count - 1);
            var entry = config.Levels[index];

            // Clear all abilities and obstacles before applying the level set
            foreach (PrinterAbility ability in Enum.GetValues(typeof(PrinterAbility)))
                magic.DisableAbility(ability);
            foreach (PrinterObstacle obstacle in Enum.GetValues(typeof(PrinterObstacle)))
                magic.DisableObstacle(obstacle);

            foreach (var ability in entry.GetAbilities())
                magic.EnableAbility(ability);
            foreach (var obstacle in entry.GetObstacles())
                magic.EnableObstacle(obstacle);

            // Look up the sprite by filename via PrintRefManager, which already has all
            // sprites loaded correctly via Resources.LoadAll<Sprite>.
            if (!string.IsNullOrEmpty(entry.ImagePath))
            {
                string spriteName = System.IO.Path.GetFileName(entry.ImagePath);
                Sprite sprite = PrintRefManager.Instance.GetByName(spriteName);
                if (sprite != null)
                    reference.LoadReference(sprite);
                else
                    Debug.LogWarning($"LevelManager: sprite '{spriteName}' not found in PrintRefs.");
            }
        }
    }
}