#if SCENEMANAGER_LSM
using LoadScreenManager.Runtime;
using UnityEngine;

namespace SceneManager.Runtime
{
    /// <summary>
    /// <b>LoadScreenManagerBridge</b> connects SceneManager to LoadScreenManager.
    /// <para>
    /// When <c>SCENEMANAGER_LSM</c> is defined:
    /// <list type="bullet">
    ///   <item>Shows the load screen when a scene transition begins
    ///   (<see cref="SceneManager.OnSceneLoading"/>).</item>
    ///   <item>Hides the load screen once the destination scene has finished loading
    ///   (<see cref="SceneManager.OnSceneLoaded"/>).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to the same GameObject as <see cref="SceneManager"/>
    /// and add <c>SCENEMANAGER_LSM</c> to Player Settings › Scripting Define Symbols.
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SceneManager/LoadScreenManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class LoadScreenManagerBridge : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Tooltip("Id of the LoadScreenDefinition to show during scene transitions. " +
                             "Leave empty to use LoadScreenManager's defaultScreenId.")]
        [UnityEngine.SerializeField] private string loadScreenId = "";

        private SceneManager                           _sceneManager;
        private LoadScreenManager.Runtime.LoadScreenManager _lsm;

        private void Awake()
        {
            _sceneManager = GetComponent<SceneManager>() ?? FindFirstObjectByType<SceneManager>();
            _lsm          = GetComponent<LoadScreenManager.Runtime.LoadScreenManager>()
                            ?? FindFirstObjectByType<LoadScreenManager.Runtime.LoadScreenManager>();

            if (_sceneManager == null)
                Debug.LogWarning("[SceneManager/LoadScreenManagerBridge] SceneManager not found.");
            if (_lsm == null)
                Debug.LogWarning("[SceneManager/LoadScreenManagerBridge] LoadScreenManager not found — load screen automation disabled.");
        }

        private void OnEnable()
        {
            if (_sceneManager != null)
            {
                _sceneManager.OnSceneLoading += HandleSceneLoading;
                _sceneManager.OnSceneLoaded  += HandleSceneLoaded;
            }
        }

        private void OnDisable()
        {
            if (_sceneManager != null)
            {
                _sceneManager.OnSceneLoading -= HandleSceneLoading;
                _sceneManager.OnSceneLoaded  -= HandleSceneLoaded;
            }
        }

        private void HandleSceneLoading(SceneTransitionData data)
        {
            _lsm?.Show(loadScreenId);
        }

        private void HandleSceneLoaded(string sceneId)
        {
            _lsm?.Hide();
        }
    }
}
#else
namespace SceneManager.Runtime
{
    /// <summary>No-op stub — define <c>SCENEMANAGER_LSM</c> to activate this bridge.</summary>
    public class LoadScreenManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
