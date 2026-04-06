#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SceneManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="SceneManager.Runtime.SceneManager"/>.
    /// Adds runtime scene-load controls and transition progress display.
    /// </summary>
    [CustomEditor(typeof(SceneManager.Runtime.SceneManager))]
    public class SceneManagerEditor : UnityEditor.Editor
    {
        private string _loadById   = "";
        private string _loadByName = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mgr = (SceneManager.Runtime.SceneManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use runtime controls.", MessageType.Info);
                return;
            }

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Current Scene", mgr.CurrentSceneId ?? "(none)");
            EditorGUILayout.Slider("Load Progress", mgr.LoadProgress, 0f, 1f);
            EditorGUILayout.Toggle("Is Transitioning", mgr.IsTransitioning);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            // Load by Id
            EditorGUILayout.LabelField("Load Scene by Id", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _loadById = EditorGUILayout.TextField("Scene Id", _loadById);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_loadById) || mgr.IsTransitioning);
            if (GUILayout.Button("Load", GUILayout.Width(60)))
                mgr.LoadScene(_loadById);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // Load by Unity scene name
            EditorGUILayout.LabelField("Load Scene by Name (direct)", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _loadByName = EditorGUILayout.TextField("Scene Name", _loadByName);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_loadByName) || mgr.IsTransitioning);
            if (GUILayout.Button("Load", GUILayout.Width(60)))
                mgr.LoadSceneByName(_loadByName);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // History
            EditorGUILayout.LabelField("History", EditorStyles.miniBoldLabel);
            if (mgr.History.Count == 0)
            {
                EditorGUILayout.HelpBox("No history yet.", MessageType.None);
            }
            else
            {
                for (int i = mgr.History.Count - 1; i >= 0; i--)
                    EditorGUILayout.LabelField($"  [{i}]", mgr.History[i]);
            }

            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(mgr.IsTransitioning || mgr.History.Count < 2);
            if (GUILayout.Button("Go Back"))
                mgr.GoBack();
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
