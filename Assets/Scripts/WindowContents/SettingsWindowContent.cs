using System;
using AudioSystem;
using Config;
using Data;
using Desktop.WindowSystem;
using Printer;
using TMPro;
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
		
		[Header("Progression")]
		[SerializeField] private TMP_Text progressionStatusText;
		[SerializeField] private Button resetProgressionButton;

		private void Awake()
		{
			masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
			musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
			sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
			
			resetProgressionButton.onClick.AddListener(ResetProgression);
		}
		
		private void OnEnable()
		{
			masterVolumeSlider.value = UserSettings.Instance.Volumes[AudioBus.Master];
			musicVolumeSlider.value = UserSettings.Instance.Volumes[AudioBus.Music];
			sfxVolumeSlider.value = UserSettings.Instance.Volumes[AudioBus.SFX];
			
			UpdateProgressionStatus();
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

		private void UpdateProgressionStatus()
		{
			int idx = UserSave.Instance.ProgressionNextPrintIdx;
			if (idx >= LevelSequenceConfig.Instance.Levels.Count)
			{
				progressionStatusText.text = "Completed";
			}
			else
			{
				progressionStatusText.text = $"Next Print ID: {UserSave.Instance.ProgressionNextPrintIdx}";
			}
			resetProgressionButton.interactable = idx > 0;
		}

		private void ResetProgression()
		{
			WindowManager.Instance.Launch<ConfirmationPopupWindowContent>((_, c) =>
			{
				c.Message = "Are you sure you want to reset your progression? This cannot be undone. " +
				            "Your prints will not be deleted, but levels will need to be replayed.";
				c.ConfirmButtonText = "Reset";
				c.CancelButtonText = "Cancel";
				c.OnConfirm += ResetProgressionForReal;
			});
		}

		private void ResetProgressionForReal()
		{
			UserSave.Instance.ProgressionNextPrintIdx = 0;
			UpdateProgressionStatus();
		}
	}
}