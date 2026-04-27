using System;
using AudioSystem;
using Data;
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
			
			// apply user settings =============================================
			
			foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
			{
				AudioManager.Instance.SetVolume(bus, UserSettings.Instance.Volumes[bus]);
			}
		}
	}
}