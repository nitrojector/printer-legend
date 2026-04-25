using EngineSystem;
using UnityEngine;

namespace Setup
{
	public class GameSetup
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			ReferenceManager.Instance.inputActions.Enable();
		}
	}
}