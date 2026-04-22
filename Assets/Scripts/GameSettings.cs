using System.IO;
using UnityEngine;
using Utility;

namespace DefaultNamespace
{
	public class GameSettings : PersistentConfig<GameSettings>
	{
		public override string ConfigPath =>
			Path.Combine(Application.persistentDataPath, "settings.json");
	}
}