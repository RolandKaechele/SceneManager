#if SCENEMANAGER_AM
using AudioManager.Runtime;
using UnityEngine;

namespace SceneManager.Runtime
{
    /// <summary>
    /// <b>AudioManagerBridge</b> connects SceneManager to AudioManager.
    /// <para>
    /// When <c>SCENEMANAGER_AM</c> is defined:
    /// <list type="bullet">
    ///   <item>Crossfades to the scene's configured audio track after each scene load.</item>
    ///   <item>Reads <see cref="SceneDefinition.audioTrackId"/> from the loaded scene's definition.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SceneManager/AudioManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class AudioManagerBridge : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Tooltip("Crossfade duration in seconds when switching to the scene's audio track.")]
        [UnityEngine.SerializeField] private float crossfadeDuration = 1.0f;

        private SceneManager  _sceneManager;
        private AudioManager  _audioManager;

        private void Awake()
        {
            _sceneManager = GetComponent<SceneManager>() ?? FindFirstObjectByType<SceneManager>();
            _audioManager = GetComponent<AudioManager>() ?? FindFirstObjectByType<AudioManager>();

            if (_sceneManager == null)
                Debug.LogWarning("[SceneManager/AudioManagerBridge] SceneManager not found.");
            if (_audioManager == null)
                Debug.LogWarning("[SceneManager/AudioManagerBridge] AudioManager not found.");
        }

        private void OnEnable()
        {
            if (_sceneManager != null)
                _sceneManager.OnSceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (_sceneManager != null)
                _sceneManager.OnSceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(string sceneId)
        {
            if (_audioManager == null) return;
            var def = _sceneManager.GetDefinition(sceneId);
            if (def == null || string.IsNullOrEmpty(def.audioTrackId)) return;
            _audioManager.PlayMusic(def.audioTrackId, crossfadeDuration);
        }
    }
}
#endif
