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
            public GameObject slot;
        }

        [SerializeField] private PrinterMagic magic;
        [SerializeField] private AbilitySlot[] slots;

        private void OnEnable()
        {
            if (magic == null) return;
            magic.onAbilityChanged += OnAbilityChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (magic == null) return;
            magic.onAbilityChanged -= OnAbilityChanged;
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

        private static void SetSlotVisible(AbilitySlot entry, bool visible)
        {
            if (entry.slot != null)
                entry.slot.SetActive(visible);
        }
    }
}