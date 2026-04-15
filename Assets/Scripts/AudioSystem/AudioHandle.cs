using System;

namespace AudioSystem
{
    /// <summary>
    /// Represents a handle to an audio instance.
    /// </summary>
    public struct AudioHandle : IEquatable<AudioHandle>
    {
        private uint id;

        public AudioHandle(uint id)
        {
            this.id = id;
        }

        public bool Equals(AudioHandle other)
        {
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)id;
        }
    }
}
