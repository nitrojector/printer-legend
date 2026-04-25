using System.Diagnostics;

namespace Utility
{
	public static class Logr
	{
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		public static void Info(string message) => UnityEngine.Debug.Log($"<color=cyan>[INFO]</color> {message}");
		
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		public static void Warning(string message) => UnityEngine.Debug.LogWarning($"<color=yellow>[WARNING]</color> {message}");
		
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		public static void Error(string message) => UnityEngine.Debug.LogError($"<color=red>[ERROR]</color> {message}");
	}
}