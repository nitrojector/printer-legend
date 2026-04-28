using System;
using UnityEngine;

namespace Printer
{
    /// <summary>
    /// Shows or hides each toolbar slot based on which <see cref="PrinterAbility"/> values
    /// are currently enabled on <see cref="PrinterMagic"/>.
    ///
    /// Assign <see cref="magic"/> and populate <see cref="slots"/> in the Inspector.
    /// Each entry explicitly names the ability it represents, so slot order doesn't matter.
    /// Slot GameObjects should be inactive by default — the toolbar activates them.
    /// </summary>
    public class MagicToolbar : MonoBehaviour
    {
        [Serializable]
        public struct AbilitySlot
        {
            public PrinterAbility ability;
            public MagicToolbarAction slot;
        }

        [Serializable]
        public struct SpeedSlot
        {
            public int level; // 0=slow, 1=normal, 2=fast
            public MagicToolbarAction slot;
        }

        [SerializeField] private PrinterMagic magic;
        [SerializeField] private AbilitySlot[] slots;

        [Header("Speed")]
        [SerializeField] private PrinterPlayerController playerController;
        [SerializeField] private SpeedSlot[] speedSlots;

        private void OnEnable()
        {
            if (magic != null)
            {
                magic.onAbilityChanged += OnAbilityChanged;
                RefreshAll();
            }

            if (playerController != null)
            {
                playerController.OnSpeedLevelChanged += OnSpeedLevelChanged;
                RefreshSpeedActive(playerController.CurrentSpeedLevel);
            }
        }

        private void OnDisable()
        {
            if (magic != null)
                magic.onAbilityChanged -= OnAbilityChanged;
            if (playerController != null)
                playerController.OnSpeedLevelChanged -= OnSpeedLevelChanged;
        }

        private void RefreshAll()
        {
            foreach (var entry in slots)
                SetSlotVisible(entry, magic.IsAbilityEnabled(entry.ability));
        }

        private void OnAbilityChanged(PrinterAbility ability, bool enabled)
        {
            foreach (var entry in slots)
                if (entry.ability == ability)
                    SetSlotVisible(entry, enabled);
        }

        private void OnSpeedLevelChanged(int level) => RefreshSpeedActive(level);

        private void RefreshSpeedActive(int activeLevel)
        {
            foreach (var entry in speedSlots)
                if (entry.slot != null)
                    entry.slot.SetStyleActive(entry.level == activeLevel);
        }

        private static void SetSlotVisible(AbilitySlot entry, bool visible)
        {
            if (entry.slot != null)
                entry.slot.gameObject.SetActive(visible);
        }
    }
}