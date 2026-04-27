using Desktop.WindowSystem;
using UnityEditor;
using UnityEngine;

namespace Editor.WindowSystem
{
	[CustomEditor(typeof(Window))]
	public class WindowEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var window = (Window)target;
			var content = window.GetComponentInChildren<WindowContent>();

			if (content != null)
			{
				EditorGUILayout.HelpBox(
					$"Minimize/maximize buttons are shown only when both the window's own setting and " +
					$"{content.GetType().Name}'s AllowMinimize/AllowMaximize are enabled.",
					MessageType.Info);
			}

			DrawDefaultInspector();

			if (content != null)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Content Size Constraints", EditorStyles.boldLabel);

				var contentSO = new SerializedObject(content);
				contentSO.Update();

				if (content.EnforceMaxSize && serializedObject.FindProperty("maximizeEnabled").boolValue)
				{
					EditorGUILayout.HelpBox(
						"Enabling Maximize for Window is invalid when EnforceMaxSize is enabled on child WindowContent",
						MessageType.Error);
				}

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Use Current Content Size as Min"))
				{
					var rt = content.GetComponent<RectTransform>();
					contentSO.FindProperty("minContentSize").vector2Value = rt.rect.size;
					contentSO.FindProperty("<EnforceMinSize>k__BackingField").boolValue = true;
					contentSO.ApplyModifiedProperties();
				}
				if (GUILayout.Button("Use Current Content Size as Max"))
				{
					var rt = content.GetComponent<RectTransform>();
					contentSO.FindProperty("maxContentSize").vector2Value = rt.rect.size;
					contentSO.FindProperty("enforceMaxSize").boolValue = true;
					contentSO.ApplyModifiedProperties();
				}
				EditorGUILayout.EndHorizontal();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
