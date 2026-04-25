using System;

namespace AudioSystem
{
    /// <summary>
    /// Identifies an audio category created by <see cref="AudioManager.CreateCategory"/>.
    /// <see cref="None"/> (default) means no category.
    /// </summary>
    public struct AudioCategory : IEquatable<AudioCategory>
    {
        /// <summary>The null/invalid category. Passing this to play methods disables category logic.</summary>
        public static readonly AudioCategory None = default;

        internal uint Id { get; }

        internal AudioCategory(uint id) => Id = id;
        
        public bool Equals(AudioCategory other) => Id == other.Id;
        public override bool Equals(object obj) => obj is AudioCategory other && Equals(other);
        public override int GetHashCode() => (int)Id;
        public static bool operator ==(AudioCategory a, AudioCategory b) => a.Id == b.Id;
        public static bool operator !=(AudioCategory a, AudioCategory b) => a.Id != b.Id;
    }
}
