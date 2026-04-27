using System;
using AudioSystem;
using Data;
using Desktop.WindowSystem;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
	public class SettingsWindowContent : WindowContent
	{
		public override string WindowTitle => "Settings";

		[Header("Audio Settings")]
		[SerializeField] private Slider masterVolumeSlider;
		[SerializeField] private Slider musicVolumeSlider;
		[SerializeField] private Slider sfxVolumeSlider;

		private void Awake()
		{
			masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
			musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
			sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
		}
		
		private void OnEnable()
		{
			masterVolumeSlider.value = UserSettings.Instance.Volumes[AudioBus.Master];
			musicVolumeSlider.value = UserSettings.Instance.Volumes[AudioBus.Music];
			sfxVolumeSlider.value = UserSettings.Instance.Volumes[AudioBus.SFX];
		}
		
		private void OnMasterVolumeChanged(float value)
		{
			UserSettings.Instance.Volumes[AudioBus.Master] = value;
			AudioManager.Instance.SetVolume(AudioBus.Master, value);
		}
		
		private void OnMusicVolumeChanged(float value)
		{
			UserSettings.Instance.Volumes[AudioBus.Music] = value;
			AudioManager.Instance.SetVolume(AudioBus.Music, value);
		}
		
		private void OnSFXVolumeChanged(float value)
		{
			UserSettings.Instance.Volumes[AudioBus.SFX] = value;
			AudioManager.Instance.SetVolume(AudioBus.SFX, value);
		}
	}
}