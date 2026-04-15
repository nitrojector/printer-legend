using System.Collections;
using UnityEngine;

namespace AudioSystem
{
    public class AudioSystemTest : MonoBehaviour
    {
        public AudioClip musicClip;
        public AudioClip sfxClip;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        /// <summary>Runs a small sequence of runtime checks over several seconds.</summary>
        private IEnumerator RunTests()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                Debug.LogError("no instance");
                yield break;
            }

            if (musicClip == null || sfxClip == null)
            {
                Debug.LogError("clip invalid");
            }

            StartCoroutine(ModulateVolume());

            Debug.Assert(audioManager.PlayManaged(musicClip, transform, AudioBus.Music, out var musichand, true),
                "loop");
            yield return new WaitForSeconds(Mathf.Min(0.2f, musicClip.length * 0.25f));

            Debug.Assert(audioManager.PlayManaged(sfxClip, transform.position, AudioBus.SFX, out var sfxhand),
                "managed one-shot play");
            yield return new WaitForSeconds(0.2f);
            Debug.Assert(audioManager.IsPlaying(sfxhand), "sfx should be playing");

            bool paused = audioManager.Pause(sfxhand);
            Debug.Assert(paused, "paused");
            yield return new WaitForSeconds(0.2f);
            Debug.Assert(!audioManager.IsPlaying(sfxhand), "not playing when paused");

            bool resumed = audioManager.Resume(sfxhand);
            Debug.Assert(resumed, "resumed");
            yield return new WaitForSeconds(0.2f);
            Debug.Assert(audioManager.IsPlaying(sfxhand), "resume, playing");

            bool stopped = audioManager.Stop(sfxhand);
            Debug.Assert(stopped, "stop");
            yield return new WaitForSeconds(0.2f);
            Debug.Assert(!audioManager.IsPlaying(sfxhand), "!audioManager.IsPlaying(sfxhand)");

            Debug.Assert(!audioManager.Stop(new AudioHandle(12312312)), "audioManager.Stop(new AudioHandle(12312312))");

            Debug.Log("completed");
        }

        private IEnumerator ModulateVolume()
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time) + 1f) / 2f;
                AudioManager.Instance.SetVolume(AudioBus.Music, t);
                yield return null;
            }
        }
    }
}
