using System.Collections.Generic;
using System.Reflection;
using Desktop.WindowSystem;
using UnityEditor;
using UnityEngine;

namespace Editor.WindowSystem
{
	[CustomEditor(typeof(WindowContent), editorForChildClasses: true)]
	public class WindowContentEditor : UnityEditor.Editor
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
				         BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
				BaseFieldNames.Add(field.Name);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var content = (WindowContent)target;
			var type = content.GetType();
			bool maximizeOverridden = IsGetterOverridden(type, nameof(WindowContent.AllowMaximize));
			bool minimizeOverridden = IsGetterOverridden(type, nameof(WindowContent.AllowMinimize));

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

				if (maximizeOverridden || minimizeOverridden)
				{
					string which = (maximizeOverridden && minimizeOverridden)
						? "AllowMaximize and AllowMinimize"
						: maximizeOverridden ? "AllowMaximize" : "AllowMinimize";
					string verb = (maximizeOverridden && minimizeOverridden) ? "are" : "is";
					EditorGUILayout.HelpBox(
						$"{which} {verb} overridden by {type.Name}. " +
						"The serialized field(s) reflect the effective value and will revert if edited.",
						MessageType.Info);
				}

				iterator = serializedObject.GetIterator();
				iterator.NextVisible(true);
				while (iterator.NextVisible(false))
				{
					if (!BaseFieldNames.Contains(iterator.name)) continue;

					bool disabled = (iterator.name == "allowMaximize" && maximizeOverridden)
					             || (iterator.name == "allowMinimize" && minimizeOverridden);
					using (new EditorGUI.DisabledScope(disabled))
						EditorGUILayout.PropertyField(iterator, true);
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Use Current Content Size as Min"))
				{
					var rt = content.GetComponent<RectTransform>();
					serializedObject.FindProperty("minContentSize").vector2Value = rt.rect.size;
					serializedObject.FindProperty("<EnforceMinSize>k__BackingField").boolValue = true;
				}
				if (GUILayout.Button("Use Current Content Size as Max"))
				{
					var rt = content.GetComponent<RectTransform>();
					serializedObject.FindProperty("maxContentSize").vector2Value = rt.rect.size;
					serializedObject.FindProperty("enforceMaxSize").boolValue = true;
				}
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}

		private static bool IsGetterOverridden(System.Type type, string propertyName)
		{
			var baseGetter = typeof(WindowContent)
				.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
				?.GetGetMethod();
			if (baseGetter == null) return false;
			var overrideGetter = type
				.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
				?.GetGetMethod();
			return overrideGetter != null && overrideGetter.DeclaringType != typeof(WindowContent);
		}
	}
}
