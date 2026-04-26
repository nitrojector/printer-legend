using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Utility
{
	public static class Logr
	{
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Info(object message) => UnityEngine.Debug.Log($"<color=cyan>[INFO]</color> {message}");
		
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Info(object message, Object context) => UnityEngine.Debug.Log($"<color=cyan>[INFO]</color> {message}", context);
		
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Warn(object message) => UnityEngine.Debug.LogWarning($"<color=yellow>[WARNING]</color> {message}");
		
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Warn(object message, Object context) => UnityEngine.Debug.LogWarning($"<color=yellow>[WARNING]</color> {message}", context);
		
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Error(object message) => UnityEngine.Debug.LogError($"<color=red>[ERROR]</color> {message}");
		
		[Conditional("UNITY_EDITOR")] [Conditional("DEVELOPMENT_BUILD")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Error(object message, Object context) => UnityEngine.Debug.LogError($"<color=red>[ERROR]</color> {message}", context);
	}
}