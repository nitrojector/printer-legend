using System.Collections.Generic;
using Desktop.WindowSystem;
using EngineSystem;
using UnityEditor;
using UnityEngine;

namespace Editor.WindowSystem
{
    public class CreateWindowTool : EditorWindow
    {
        private List<GameObject> _contentPrefabs = new();
        private string[]         _contentNames   = System.Array.Empty<string>();
        private int              _selectedIndex  = 0;

        [MenuItem("GameObject/Window System/Create Empty Window", false, 11)]
        public static void CreateEmpty()
        {
            var windowPrefab = FindWindowPrefab();
            if (windowPrefab == null)
            {
                Debug.LogError("CreateWindowTool: could not find Window prefab via ReferenceManager.");
                return;
            }

            var windowGO = (GameObject)PrefabUtility.InstantiatePrefab(windowPrefab.gameObject, Selection.activeTransform);
            Undo.RegisterCreatedObjectUndo(windowGO, "Create Empty Window");
            Selection.activeGameObject = windowGO;
            EditorGUIUtility.PingObject(windowGO);
        }

        [MenuItem("GameObject/Window System/Create Window", false, 10)]
        public static void Open()
        {
            var window = GetWindow<CreateWindowTool>(true, "Create Window", true);
            window.minSize = new Vector2(320, 120);
            window.Refresh();
        }

        private void Refresh()
        {
            _contentPrefabs.Clear();

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/WindowContent" });
            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<WindowContent>() != null)
                    _contentPrefabs.Add(prefab);
            }

            _contentPrefabs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            _contentNames = new string[_contentPrefabs.Count];
            for (int i = 0; i < _contentPrefabs.Count; i++)
                _contentNames[i] = _contentPrefabs[i].name;

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _contentPrefabs.Count - 1));
        }

        private void OnGUI()
        {
            if (_contentPrefabs.Count == 0)
            {
                EditorGUILayout.HelpBox("No WindowContent prefabs found under Assets/Resources/WindowContent.", MessageType.Warning);
                if (GUILayout.Button("Refresh")) Refresh();
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Content", EditorStyles.boldLabel);
            _selectedIndex = EditorGUILayout.Popup(_selectedIndex, _contentNames);

            EditorGUILayout.Space(6);

            using (new EditorGUI.DisabledScope(!Application.isPlaying && FindWindowPrefab() == null))
            {
                if (GUILayout.Button("Create"))
                    CreateWindow();
            }

            if (FindWindowPrefab() == null)
                EditorGUILayout.HelpBox("Window prefab not found in ReferenceManager. Make sure the asset exists at Resources/ReferenceManager.", MessageType.Error);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Refresh List")) Refresh();
        }

        private void CreateWindow()
        {
            var windowPrefab = FindWindowPrefab();
            if (windowPrefab == null)
            {
                Debug.LogError("CreateWindowTool: could not find Window prefab via ReferenceManager.");
                return;
            }

            var contentPrefab = _contentPrefabs[_selectedIndex];

            // Determine parent: selected object in hierarchy, or scene root
            Transform parent = Selection.activeTransform;

            var windowGO = (GameObject)PrefabUtility.InstantiatePrefab(windowPrefab.gameObject, parent);
            Undo.RegisterCreatedObjectUndo(windowGO, "Create Window");

            // Find the contentContainer child via serialized property
            var so = new SerializedObject(windowGO.GetComponent<Window>());
            var containerProp = so.FindProperty("contentContainer");
            var containerGO   = containerProp?.objectReferenceValue as GameObject;

            if (containerGO != null)
            {
                var contentGO = (GameObject)PrefabUtility.InstantiatePrefab(contentPrefab, containerGO.transform);
                Undo.RegisterCreatedObjectUndo(contentGO, "Create Window Content");
            }
            else
            {
                Debug.LogWarning("CreateWindowTool: could not locate contentContainer on Window prefab; content not parented.");
            }

            Selection.activeGameObject = windowGO;
            EditorGUIUtility.PingObject(windowGO);
        }

        private static Window FindWindowPrefab()
        {
            var guids = AssetDatabase.FindAssets("ReferenceManager t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var rm   = AssetDatabase.LoadAssetAtPath<ReferenceManager>(path);
                if (rm != null && rm.windowPrefab != null) return rm.windowPrefab;
            }
            return null;
        }
    }
}
