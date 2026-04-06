#if SCENEMANAGER_MLF
using MapLoaderFramework.Runtime;
using UnityEngine;

namespace SceneManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderBridge</b> connects SceneManager to MapLoaderFramework.
    /// <para>
    /// When <c>SCENEMANAGER_MLF</c> is defined:
    /// <list type="bullet">
    ///   <item>Subscribes to <c>MapLoaderFramework.OnMapLoaded</c> and triggers a scene transition
    ///   to the scene id matching the loaded map's <c>sceneId</c> field (if a matching scene is registered).</item>
    ///   <item>Allows chapters to declaratively specify which Unity scene to load via map JSON data.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SceneManager/MapLoader Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MapLoaderBridge : UnityEngine.MonoBehaviour
    {
        [Tooltip("If true, the bridge will automatically load the scene specified in the map's sceneId field.")]
        [UnityEngine.SerializeField] private bool autoLoadOnMapChange = true;

        private SceneManager _sceneManager;
        private MapLoaderFramework _mapLoader;

        private void Awake()
        {
            _sceneManager = GetComponent<SceneManager>() ?? FindFirstObjectByType<SceneManager>();
            _mapLoader    = GetComponent<MapLoaderFramework>() ?? FindFirstObjectByType<MapLoaderFramework>();

            if (_sceneManager == null)
                Debug.LogWarning("[SceneManager/MapLoaderBridge] SceneManager not found.");
            if (_mapLoader == null)
                Debug.LogWarning("[SceneManager/MapLoaderBridge] MapLoaderFramework not found.");
        }

        private void OnEnable()
        {
            if (_mapLoader != null)
                _mapLoader.OnMapLoaded += HandleMapLoaded;
        }

        private void OnDisable()
        {
            if (_mapLoader != null)
                _mapLoader.OnMapLoaded -= HandleMapLoaded;
        }

        private void HandleMapLoaded(MapData mapData)
        {
            if (!autoLoadOnMapChange || _sceneManager == null) return;
            if (mapData == null || string.IsNullOrEmpty(mapData.sceneId)) return;

            if (_sceneManager.GetDefinition(mapData.sceneId) != null)
                _sceneManager.LoadScene(mapData.sceneId);
        }
    }
}
#endif
