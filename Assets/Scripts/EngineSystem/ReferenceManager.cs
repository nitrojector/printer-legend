using UnityEngine;
using UnityEngine.InputSystem;
using Utility;

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
	}
}