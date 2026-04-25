namespace AudioSystem
{
    /// <summary>
    /// Determines what happens when a category's voice limit is exceeded.
    /// </summary>
    public enum AudioOverflowMode
    {
        /// <summary>The new play request is silently rejected.</summary>
        IgnoreNew,

        /// <summary>The oldest active voice in the category is stopped to make room.</summary>
        StopOldest,
    }
}
