using Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameMaster.Scripts
{
	public class GameMasterHost
	{
		private static GameMasterHost _instance;
			
		private InputActionAsset actions;
		private GameObject gmPanelPrefab;

		private InputAction toggleAction;
		
		private GameObject gmViewInstance;
		private GameMasterUI gmViewUI;
		
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			if (_instance != null)
			{
				_instance.toggleAction.performed -= _instance.OnToggle;
				_instance.actions.Disable();
				_instance = null;
			}
			_instance = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Init()
		{
			if (_instance != null) return;

			if (DeveloperSettings.Instance.EnableGameMaster)
			{
				_instance = new GameMasterHost
				{
					actions = Resources.Load<InputActionAsset>("GameMasterActions"),
					gmPanelPrefab = Resources.Load<GameObject>("GameMasterView")
				};
				_instance.Setup();
			}
		}

		private void Setup()
		{
			gmViewInstance = Object.Instantiate(gmPanelPrefab);
			gmViewUI = gmViewInstance.GetComponent<GameMasterUI>();
			Object.DontDestroyOnLoad(gmViewInstance);
			
			toggleAction = actions.FindAction("Global/Toggle");
			if (toggleAction == null)
			{
				Debug.LogError("ToggleGMPanel action not found in GM input asset.");
				return;
			}

			toggleAction.performed += OnToggle;
			actions.Enable();
		}

		private void OnToggle(InputAction.CallbackContext ctx)
		{
			if (gmViewUI == null) return;
			gmViewUI.ToggleActive();
		}
	}
}