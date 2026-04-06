#if SCENEMANAGER_GM
using GameManager.Runtime;
using UnityEngine;

namespace SceneManager.Runtime
{
    /// <summary>
    /// <b>GameManagerBridge</b> connects SceneManager to GameManager.
    /// <para>
    /// When <c>SCENEMANAGER_GM</c> is defined:
    /// <list type="bullet">
    ///   <item>Listens to <see cref="SceneManager.OnSceneLoading"/> and transitions GameManager
    ///   to <see cref="GameState.Loading"/> so gameplay systems pause during scene transitions.</item>
    ///   <item>Listens to <see cref="SceneManager.OnSceneLoaded"/> and transitions GameManager
    ///   to <see cref="GameState.Playing"/> once the new scene is ready.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to the same GameObject as <see cref="SceneManager"/>
    /// and add <c>SCENEMANAGER_GM</c> to Player Settings › Scripting Define Symbols.
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SceneManager/GameManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class GameManagerBridge : UnityEngine.MonoBehaviour
    {
        private SceneManager                    _sceneManager;
        private GameManager.Runtime.GameManager _gameManager;

        private void Awake()
        {
            _sceneManager = GetComponent<SceneManager>() ?? FindFirstObjectByType<SceneManager>();
            _gameManager  = GetComponent<GameManager.Runtime.GameManager>()
                            ?? FindFirstObjectByType<GameManager.Runtime.GameManager>();

            if (_sceneManager == null)
                Debug.LogWarning("[SceneManager/GameManagerBridge] SceneManager not found.");
            if (_gameManager == null)
                Debug.LogWarning("[SceneManager/GameManagerBridge] GameManager not found — state sync disabled.");
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
            _gameManager?.ChangeState(GameState.Loading);
        }

        private void HandleSceneLoaded(string sceneId)
        {
            _gameManager?.ChangeState(GameState.Playing);
        }
    }
}
#else
namespace SceneManager.Runtime
{
    /// <summary>No-op stub — define <c>SCENEMANAGER_GM</c> to activate this bridge.</summary>
    public class GameManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
