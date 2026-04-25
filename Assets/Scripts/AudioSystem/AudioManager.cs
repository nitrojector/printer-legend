using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    /// <summary>
    /// Provides mixer volume control and one-shot or managed audio playback, with optional category support.
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
                if (instance != null) return instance;

                instance = FindFirstObjectByType<AudioManager>();
                if (instance != null) return instance;

                instance = new GameObject("AudioManager").AddComponent<AudioManager>();
                return instance;
            }
        }

        /// <summary>Called when the audio manager is enabled.</summary>
        public static event Action OnEnableAudioManager;

        private static AudioManager instance;
        private AudioBusMap audioBusMap;
        private AudioMixer mixer;

        // Handle tracking
        private readonly Dictionary<AudioHandle, AudioSource> activeSources = new();
        private readonly HashSet<AudioHandle> pausedHandles = new();
        private readonly HashSet<AudioHandle> loopingHandles = new();
        private readonly List<AudioHandle> cleanupHandles = new();
        private uint nextHandleId = 1;

        // Category tracking
        private readonly Dictionary<AudioCategory, CategoryState> categories = new();
        private readonly Dictionary<AudioHandle, AudioCategory> handleToCategory = new();
        private uint nextCategoryId = 1;

        private class CategoryState
        {
            public AudioCategoryConfig Config;
            public readonly List<AudioHandle> ActiveVoices = new();
            public float LastPlayTime = float.NegativeInfinity;
        }

        // ── Volume ───────────────────────────────────────────────────────────

        /// <summary>Sets the volume of the given bus.</summary>
        /// <param name="channel">bus to adjust</param>
        /// <param name="volume">linear volume [0..1]</param>
        public void SetVolume(AudioBus channel, float volume)
        {
            string busName = audioBusMap.Resolve(channel).name;
            mixer.SetFloat($"Volume{busName}", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
        }

        // ── Category management ───────────────────────────────────────────────

        /// <summary>
        /// Creates a new audio category with the given config and returns its handle.
        /// </summary>
        public AudioCategory CreateCategory(AudioCategoryConfig config = default)
        {
            var category = new AudioCategory(nextCategoryId++);
            categories[category] = new CategoryState { Config = config };
            return category;
        }

        /// <summary>
        /// Destroys a category, stopping all of its currently active voices.
        /// </summary>
        public bool DestroyCategory(AudioCategory category)
        {
            if (!categories.ContainsKey(category)) return false;
            StopAllInCategory(category);
            categories.Remove(category);
            return true;
        }

        /// <summary>
        /// Reconfigures a category. Volume and mute changes are applied immediately to all active voices.
        /// </summary>
        public bool ConfigureCategory(AudioCategory category, AudioCategoryConfig config)
        {
            if (!categories.TryGetValue(category, out var state)) return false;

            state.Config = config;

            PruneVoices(state);
            float vol = EffectiveVolume(config);
            foreach (var handle in state.ActiveVoices)
            {
                if (activeSources.TryGetValue(handle, out var src) && src != null)
                    src.volume = vol;
            }

            return true;
        }

        /// <summary>Stops all active voices in a category.</summary>
        public bool StopAllInCategory(AudioCategory category)
        {
            if (!categories.TryGetValue(category, out var state)) return false;

            PruneVoices(state);
            var snapshot = new List<AudioHandle>(state.ActiveVoices);
            foreach (var handle in snapshot)
                Stop(handle);
            return true;
        }

        /// <summary>Pauses all active voices in a category.</summary>
        public bool PauseAllInCategory(AudioCategory category)
        {
            if (!categories.TryGetValue(category, out var state)) return false;

            PruneVoices(state);
            foreach (var handle in state.ActiveVoices)
                Pause(handle);
            return true;
        }

        /// <summary>Resumes all paused voices in a category.</summary>
        public bool ResumeAllInCategory(AudioCategory category)
        {
            if (!categories.TryGetValue(category, out var state)) return false;

            PruneVoices(state);
            foreach (var handle in state.ActiveVoices)
                Resume(handle);
            return true;
        }

        // ── Playback ──────────────────────────────────────────────────────────

        /// <summary>Plays a clip attached to a target transform.</summary>
        /// <param name="clip">clip to play</param>
        /// <param name="target">target transform to follow</param>
        /// <param name="channel">bus to route through</param>
        /// <param name="category">category to associate with; use <see cref="AudioCategory.None"/> for none</param>
        public bool Play(AudioClip clip, Transform target, AudioBus channel, AudioCategory category = default)
        {
            if (clip == null || target == null || audioBusMap == null) return false;
            if (!TryAcquireCategorySlot(category, out var toEvict)) return false;

            if (toEvict != default) Stop(toEvict);

            var source = CreateSource(clip, channel, false);
            source.transform.SetParent(target, false);
            var handle = GetNewHandle();
            activeSources[handle] = source;
            RegisterWithCategory(handle, source, category);
            source.Play();
            return true;
        }

        /// <summary>Plays a clip at a world position.</summary>
        /// <param name="clip">clip to play</param>
        /// <param name="position">world position to spawn at</param>
        /// <param name="channel">bus to route through</param>
        /// <param name="category">category to associate with; use <see cref="AudioCategory.None"/> for none</param>
        public bool Play(AudioClip clip, Vector3 position, AudioBus channel, AudioCategory category = default)
        {
            if (clip == null || audioBusMap == null) return false;
            if (!TryAcquireCategorySlot(category, out var toEvict)) return false;

            if (toEvict != default) Stop(toEvict);

            var source = CreateSource(clip, channel, false);
            source.transform.position = position;
            var handle = GetNewHandle();
            activeSources[handle] = source;
            RegisterWithCategory(handle, source, category);
            source.Play();
            return true;
        }

        /// <summary>Plays a tracked clip attached to a target transform.</summary>
        /// <param name="clip">clip to play</param>
        /// <param name="target">target transform to follow</param>
        /// <param name="channel">bus to route through</param>
        /// <param name="handle">the handle of the played clip if successful</param>
        /// <param name="loop">whether the clip should loop</param>
        /// <param name="category">category to associate with; use <see cref="AudioCategory.None"/> for none</param>
        public bool PlayManaged(AudioClip clip, Transform target, AudioBus channel, out AudioHandle handle,
            bool loop = false, AudioCategory category = default)
        {
            if (clip == null || target == null || audioBusMap == null)
            {
                handle = default;
                return false;
            }

            if (!TryAcquireCategorySlot(category, out var toEvict))
            {
                handle = default;
                return false;
            }

            if (toEvict != default) Stop(toEvict);

            var source = CreateSource(clip, channel, loop);
            source.transform.SetParent(target, false);
            handle = GetNewHandle();
            activeSources[handle] = source;
            if (loop) loopingHandles.Add(handle);
            RegisterWithCategory(handle, source, category);
            source.Play();
            return true;
        }

        /// <summary>Plays a tracked clip at a world position.</summary>
        /// <param name="clip">clip to play</param>
        /// <param name="position">world position to spawn at</param>
        /// <param name="channel">bus to route through</param>
        /// <param name="handle">the handle of the played clip if successful</param>
        /// <param name="loop">whether the clip should loop</param>
        /// <param name="category">category to associate with; use <see cref="AudioCategory.None"/> for none</param>
        public bool PlayManaged(AudioClip clip, Vector3 position, AudioBus channel, out AudioHandle handle,
            bool loop = false, AudioCategory category = default)
        {
            if (clip == null || audioBusMap == null)
            {
                handle = default;
                return false;
            }

            if (!TryAcquireCategorySlot(category, out var toEvict))
            {
                handle = default;
                return false;
            }

            if (toEvict != default) Stop(toEvict);

            var source = CreateSource(clip, channel, loop);
            source.transform.position = position;
            handle = GetNewHandle();
            activeSources[handle] = source;
            if (loop) loopingHandles.Add(handle);
            RegisterWithCategory(handle, source, category);
            source.Play();
            return true;
        }

        // ── Handle control ────────────────────────────────────────────────────

        /// <summary>
        /// Tries to get the AudioSource associated with a handle.
        /// </summary>
        public bool TryGetAudioSource(AudioHandle handle, out AudioSource source)
        {
            return activeSources.TryGetValue(handle, out source) && source != null;
        }

        /// <summary>Stops a managed audio instance.</summary>
        public bool Stop(AudioHandle handle)
        {
            if (!activeSources.TryGetValue(handle, out var source) || source == null) return false;

            pausedHandles.Remove(handle);
            loopingHandles.Remove(handle);
            activeSources.Remove(handle);
            RemoveFromCategory(handle);
            source.Stop();
            Destroy(source.gameObject);
            return true;
        }

        /// <summary>Pauses a tracked instance.</summary>
        public bool Pause(AudioHandle handle)
        {
            if (!activeSources.TryGetValue(handle, out var source) || source == null) return false;

            source.Pause();
            pausedHandles.Add(handle);
            return true;
        }

        /// <summary>Resumes a tracked instance.</summary>
        public bool Resume(AudioHandle handle)
        {
            if (!activeSources.TryGetValue(handle, out var source) || source == null) return false;

            source.UnPause();
            pausedHandles.Remove(handle);
            return true;
        }

        /// <summary>Checks whether a tracked instance is playing.</summary>
        public bool IsPlaying(AudioHandle handle)
        {
            return activeSources.TryGetValue(handle, out var source) && source != null && source.isPlaying;
        }

        /// <summary>Checks whether a handle refers to a live AudioSource.</summary>
        public bool IsValid(AudioHandle handle)
        {
            return activeSources.TryGetValue(handle, out var source) && source != null;
        }

        /// <summary>Checks whether a tracked instance is paused.</summary>
        public bool IsPaused(AudioHandle handle)
        {
            return activeSources.ContainsKey(handle) && pausedHandles.Contains(handle);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        /// <summary>
        /// Checks category constraints (cooldown, polyphony) and returns whether the play is allowed.
        /// If StopOldest eviction is needed, the handle to evict is returned via <paramref name="toEvict"/>.
        /// </summary>
        private bool TryAcquireCategorySlot(AudioCategory category, out AudioHandle toEvict)
        {
            toEvict = default;
            if (category == AudioCategory.None || !categories.TryGetValue(category, out var state))
                return true;

            var cfg = state.Config;

            if (cfg.MinInterval > 0f && Time.unscaledTime - state.LastPlayTime < cfg.MinInterval)
                return false;

            PruneVoices(state);

            if (cfg.MaxVoices > 0 && state.ActiveVoices.Count >= cfg.MaxVoices)
            {
                if (cfg.Overflow == AudioOverflowMode.IgnoreNew)
                    return false;

                // StopOldest: hand the caller the oldest voice to evict
                toEvict = state.ActiveVoices[0];
                state.ActiveVoices.RemoveAt(0);
                handleToCategory.Remove(toEvict);
            }

            return true;
        }

        /// <summary>
        /// Applies category volume/mute to a freshly created source and registers the handle.
        /// No-op when <paramref name="category"/> is <see cref="AudioCategory.None"/>.
        /// </summary>
        private void RegisterWithCategory(AudioHandle handle, AudioSource source, AudioCategory category)
        {
            if (category == AudioCategory.None || !categories.TryGetValue(category, out var state))
                return;

            source.volume = EffectiveVolume(state.Config);
            state.ActiveVoices.Add(handle);
            state.LastPlayTime = Time.unscaledTime;
            handleToCategory[handle] = category;
        }

        /// <summary>Removes a handle from its category's voice list. No-op if unregistered.</summary>
        private void RemoveFromCategory(AudioHandle handle)
        {
            if (!handleToCategory.Remove(handle, out var category)) return;
            if (categories.TryGetValue(category, out var state))
                state.ActiveVoices.Remove(handle);
        }

        /// <summary>Removes finished voices from a category's active list.</summary>
        private void PruneVoices(CategoryState state)
        {
            state.ActiveVoices.RemoveAll(h => !activeSources.ContainsKey(h));
        }

        /// <summary>Returns the source volume to use given a config (treats Volume=0 as 1).</summary>
        private static float EffectiveVolume(AudioCategoryConfig cfg)
        {
            if (cfg.Mute) return 0f;
            return Mathf.Approximately(cfg.Volume, 0f) ? 1f : cfg.Volume;
        }

        private AudioSource CreateSource(AudioClip clip, AudioBus channel, bool loop)
        {
            string goName = string.IsNullOrEmpty(clip.name) ? "AudioEmitter" : $"AudioEmitter[{clip.name}]";
            var go = new GameObject(goName);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.outputAudioMixerGroup = audioBusMap.Resolve(channel);
            return source;
        }

        private AudioHandle GetNewHandle()
        {
            uint candidate = nextHandleId;
            int attempts = 0;

            while (attempts <= activeSources.Count)
            {
                if (candidate == 0) candidate = 1;

                var handle = new AudioHandle(candidate);
                if (!activeSources.ContainsKey(handle))
                {
                    nextHandleId = candidate + 1;
                    return handle;
                }

                candidate++;
                attempts++;
            }

            throw new InvalidOperationException(
                "Ran out of audio handles. This should never happen unless you have more than 4 billion simultaneous audio instances.");
        }

        /// <summary>Cleans up finished sources each frame.</summary>
        private void LateUpdate()
        {
            if (activeSources.Count == 0) return;

            cleanupHandles.Clear();
            foreach (var (handle, source) in activeSources)
            {
                if (source == null)
                {
                    cleanupHandles.Add(handle);
                    continue;
                }

                if (!source.isPlaying && !pausedHandles.Contains(handle) && !loopingHandles.Contains(handle))
                    cleanupHandles.Add(handle);
            }

            foreach (var handle in cleanupHandles)
            {
                if (!activeSources.Remove(handle, out var source)) continue;
                pausedHandles.Remove(handle);
                loopingHandles.Remove(handle);
                RemoveFromCategory(handle);
                if (source != null) Destroy(source.gameObject);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            audioBusMap = Resources.Load<AudioBusMap>("Audio/AudioBusMap");
            mixer = audioBusMap.GetMixer();
        }

        private void OnEnable()
        {
            OnEnableAudioManager?.Invoke();
        }
    }
}
