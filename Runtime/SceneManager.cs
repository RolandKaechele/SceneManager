using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SceneManager.Runtime
{
    /// <summary>
    /// <b>SceneManager</b> is the central orchestrator for scene loading, transitions, and organization.
    ///
    /// <para><b>Responsibilities:</b>
    /// <list type="number">
    ///   <item>Register scene definitions (id → Unity scene name) from the Inspector and optional JSON.</item>
    ///   <item>Load scenes by id with async + progress tracking, fade-in/out, and load-screen integration.</item>
    ///   <item>Support Single, Additive, and AdditiveActive load modes.</item>
    ///   <item>Maintain a scene history stack for back-navigation.</item>
    ///   <item>Expose delegate hooks for optional bridge components.</item>
    ///   <item>Support JSON-authored scene definitions for modding.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Modding / JSON:</b> Enable <c>loadFromJson</c> and place a
    /// <c>scenes.json</c> in <c>StreamingAssets/</c>.
    /// JSON entries are <b>merged by id</b>: JSON overrides Inspector entries with the same id.</para>
    ///
    /// <para><b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>SCENEMANAGER_MLF</c>  — MapLoaderFramework: trigger scene transitions on chapter/map load events.</item>
    ///   <item><c>SCENEMANAGER_STM</c>  — StateManager: block transitions while non-Gameplay states are active.</item>
    ///   <item><c>SCENEMANAGER_GM</c>   — GameManager: notify GameManager when scene changes are requested.</item>
    ///   <item><c>SCENEMANAGER_LSM</c>  — LoadScreenManager: show configured load screen during async loads.</item>
    ///   <item><c>SCENEMANAGER_EM</c>   — EventManager: fire <c>scene.loading</c>, <c>scene.loaded</c>, <c>scene.unloaded</c> events.</item>
    ///   <item><c>SCENEMANAGER_AM</c>   — AudioManager: crossfade to the scene's configured audio track after load.</item>
    ///   <item><c>SCENEMANAGER_DOTWEEN</c> — DOTween Pro: drive fade canvas tweens for transitions instead of coroutines.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("SceneManager/Scene Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class SceneManager : SerializedMonoBehaviour
#else
    public class SceneManager : MonoBehaviour
#endif
    {
        // ─── Inspector ───────────────────────────────────────────────────────────

        [Header("Scene Definitions")]
        [Tooltip("Built-in scene definitions. JSON entries are merged on top by id.")]
        [SerializeField] private List<SceneDefinition> scenes = new List<SceneDefinition>();

        [Header("Transition")]
        [Tooltip("CanvasGroup used for fade-out / fade-in transitions. Leave null to skip screen fades.")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Tooltip("Default fade-out duration in seconds before loading a scene.")]
        [SerializeField] private float defaultFadeOutDuration = 0.4f;

        [Tooltip("Default fade-in duration in seconds after a scene has loaded.")]
        [SerializeField] private float defaultFadeInDuration = 0.4f;

        [Header("History")]
        [Tooltip("Maximum number of scene ids to keep in the navigation history.")]
        [SerializeField] private int maxHistoryDepth = 16;

        [Header("Modding / JSON")]
        [Tooltip("Merge additional scene definitions from StreamingAssets/<jsonPath> at startup.")]
        [SerializeField] private bool loadFromJson = false;

        [Tooltip("Path relative to StreamingAssets/ (e.g. 'scenes/' or 'scenes.json').")]
        [SerializeField] private string jsonPath = "scenes/";

        [Header("Debug")]
        [Tooltip("Log scene load/unload events to the Unity Console.")]
        [SerializeField] private bool verboseLogging = false;

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired just before async loading starts. Parameter: transition data.</summary>
        public event Action<SceneTransitionData> OnSceneLoading;

        /// <summary>Fired after the scene has loaded and the fade-in has started. Parameter: scene id.</summary>
        public event Action<string> OnSceneLoaded;

        /// <summary>Fired when a scene is unloaded (additive mode). Parameter: scene id.</summary>
        public event Action<string> OnSceneUnloaded;

        /// <summary>Async load progress (0–1) updated each frame during loading.</summary>
        public float LoadProgress { get; private set; }

        /// <summary>True while a scene transition is in progress.</summary>
        public bool IsTransitioning { get; private set; }

        // ─── Delegate hooks for bridge components ────────────────────────────────

        /// <summary>
        /// Invoked during a fade-out at the start of a transition.
        /// Signature: (transitionData, onComplete).
        /// If set, SceneManager skips its built-in CanvasGroup coroutine and calls onComplete when the
        /// bridge's tween/effect is done. Use with DOTween bridge.
        /// </summary>
        public Action<SceneTransitionData, Action> FadeOutOverride;

        /// <summary>
        /// Invoked during a fade-in at the end of a transition.
        /// Signature: (transitionData, onComplete).
        /// If set, SceneManager skips its built-in CanvasGroup coroutine and calls onComplete when done.
        /// </summary>
        public Action<SceneTransitionData, Action> FadeInOverride;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, SceneDefinition> _index =
            new Dictionary<string, SceneDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _history = new List<string>();

        /// <summary>Read-only scene navigation history (most recent first).</summary>
        public IReadOnlyList<string> History => _history;

        /// <summary>Id of the currently active scene (null if none tracked by SceneManager).</summary>
        public string CurrentSceneId { get; private set; }

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            BuildIndex();

            if (loadFromJson)
                LoadJsonDefinitions();
        }

        // ─── Scene registration ──────────────────────────────────────────────────

        /// <summary>
        /// Register or overwrite a scene definition at runtime.
        /// </summary>
        public void RegisterScene(SceneDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) return;
            _index[def.id] = def;
            if (verboseLogging)
                Debug.Log($"[SceneManager] Registered scene '{def.id}' → '{def.sceneName}'.");
        }

        /// <summary>Return the <see cref="SceneDefinition"/> for a given id, or null.</summary>
        public SceneDefinition GetDefinition(string id)
            => _index.TryGetValue(id, out var d) ? d : null;

        /// <summary>All registered scene ids.</summary>
        public IEnumerable<string> GetAllIds() => _index.Keys;

        // ─── Scene loading ────────────────────────────────────────────────────────

        /// <summary>
        /// Load a scene by id. The scene definition controls load mode, fade durations, and audio.
        /// </summary>
        /// <param name="sceneId">Id registered via Inspector or JSON.</param>
        /// <param name="fadeOutDuration">Override fade-out seconds. -1 uses the default/definition value.</param>
        /// <param name="fadeInDuration">Override fade-in seconds. -1 uses the default/definition value.</param>
        public void LoadScene(string sceneId, float fadeOutDuration = -1f, float fadeInDuration = -1f)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning($"[SceneManager] Transition already in progress — ignoring request to load '{sceneId}'.");
                return;
            }

            if (!_index.TryGetValue(sceneId, out var def))
            {
                Debug.LogWarning($"[SceneManager] No scene registered with id '{sceneId}'.");
                return;
            }

            float foOut = fadeOutDuration >= 0f ? fadeOutDuration : defaultFadeOutDuration;
            float foIn  = fadeInDuration  >= 0f ? fadeInDuration  : defaultFadeInDuration;

            var transition = new SceneTransitionData
            {
                fromSceneId     = CurrentSceneId,
                toSceneId       = sceneId,
                loadMode        = def.loadMode,
                fadeOutDuration = foOut,
                fadeInDuration  = foIn
            };

            StartCoroutine(DoLoadScene(def, transition));
        }

        /// <summary>
        /// Load a scene by its Unity Build-Settings scene name (bypasses the id registry).
        /// Useful for quick direct loads without a registered definition.
        /// </summary>
        public void LoadSceneByName(string sceneName, SceneLoadMode mode = SceneLoadMode.Single,
                                    float fadeOutDuration = -1f, float fadeInDuration = -1f)
        {
            var def = new SceneDefinition
            {
                id        = sceneName,
                sceneName = sceneName,
                loadMode  = mode
            };
            float foOut = fadeOutDuration >= 0f ? fadeOutDuration : defaultFadeOutDuration;
            float foIn  = fadeInDuration  >= 0f ? fadeInDuration  : defaultFadeInDuration;

            var transition = new SceneTransitionData
            {
                fromSceneId     = CurrentSceneId,
                toSceneId       = sceneName,
                loadMode        = mode,
                fadeOutDuration = foOut,
                fadeInDuration  = foIn
            };

            StartCoroutine(DoLoadScene(def, transition));
        }

        /// <summary>
        /// Unload an additively loaded scene by id.
        /// </summary>
        public void UnloadScene(string sceneId)
        {
            if (!_index.TryGetValue(sceneId, out var def))
            {
                Debug.LogWarning($"[SceneManager] No scene registered with id '{sceneId}' — cannot unload.");
                return;
            }
            StartCoroutine(DoUnloadScene(def));
        }

        /// <summary>Navigate to the previous scene in the history stack.</summary>
        public void GoBack()
        {
            if (_history.Count < 2)
            {
                Debug.LogWarning("[SceneManager] No previous scene in history.");
                return;
            }
            string previous = _history[_history.Count - 2];
            LoadScene(previous);
        }

        // ─── Preloading ──────────────────────────────────────────────────────────

        /// <summary>
        /// Asynchronously preload all scenes flagged <c>preload = true</c> in the background.
        /// Call once at startup (e.g. from BootStartupManager).
        /// </summary>
        public void PreloadFlaggedScenes()
        {
            foreach (var def in _index.Values)
                if (def.preload) StartCoroutine(PreloadScene(def));
        }

        // ─── Internal coroutines ─────────────────────────────────────────────────

        private IEnumerator DoLoadScene(SceneDefinition def, SceneTransitionData transition)
        {
            IsTransitioning = true;
            LoadProgress    = 0f;

            OnSceneLoading?.Invoke(transition);

            if (verboseLogging)
                Debug.Log($"[SceneManager] Loading '{def.id}' ({def.sceneName}) in mode {def.loadMode}.");

            // Fade out
            yield return StartCoroutine(FadeOut(transition));

            // Async load
            var mode = def.loadMode == SceneLoadMode.Single
                ? UnityEngine.SceneManagement.LoadSceneMode.Single
                : UnityEngine.SceneManagement.LoadSceneMode.Additive;

            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(def.sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneManager] Failed to load scene '{def.sceneName}'. Check Build Settings.");
                IsTransitioning = false;
                yield break;
            }

            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                LoadProgress = Mathf.Clamp01(op.progress / 0.9f);
                yield return null;
            }

            LoadProgress = 1f;

            if (def.loadMode == SceneLoadMode.AdditiveActive)
            {
                var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneByName(def.sceneName);
                if (loaded.IsValid())
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(loaded);
            }

            // Update history
            if (!string.IsNullOrEmpty(CurrentSceneId))
            {
                _history.Add(CurrentSceneId);
                if (_history.Count > maxHistoryDepth)
                    _history.RemoveAt(0);
            }
            CurrentSceneId = def.id;

            OnSceneLoaded?.Invoke(def.id);

            // Fade in
            yield return StartCoroutine(FadeIn(transition));

            IsTransitioning = false;

            if (verboseLogging)
                Debug.Log($"[SceneManager] Scene '{def.id}' loaded.");
        }

        private IEnumerator DoUnloadScene(SceneDefinition def)
        {
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(def.sceneName);
            if (op == null) yield break;
            while (!op.isDone) yield return null;
            OnSceneUnloaded?.Invoke(def.id);
            if (verboseLogging)
                Debug.Log($"[SceneManager] Scene '{def.id}' unloaded.");
        }

        private IEnumerator PreloadScene(SceneDefinition def)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                def.sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            if (op == null) yield break;
            op.allowSceneActivation = false;
            while (op.progress < 0.9f) yield return null;
            if (verboseLogging)
                Debug.Log($"[SceneManager] Preloaded scene '{def.id}' (activation pending).");
        }

        // ─── Fade helpers ────────────────────────────────────────────────────────

        private IEnumerator FadeOut(SceneTransitionData t)
        {
            if (t.fadeOutDuration <= 0f) yield break;

            if (FadeOutOverride != null)
            {
                bool done = false;
                FadeOutOverride(t, () => done = true);
                while (!done) yield return null;
                yield break;
            }

            if (fadeCanvasGroup == null) yield break;

            fadeCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < t.fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / t.fadeOutDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeIn(SceneTransitionData t)
        {
            if (t.fadeInDuration <= 0f) yield break;

            if (FadeInOverride != null)
            {
                bool done = false;
                FadeInOverride(t, () => done = true);
                while (!done) yield return null;
                yield break;
            }

            if (fadeCanvasGroup == null) yield break;

            fadeCanvasGroup.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < t.fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / t.fadeInDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
        }

        // ─── Internal helpers ────────────────────────────────────────────────────

        private void BuildIndex()
        {
            _index.Clear();
            foreach (var def in scenes)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                _index[def.id] = def;
            }
        }

        private void LoadJsonDefinitions()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, jsonPath);
            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly))
                    MergeScenesFromFile(file);
            }
            else if (File.Exists(fullPath))
            {
                MergeScenesFromFile(fullPath);
            }
            else
            {
                Debug.LogWarning($"[SceneManager] JSON not found: {fullPath}");
            }
        }

        private void MergeScenesFromFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                var root = JsonUtility.FromJson<SceneDefinitionList>(json);
                if (root?.scenes == null) return;
                foreach (var def in root.scenes)
                {
                    if (def == null || string.IsNullOrEmpty(def.id)) continue;
                    def.rawJson = json;
                    _index[def.id] = def;
                    if (verboseLogging)
                        Debug.Log($"[SceneManager] JSON override for scene '{def.id}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneManager] Failed to load JSON from '{path}': {e.Message}");
            }
        }

        // ─── JSON wrapper ─────────────────────────────────────────────────────────

        [Serializable]
        private class SceneDefinitionList
        {
            public List<SceneDefinition> scenes;
        }
    }
}
