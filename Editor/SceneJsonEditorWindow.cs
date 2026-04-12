#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SceneManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace SceneManager.Editor
{
    // ────────────────────────────────────────────────────────────────────────────
    // Scene Definitions JSON Editor Window
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Editor window for creating and editing <c>scenes.json</c> in StreamingAssets.
    /// Open via <b>JSON Editors → Scene Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class SceneJsonEditorWindow : EditorWindow
    {
        private const string JsonFolderName   = "scenes";
        private const string JsonSaveFileName = "scenes.json";

        private SceneDefinitionEditorBridge _bridge;
        private UnityEditor.Editor          _bridgeEditor;
        private Vector2                     _scroll;
        private string                      _status;
        private bool                        _statusError;

        [MenuItem("JSON Editors/Scene Manager")]
        public static void ShowWindow() =>
            GetWindow<SceneJsonEditorWindow>("Scene Definitions JSON");

        private void OnEnable()
        {
            _bridge = CreateInstance<SceneDefinitionEditorBridge>();
            Load();
        }

        private void OnDisable()
        {
            if (_bridgeEditor != null) DestroyImmediate(_bridgeEditor);
            if (_bridge      != null) DestroyImmediate(_bridge);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusError ? MessageType.Error : MessageType.Info);

            if (_bridge == null) return;
            if (_bridgeEditor == null)
                _bridgeEditor = UnityEditor.Editor.CreateEditor(_bridge);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _bridgeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                $"StreamingAssets/{JsonFolderName}/",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            string folderPath = Path.Combine(Application.streamingAssetsPath, JsonFolderName);
            try
            {
                var list = new List<SceneDefinition>();
                if (Directory.Exists(folderPath))
                {
                    foreach (var file in Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var w = JsonUtility.FromJson<SceneDefinitionEditorWrapper>(File.ReadAllText(file));
                        if (w?.scenes != null) list.AddRange(w.scenes);
                    }
                }
                else
                {
                    Directory.CreateDirectory(folderPath);
                    File.WriteAllText(Path.Combine(folderPath, JsonSaveFileName), JsonUtility.ToJson(new SceneDefinitionEditorWrapper(), true));
                    AssetDatabase.Refresh();
                }
                _bridge.scenes = list;
                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }
                _status = $"Loaded {list.Count} scenes from {JsonFolderName}/.";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Load error: {e.Message}"; _statusError = true; }
        }

        private void Save()
        {
            try
            {
                string folderPath = Path.Combine(Application.streamingAssetsPath, JsonFolderName);
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var w = new SceneDefinitionEditorWrapper { scenes = _bridge.scenes.ToArray() };
                var path = Path.Combine(folderPath, JsonSaveFileName);
                File.WriteAllText(path, JsonUtility.ToJson(w, true));
                AssetDatabase.Refresh();
                _status = $"Saved {_bridge.scenes.Count} scenes to {JsonFolderName}/{JsonSaveFileName}.";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Save error: {e.Message}"; _statusError = true; }
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class SceneDefinitionEditorBridge : ScriptableObject
    {
        public List<SceneDefinition> scenes = new List<SceneDefinition>();
    }

    // ── Local wrapper mirrors the private SceneDefinitionList ────────────────
    [Serializable]
    internal class SceneDefinitionEditorWrapper
    {
        public SceneDefinition[] scenes = Array.Empty<SceneDefinition>();
    }
}
#endif
