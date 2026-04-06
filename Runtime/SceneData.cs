using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneManager.Runtime
{
    // -------------------------------------------------------------------------
    // SceneLoadMode
    // -------------------------------------------------------------------------

    /// <summary>
    /// How a scene is loaded relative to already-loaded scenes.
    /// </summary>
    public enum SceneLoadMode
    {
        /// <summary>Unload all other scenes and load the target scene.</summary>
        Single,

        /// <summary>Load the target scene on top of the currently active scenes.</summary>
        Additive,

        /// <summary>Additive load, but set the new scene as the active scene.</summary>
        AdditiveActive
    }

    // -------------------------------------------------------------------------
    // SceneDefinition
    // -------------------------------------------------------------------------

    /// <summary>
    /// Describes a registered scene entry. Authored in JSON or via Inspector.
    /// </summary>
    [Serializable]
    public class SceneDefinition
    {
        /// <summary>Unique string id used to reference this scene (e.g. "hub_town").</summary>
        public string id;

        /// <summary>Unity Build Settings scene name (as shown in the scene list).</summary>
        public string sceneName;

        /// <summary>Human-readable label shown in Editor UI and load screens.</summary>
        public string label;

        /// <summary>Default load mode for this scene.</summary>
        public SceneLoadMode loadMode = SceneLoadMode.Single;

        /// <summary>Audio track id (registered in AudioManager) to play after loading.</summary>
        public string audioTrackId;

        /// <summary>Load screen id (registered in LoadScreenManager) to display during loading.</summary>
        public string loadScreenId;

        /// <summary>Whether to preload this scene asynchronously at startup.</summary>
        public bool preload = false;

        /// <summary>Arbitrary key/value tags available for game-specific logic.</summary>
        public List<string> tags = new();

        /// <summary>Raw JSON stored during deserialisation (non-serialised).</summary>
        [NonSerialized] public string rawJson;
    }

    // -------------------------------------------------------------------------
    // SceneTransitionData
    // -------------------------------------------------------------------------

    /// <summary>
    /// Payload passed to bridge callbacks and events during a scene transition.
    /// </summary>
    [Serializable]
    public class SceneTransitionData
    {
        /// <summary>Id of the scene being unloaded (null if no previous scene).</summary>
        public string fromSceneId;

        /// <summary>Id of the scene being loaded.</summary>
        public string toSceneId;

        /// <summary>Load mode used for this transition.</summary>
        public SceneLoadMode loadMode;

        /// <summary>Duration in seconds of the fade-out before loading starts.</summary>
        public float fadeOutDuration;

        /// <summary>Duration in seconds of the fade-in after loading completes.</summary>
        public float fadeInDuration;
    }
}
