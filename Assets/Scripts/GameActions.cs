using Desktop.WindowSystem;
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
	}

	public void OpenFreePrint()
	{
		WindowManager.Instance.Launch<PrinterViewWindowContent>();
	}

	public void OpenGallery()
	{
		WindowManager.Instance.Launch<GalleryWindowContent>();
	}

	public void OpenSettings()
	{
		// TODO: does nothing, low priority
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