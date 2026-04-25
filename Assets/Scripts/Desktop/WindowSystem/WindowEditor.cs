using UnityEditor;
using UnityEngine;

namespace Desktop.WindowSystem
{
	[CustomEditor(typeof(Window))]
	public class WindowEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawDefaultInspector();

			var window = (Window)target;
			var content = window.GetComponentInChildren<WindowContent>();

			if (content != null)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Content Size Constraints", EditorStyles.boldLabel);

				var contentSO = new SerializedObject(content);
				contentSO.Update();

				if (content.EnforceMaxSize && serializedObject.FindProperty("maximizeEnabled").boolValue)
				{
					EditorGUILayout.HelpBox("Enabling Maximize for Window is invalid when EnforceMaxSize is enabled on child WindowContent",
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
	}}