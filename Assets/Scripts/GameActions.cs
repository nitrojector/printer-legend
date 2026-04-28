using Config;
using Data;
using Desktop.WindowSystem;
using UnityEngine;
using WindowContents;

// TODO: refactor
public class GameActions : MonoBehaviour
{
	public static GameActions Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
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
				c.ConfirmButtonText = "OK";
				c.SetAllowCancel(false);
				c.OnConfirm += OpenGallery;
			});
			return;
		}

		if (GameMgr.Instance.ProgressionLinkedPrintViewCount > 0)
		{
			WindowManager.Instance.Launch<ConfirmationPopupWindowContent>((w, c) =>
			{
				w.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
				c.Title = "Print In Progress";
				c.Message = "You already have a print in progress for progression.";
				c.ConfirmButtonText = "OK";
				c.SetAllowCancel(false);
			});
			return;
		}

		var levelEntry = LevelSequenceConfig.Instance.Levels[idx];
		var refSprite = PrinterViewWindowContent.GetReferenceSprite(idx);

		var printerView = WindowManager.Instance.Launch<PrinterViewWindowContent>((w, c) =>
		{
			w.SetPositionNormalized(new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f));
			c.ApplyLevel(levelEntry);
			c.SetProgressionMode(true);
		});

		WindowManager.Instance.Launch<PrinterReferenceWindowContent>((w, c) =>
		{
			w.SetPositionNormalized(new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f));
			c.SetPrintViewId(printerView.PrintViewId);
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
		var galleryWc = WindowManager.Instance.GetFirstWindowOfType<GalleryWindowContent>();
		if (galleryWc != null)
		{
			WindowManager.Instance.BringToFront(galleryWc);
			return;
		}
		
		WindowManager.Instance.Launch<GalleryWindowContent>();
	}

	public void OpenSettings()
	{
		var settingsWc = WindowManager.Instance.GetFirstWindowOfType<SettingsWindowContent>();
		if (settingsWc != null)
		{
			WindowManager.Instance.BringToFront(settingsWc);
			return;
		}
		
		WindowManager.Instance.Launch<SettingsWindowContent>();
	}

	public void ExitGame()
	{
		WindowManager.Instance.Launch<ConfirmationPopupWindowContent>((window, content) =>
		{
			window.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
			content.Title = "Exit Game";
			content.Message = "Are you sure you want to exit the game?";
			content.ConfirmButtonText = "Exit";
			content.CancelButtonText = "Cancel";
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
