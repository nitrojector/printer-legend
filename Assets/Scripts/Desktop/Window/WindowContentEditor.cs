using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Desktop.Window
{
	[CustomEditor(typeof(WindowContent), editorForChildClasses: true)]
	public class WindowContentEditor : Editor
	{
		private static readonly HashSet<string> BaseFieldNames = new();
		private static bool BaseFieldsFoldout
		{
			get => EditorPrefs.GetBool("WindowContentEditor.Foldout", true);
			set => EditorPrefs.SetBool("WindowContentEditor.Foldout", value);
		}
		
		static WindowContentEditor()
		{
			foreach (var field in typeof(WindowContent).GetFields(
				         System.Reflection.BindingFlags.Instance |
				         System.Reflection.BindingFlags.NonPublic |
				         System.Reflection.BindingFlags.Public))
			{
				BaseFieldNames.Add(field.Name);
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var window = (WindowContent)target;

			// Subclass fields first
			var iterator = serializedObject.GetIterator();
			iterator.NextVisible(true);
			while (iterator.NextVisible(false))
			{
				if (BaseFieldNames.Contains(iterator.name)) continue;
				EditorGUILayout.PropertyField(iterator, true);
			}

			EditorGUILayout.Space();
			BaseFieldsFoldout = EditorGUILayout.Foldout(BaseFieldsFoldout, "Window Content Settings", toggleOnLabelClick: true);
			if (BaseFieldsFoldout)
			{
				EditorGUI.indentLevel++;

				iterator = serializedObject.GetIterator();
				iterator.NextVisible(true);
				while (iterator.NextVisible(false))
				{
					if (!BaseFieldNames.Contains(iterator.name)) continue;
					EditorGUILayout.PropertyField(iterator, true);
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Use Current Content Size as Min"))
				{
					var rt = window.GetComponent<RectTransform>();
					serializedObject.FindProperty("minContentSize").vector2Value = rt.rect.size;
					serializedObject.FindProperty("enforceMinSize").boolValue = true;
				}
				if (GUILayout.Button("Use Current Content Size as Max"))
				{
					var rt = window.GetComponent<RectTransform>();
					serializedObject.FindProperty("maxContentSize").vector2Value = rt.rect.size;
					serializedObject.FindProperty("enforceMaxSize").boolValue = true;
				}
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif