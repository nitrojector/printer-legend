using Desktop.WindowSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using Utility;
using WindowContents;

namespace EngineSystem
{
	[CreateAssetMenu(fileName = "ReferenceManager", menuName = "ScriptableObjects/ReferenceManager", order = 1)]
	public class ReferenceManager : ScriptableObject
	{
		private const string ResourcePath = "ReferenceManager";
		
		private static ReferenceManager _instance;

		public static ReferenceManager Instance => _instance ??= Load();

		private static ReferenceManager Load()
		{
			var instance = Resources.Load<ReferenceManager>(ResourcePath);
			if (instance == null)
			{
				Logr.Error($"Failed to load ReferenceManager from Resources/{ResourcePath}. Please ensure it exists and is of type ReferenceManager.");
			}
			return instance;
		}
		
		[Header("Input Actions")]
		public InputActionAsset inputActions;

		[Header("Prefabs")]
		public Window windowPrefab;
		public Gallery.GalleryEntryUI galleryEntryPrefab;
		
		[Header("Cursor")]
		public Texture2D cursorDefault;

		[Header("Resize Cursors")]
		public Texture2D cursorResizeH;
		public Texture2D cursorResizeV;
		public Texture2D cursorResizeDiagNE;
		public Texture2D cursorResizeDiagNW;
	}
}