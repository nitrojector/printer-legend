namespace AudioSystem
{
    /// <summary>
    /// Configuration for an <see cref="AudioCategory"/>.
    /// The zero-value struct is a valid unconstrained config (no voice cap, no cooldown, full volume).
    /// </summary>
    public struct AudioCategoryConfig
    {
        /// <summary>
        /// Maximum number of simultaneously playing voices in this category.
        /// 0 means unlimited.
        /// </summary>
        public int MaxVoices;

        /// <summary>
        /// What happens when <see cref="MaxVoices"/> is exceeded.
        /// </summary>
        public AudioOverflowMode Overflow;

        /// <summary>
        /// Volume multiplier applied to every voice in this category [0..1].
        /// 0 is treated as 1 (full volume) — use <see cref="Mute"/> to silence the category.
        /// </summary>
        public float Volume;

        /// <summary>
        /// When true, all voices in this category play at zero volume.
        /// Voices are still tracked and subject to polyphony limits.
        /// </summary>
        public bool Mute;

        /// <summary>
        /// Minimum seconds that must elapse between successive plays in this category.
        /// Useful for high-frequency SFX (footsteps, collision hits) to avoid stacking.
        /// 0 means no cooldown.
        /// </summary>
        public float MinInterval;
    }
}
