using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    [CreateAssetMenu(fileName = "AudioBusMap", menuName = "AudioSystem/AudioBusMap", order = 0)]
    public class AudioBusMap : ScriptableObject
    {
        [SerializeField] AudioMixerGroup masterBus;
        [SerializeField] AudioMixerGroup musicBus;
        [SerializeField] AudioMixerGroup sfxBus;

        /// <summary>
        /// Returns the AudioMixerGroup associated with the given AudioBus.
        /// </summary>
        /// <param name="bus">bus to acquire mixer group of</param>
        /// <returns>the mixer group associated</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the provided bus is illegal</exception>
        public AudioMixerGroup Resolve(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Master => masterBus,
                AudioBus.Music => musicBus,
                AudioBus.SFX => sfxBus,
                _ => throw new System.ArgumentOutOfRangeException(nameof(bus), bus, null)
            };
        }

        /// <summary>
        /// Returns the AudioMixer associated with the master bus.
        /// </summary>
        /// <returns>mixer associated with the master mixerGroup (should be the same as others)</returns>
        internal AudioMixer GetMixer()
        {
            return masterBus.audioMixer;
        }
    }
}
