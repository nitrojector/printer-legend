using UnityEngine;

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
	}

	public void OpenProgressionPrint()
	{
			
	}

	public void OpenFreePrint()
	{
			
	}

	public void OpenGallery()
	{
			
	}

	public void OpenSettings()
	{
		
	}

	public void ExitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
	}
}