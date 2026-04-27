using System.IO;
using UnityEngine;
using Utility;

public class UserSettings : PersistentConfig<UserSettings>
{
	public override string ConfigPath =>
		Path.Combine(Application.persistentDataPath, "settings.json");
}