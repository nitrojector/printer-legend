using System;

namespace AudioSystem
{
    /// <summary>
    /// Represents a handle to an audio instance.
    /// </summary>
    public struct AudioHandle : IEquatable<AudioHandle>
    {
        public static readonly AudioHandle Invalid = default;
        
        private uint id;

        public AudioHandle(uint id)
        {
            this.id = id;
        }
        
        public override bool Equals(object obj) => obj is AudioHandle other && Equals(other);
        public bool Equals(AudioHandle other) => id == other.id;
        public override int GetHashCode() => (int)id;
        public static bool operator ==(AudioHandle a, AudioHandle b) => a.id == b.id;
        public static bool operator !=(AudioHandle a, AudioHandle b) => !(a == b);
    }
}
