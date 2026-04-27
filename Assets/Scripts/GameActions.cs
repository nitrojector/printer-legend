using Config;
using Data;
using Desktop.WindowSystem;
using Printer;
using UnityEngine;
using WindowContents;

public class GameActions : MonoBehaviour
{
	public static GameActions Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		// Preload WindowManager
		_ = WindowManager.Instance;
	}

	public void OpenProgressionPrint()
	{
		int idx = UserSave.Instance.ProgressionNextPrintIdx;
		int totalLevels = LevelSequenceConfig.Instance.Levels.Count;

		if (idx >= totalLevels)
		{
			WindowManager.Instance.Launch<ConfirmationPopupWindowContent>((w, c) =>
			{
				w.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
				c.Title = "All Complete!";
				c.Message = "You've completed all available drawings. Check the gallery to see your work!";
				c.ConfirmText = "OK";
				c.SetAllowCancel(false);
				c.OnConfirm += OpenGallery;
			});
			return;
		}

		var levelEntry = LevelSequenceConfig.Instance.Levels[idx];
		var refSprite = PrinterViewWindowContent.GetReferenceSprite(idx);

		WindowManager.Instance.Launch<PrinterViewWindowContent>((w, c) =>
		{
			w.SetPositionNormalized(new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f));
			c.ApplyLevel(levelEntry);
			c.SetProgressionMode(true);
		});

		WindowManager.Instance.Launch<PrinterReferenceWindowContent>((w, c) =>
		{
			w.SetPositionNormalized(new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f));
			c.SetReferenceSprite(refSprite);
		});
	}

	public void OpenFreePrint()
	{
		WindowManager.Instance.Launch<PrinterViewWindowContent>((w, _) =>
		{
			w.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
		});
	}

	public void OpenGallery()
	{
		WindowManager.Instance.Launch<GalleryWindowContent>();
	}

	public void OpenSettings()
	{
		// TODO: low priority
	}

	public void ExitGame()
	{
		WindowManager.Instance.Launch<ConfirmationPopupWindowContent>((window, content) =>
		{
			window.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
			content.Title = "Exit Game";
			content.Message = "Are you sure you want to exit the game?";
			content.ConfirmText = "Exit";
			content.CancelText = "Cancel";
			content.OnConfirm += ExitGameForReal;
		});
	}

	private void ExitGameForReal()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
