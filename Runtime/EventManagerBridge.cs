#if SCENEMANAGER_EM
using EventManager.Runtime;
using UnityEngine;

namespace SceneManager.Runtime
{
    /// <summary>
    /// <b>EventManagerBridge</b> connects SceneManager to EventManager.
    /// <para>
    /// When <c>SCENEMANAGER_EM</c> is defined, fires the following named events:
    /// <list type="bullet">
    ///   <item><c>scene.loading</c> — payload: toSceneId, fromSceneId</item>
    ///   <item><c>scene.loaded</c>  — payload: sceneId</item>
    ///   <item><c>scene.unloaded</c> — payload: sceneId</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SceneManager/EventManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class EventManagerBridge : UnityEngine.MonoBehaviour
    {
        private SceneManager  _sceneManager;
        private EventManager  _eventManager;

        private void Awake()
        {
            _sceneManager = GetComponent<SceneManager>() ?? FindFirstObjectByType<SceneManager>();
            _eventManager = GetComponent<EventManager>() ?? FindFirstObjectByType<EventManager>();

            if (_sceneManager == null)
                Debug.LogWarning("[SceneManager/EventManagerBridge] SceneManager not found.");
            if (_eventManager == null)
                Debug.LogWarning("[SceneManager/EventManagerBridge] EventManager not found.");
        }

        private void OnEnable()
        {
            if (_sceneManager == null) return;
            _sceneManager.OnSceneLoading  += HandleLoading;
            _sceneManager.OnSceneLoaded   += HandleLoaded;
            _sceneManager.OnSceneUnloaded += HandleUnloaded;
        }

        private void OnDisable()
        {
            if (_sceneManager == null) return;
            _sceneManager.OnSceneLoading  -= HandleLoading;
            _sceneManager.OnSceneLoaded   -= HandleLoaded;
            _sceneManager.OnSceneUnloaded -= HandleUnloaded;
        }

        private void HandleLoading(SceneTransitionData t)
            => _eventManager?.FireEvent("scene.loading", t.toSceneId);

        private void HandleLoaded(string id)
            => _eventManager?.FireEvent("scene.loaded", id);

        private void HandleUnloaded(string id)
            => _eventManager?.FireEvent("scene.unloaded", id);
    }
}
#endif
