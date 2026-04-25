using System;
using Desktop.WindowSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utility;

namespace WindowContents
{
	public class MainMenuWindowContent : WindowContent
	{
		[Header("Main Menu Settings")]
		[SerializeField] private Button pictureModeButton;
		[SerializeField] private Button galleryButton;
		[SerializeField] private Button exitButton;

		private void Awake()
		{
			if (pictureModeButton != null)
				pictureModeButton.onClick.AddListener(OnPictureMode);
			if (galleryButton != null)
				galleryButton.onClick.AddListener(OnGallery);
			if (exitButton != null)
				exitButton.onClick.AddListener(OnExit);
			Debug.Log("Main Menu Initialized");
		}

		private void OnPictureMode()
		{
			SceneManager.LoadScene("Printing", LoadSceneMode.Single);
			Logr.Info("MainMenu: Picture Mode");
		}

		private void OnGallery()
		{
			SceneManager.LoadScene("Printing", LoadSceneMode.Single);
			Logr.Info("MainMenu: Gallery");
		}

		private void OnExit()
		{
			SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
			Logr.Info("MainMenu: Exit");
		}
	}
}