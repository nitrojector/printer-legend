using System;
using Desktop.WindowSystem;
using Printer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utility;

namespace WindowContents
{
	public class MainMenuWindowContent : WindowContent
	{
		public override string WindowTitle => "Main Menu";

		public override bool AllowMaximize => false;

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

		public override bool OnQuit()
		{
			// TODO: check for confirmation to quit, maybe?
			OnExit();
			return true;
		}

		private void OnPictureMode()
		{
			LevelManager.ResetLevelIndex();
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