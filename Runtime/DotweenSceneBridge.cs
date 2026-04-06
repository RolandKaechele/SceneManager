#if SCENEMANAGER_DOTWEEN
using System;
using UnityEngine;
using DG.Tweening;

namespace SceneManager.Runtime
{
    /// <summary>
    /// Optional bridge that replaces the coroutine-based fade transitions inside
    /// <see cref="SceneManager"/> with DOTween-driven CanvasGroup alpha tweens,
    /// providing easing control over all scene fade-in/fade-out effects.
    /// Enable define <c>SCENEMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Sets <see cref="SceneManager.FadeOutOverride"/> and
    /// <see cref="SceneManager.FadeInOverride"/> so that all transition fades
    /// are routed through this bridge instead of the default coroutine implementation.
    /// </para>
    /// </summary>
    [AddComponentMenu("SceneManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenSceneBridge : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("DOTween ease applied to the fade-out before scene loading.")]
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;

        [Tooltip("DOTween ease applied to the fade-in after scene loading.")]
        [SerializeField] private Ease fadeInEase = Ease.OutQuad;

        [Tooltip("CanvasGroup used for screen fades. Must match the one assigned in SceneManager.")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        private SceneManager _sceneManager;

        private void Awake()
        {
            _sceneManager = GetComponent<SceneManager>() ?? FindFirstObjectByType<SceneManager>();
            if (_sceneManager == null)
            {
                Debug.LogWarning("[SceneManager/DotweenSceneBridge] SceneManager not found.");
                return;
            }
            if (fadeCanvasGroup == null)
            {
                Debug.LogWarning("[SceneManager/DotweenSceneBridge] fadeCanvasGroup not assigned.");
            }
        }

        private void OnEnable()
        {
            if (_sceneManager == null) return;
            _sceneManager.FadeOutOverride = HandleFadeOut;
            _sceneManager.FadeInOverride  = HandleFadeIn;
        }

        private void OnDisable()
        {
            if (_sceneManager == null) return;
            if (_sceneManager.FadeOutOverride == (Action<SceneTransitionData, Action>)HandleFadeOut)
                _sceneManager.FadeOutOverride = null;
            if (_sceneManager.FadeInOverride == (Action<SceneTransitionData, Action>)HandleFadeIn)
                _sceneManager.FadeInOverride = null;
        }

        private void OnDestroy()
        {
            if (fadeCanvasGroup != null)
                DOTween.Kill(fadeCanvasGroup);
        }

        private void HandleFadeOut(SceneTransitionData t, Action onComplete)
        {
            if (fadeCanvasGroup == null) { onComplete?.Invoke(); return; }
            DOTween.Kill(fadeCanvasGroup);
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.DOFade(1f, t.fadeOutDuration)
                .SetEase(fadeOutEase)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void HandleFadeIn(SceneTransitionData t, Action onComplete)
        {
            if (fadeCanvasGroup == null) { onComplete?.Invoke(); return; }
            DOTween.Kill(fadeCanvasGroup);
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.DOFade(0f, t.fadeInDuration)
                .SetEase(fadeInEase)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}
#else
namespace SceneManager.Runtime
{
    /// <summary>No-op stub — enable define <c>SCENEMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("SceneManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenSceneBridge : UnityEngine.MonoBehaviour { }
}
#endif
