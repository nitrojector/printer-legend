using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    /// <summary>
    /// Provides mixer volume control and one-shot or managed audio playback.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of the <see cref="AudioManager"/>.
        /// If it doesn't exist, it will be created.
        /// </summary>
        public static AudioManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindFirstObjectByType<AudioManager>();
                if (instance != null)
                {
                    return instance;
                }

                instance = new GameObject("AudioManager").AddComponent<AudioManager>();
                return instance;
            }
        }

        private static AudioManager instance;
        private AudioBusMap audioBusMap;
        private AudioMixer mixer;

        private readonly Dictionary<AudioHandle, AudioSource> managedSources = new();

        private readonly HashSet<AudioHandle> pausedHandles = new();
        private readonly HashSet<AudioHandle> loopingHandles = new();
        private readonly List<AudioHandle> cleanupHandles = new();
        private uint nextHandleId = 1;

        /// <summary>Sets the volume of the given bus.</summary>
        /// <param name="channel">bus to adjust</param>
        /// <param name="volume">linear volume [0..1]</param>
        public void SetVolume(AudioBus channel, float volume)
        {
            string busName = audioBusMap.Resolve(channel).name;
            mixer.SetFloat($"Volume{busName}", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
        }

        /// <summary>Plays a clip attached to a target transform.</summary>
        /// <param name="clip">clip to play</param>
        /// <param name="target">target transform to follow</param>
        /// <param name="channel">bus to route through</param>
        public bool Play(AudioClip clip, Transform target, AudioBus channel)
        {
            if (clip == null || target == null || audioBusMap == null)
            {
                return false;
            }

            AudioSource source = CreateSource(clip, channel, false);
            source.transform.SetParent(target, false);
            source.Play();
            Destroy(source.gameObject, clip.length);
            return true;
        }

        /// <summary>Plays a clip at a world position.</summary>
        /// <param name="clip">clip to play</param>
        /// <param name="position">world position to spawn at</param>
        /// <param name="channel">bus to route through</param>
        public bool Play(AudioClip clip, Vector3 position, AudioBus channel)
        {
            if (clip == null || audioBusMap == null)
            {
                return false;
            }

            AudioSource source = CreateSource(clip, channel, false);
            source.transform.position = position;
            source.Play();
            Destroy(source.gameObject, clip.length);
            return true;
        }

        /// <summary>Plays a tracked clip attached to a target transform.</summary>
        /// <param name="clip">clip to play</param>
        /// <param name="target">target transform to follow</param>
        /// <param name="channel">bus to route through</param>
        /// <param name="handle">the handle of the played clip if successful</param>
        /// <param name="loop">whether the clip should loop</param>
        /// <returns>handle for the tracked instance</returns>
        public bool PlayManaged(AudioClip clip, Transform target, AudioBus channel, out AudioHandle handle,
            bool loop = false)
        {
            if (clip == null || target == null || audioBusMap == null)
            {
                handle = default;
                return false;
            }

            AudioSource source = CreateSource(clip, channel, loop);
            source.transform.SetParent(target, false);
            handle = new AudioHandle(nextHandleId++);
            managedSources[handle] = source;
            if (loop)
            {
                loopingHandles.Add(handle);
            }

            source.Play();
            return true;
        }

        /// <summary>
        /// Plays a tracked clip at a world position.
        /// </summary>
        /// <param name="clip">clip to play</param>
        /// <param name="position">world position to spawn at</param>
        /// <param name="channel">bus to route through</param>
        /// <param name="handle">the handle of the played clip if successful</param>
        /// <param name="loop">whether the clip should loop</param>
        /// <returns>handle for the tracked instance</returns>
        public bool PlayManaged(AudioClip clip, Vector3 position, AudioBus channel, out AudioHandle handle,
            bool loop = false)
        {
            if (clip == null || audioBusMap == null)
            {
                handle = default;
                return false;
            }

            AudioSource source = CreateSource(clip, channel, loop);
            source.transform.position = position;
            handle = new AudioHandle(nextHandleId++);
            managedSources[handle] = source;
            if (loop)
            {
                loopingHandles.Add(handle);
            }

            source.Play();
            return true;
        }

        /// <summary>
        /// Stops a managed audio instance.
        /// </summary>
        /// <param name="handle">handle to stop</param>
        /// <returns>true if stopped</returns>
        public bool Stop(AudioHandle handle)
        {
            if (!managedSources.TryGetValue(handle, out AudioSource source) || source == null)
            {
                return false;
            }

            pausedHandles.Remove(handle);
            loopingHandles.Remove(handle);
            managedSources.Remove(handle);
            source.Stop();
            Destroy(source.gameObject);
            return true;
        }

        /// <summary>
        /// Pauses a tracked instance.
        /// </summary>
        /// <param name="handle">handle to pause</param>
        /// <returns>true if paused</returns>
        public bool Pause(AudioHandle handle)
        {
            if (!managedSources.TryGetValue(handle, out AudioSource source) || source == null)
            {
                return false;
            }

            source.Pause();
            pausedHandles.Add(handle);
            return true;
        }

        /// <summary>Resumes a tracked instance.</summary>
        /// <param name="handle">handle to resume</param>
        /// <returns>true if resumed</returns>
        public bool Resume(AudioHandle handle)
        {
            if (!managedSources.TryGetValue(handle, out AudioSource source) || source == null)
            {
                return false;
            }

            source.UnPause();
            pausedHandles.Remove(handle);
            return true;
        }

        /// <summary>Checks whether a tracked instance is playing.</summary>
        /// <param name="handle">handle to query</param>
        /// <returns>true if playing</returns>
        public bool IsPlaying(AudioHandle handle)
        {
            if (!managedSources.TryGetValue(handle, out AudioSource source) || source == null)
            {
                return false;
            }

            return source.isPlaying;
        }

        /// <summary>Creates a configured AudioSource.</summary>
        /// <param name="clip">clip to assign</param>
        /// <param name="channel">bus to route through</param>
        /// <param name="loop">whether the source should loop</param>
        /// <returns>configured source</returns>
        private AudioSource CreateSource(AudioClip clip, AudioBus channel, bool loop)
        {
            string goName = string.IsNullOrEmpty(clip.name) ? "AudioEmitter" : $"AudioEmitter[{clip.name}]";
            GameObject go = new GameObject(goName);
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.outputAudioMixerGroup = audioBusMap.Resolve(channel);
            return source;
        }

        /// <summary>Cleans up finished managed sources.</summary>
        private void LateUpdate()
        {
            if (managedSources.Count == 0)
            {
                return;
            }

            cleanupHandles.Clear();
            foreach (var (handle, source) in managedSources)
            {
                if (source == null)
                {
                    cleanupHandles.Add(handle);
                    continue;
                }

                if (!source.isPlaying && !pausedHandles.Contains(handle) && !loopingHandles.Contains(handle))
                {
                    cleanupHandles.Add(handle);
                }
            }

            foreach (var handle in cleanupHandles)
            {
                if (!managedSources.Remove(handle, out AudioSource source))
                {
                    continue;
                }

                pausedHandles.Remove(handle);
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }

            audioBusMap = Resources.Load<AudioBusMap>("Audio/AudioBusMap");
            mixer = audioBusMap.GetMixer();
        }
    }
}
