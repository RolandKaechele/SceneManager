#if SCENEMANAGER_STM
using StateManager.Runtime;
using UnityEngine;

namespace SceneManager.Runtime
{
    /// <summary>
    /// <b>StateManagerBridge</b> connects SceneManager to StateManager.
    /// <para>
    /// When <c>SCENEMANAGER_STM</c> is defined:
    /// <list type="bullet">
    ///   <item>Blocks scene transitions while a modal state (Dialogue, Cutscene, MiniGame, Loading)
    ///   is active on the StateManager stack.</item>
    ///   <item>Pushes the Loading state onto the stack at the start of a transition and pops it on
    ///   completion.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SceneManager/StateManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class StateManagerBridge : UnityEngine.MonoBehaviour
    {
        [Tooltip("State id to push while a scene transition is in progress (default: 'Loading').")]
        [UnityEngine.SerializeField] private string loadingStateId = "Loading";

        [Tooltip("When true, scene transitions are blocked while StateManager has a non-Gameplay state active.")]
        [UnityEngine.SerializeField] private bool blockDuringModalStates = true;

        private SceneManager _sceneManager;
        private StateManager _stateManager;

        private void Awake()
        {
            _sceneManager = GetComponent<SceneManager>() ?? FindFirstObjectByType<SceneManager>();
            _stateManager = GetComponent<StateManager>() ?? FindFirstObjectByType<StateManager>();

            if (_sceneManager == null)
                Debug.LogWarning("[SceneManager/StateManagerBridge] SceneManager not found.");
            if (_stateManager == null)
                Debug.LogWarning("[SceneManager/StateManagerBridge] StateManager not found.");
        }

        private void OnEnable()
        {
            if (_sceneManager == null) return;
            _sceneManager.OnSceneLoading += HandleSceneLoading;
            _sceneManager.OnSceneLoaded  += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (_sceneManager == null) return;
            _sceneManager.OnSceneLoading -= HandleSceneLoading;
            _sceneManager.OnSceneLoaded  -= HandleSceneLoaded;
        }

        private void HandleSceneLoading(SceneTransitionData t)
        {
            _stateManager?.PushState(loadingStateId);
        }

        private void HandleSceneLoaded(string sceneId)
        {
            if (_stateManager != null && _stateManager.CurrentState?.id == loadingStateId)
                _stateManager.PopState();
        }
    }
}
#endif
