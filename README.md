# SceneManager

A data-driven Unity scene loading and transition framework.  
Manages scene loading with async progress, configurable fade transitions, scene history navigation, and JSON-authored scene definitions for modding.  
Optionally integrates with [MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework), [StateManager](https://github.com/RolandKaechele/StateManager), [AudioManager](https://github.com/RolandKaechele/AudioManager), and [EventManager](https://github.com/RolandKaechele/EventManager).


## Features

- **Async scene loading** — non-blocking scene loads with real-time progress tracking (0–1)
- **Fade transitions** — configurable fade-out before load and fade-in after load via `CanvasGroup`
- **Scene history** — maintains a navigation history stack for `GoBack()` support
- **Single and Additive** — load scenes in `Single`, `Additive`, or `AdditiveActive` mode per definition
- **JSON-authored definitions** — define scenes in `StreamingAssets/scenes/`; no code required for new entries
- **Modding support** — JSON entries are merged over Inspector definitions by id at runtime
- **MapLoaderFramework integration** — `MapLoaderBridge` triggers scene transitions when chapters define a `sceneId` (activated via `SCENEMANAGER_MLF`)
- **StateManager integration** — `StateManagerBridge` pushes/pops Loading state during transitions (activated via `SCENEMANAGER_STM`)
- **AudioManager integration** — `AudioManagerBridge` crossfades to the scene's configured audio track after load (activated via `SCENEMANAGER_AM`)
- **EventManager integration** — `EventManagerBridge` fires `scene.loading`, `scene.loaded`, `scene.unloaded` events (activated via `SCENEMANAGER_EM`)
- **Custom Inspector** — runtime load-by-id controls, progress bar, and history display in the Unity Inspector
- **DOTween Pro integration** — `DotweenSceneBridge` drives fade CanvasGroup alpha with DOTween tweens instead of coroutines (activated via `SCENEMANAGER_DOTWEEN`)
- **Odin Inspector integration** — `SerializedMonoBehaviour` base for full Inspector serialization (activated via `ODIN_INSPECTOR`)


## Installation

### Option A — Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL…**
3. Enter:

   ```
   https://github.com/RolandKaechele/SceneManager.git
   ```

### Option B — Clone into Assets

```bash
git clone https://github.com/RolandKaechele/SceneManager.git Assets/SceneManager
```

### Option C — Manual copy

Copy the `SceneManager/` folder into your project's `Assets/` directory.


## Quick Start

### 1. Add SceneManager to your scene

Create a persistent manager `GameObject`, then add the **SceneManager** component.

Optionally add:

| Component | Purpose |
| --------- | ------- |
| `DotweenSceneBridge` | DOTween-powered fade transitions |
| `MapLoaderBridge` | Auto-load scenes on chapter change |
| `StateManagerBridge` | Push/pop Loading state |
| `AudioManagerBridge` | Auto-crossfade scene audio |
| `EventManagerBridge` | Fire named events on load/unload |

### 2. Configure scenes in the Inspector

Add entries to the `Scenes` list in the Inspector, or define them in JSON.

### 3. Load from code

```csharp
var sm = FindFirstObjectByType<SceneManager.Runtime.SceneManager>();

// Load by registered id
sm.LoadScene("hub_town");

// Load by Unity scene name (direct, no id required)
sm.LoadSceneByName("Chapter01");

// Additive load
sm.LoadScene("hud_overlay");        // definition sets loadMode = Additive

// Navigate back
sm.GoBack();

// Unload an additive scene
sm.UnloadScene("hud_overlay");
```


## Scene Definition Format (JSON)

Place one or more `.json` files in `StreamingAssets/scenes/`.
All `*.json` files in the folder are loaded and merged by `id` at startup.

**Example:** `StreamingAssets/scenes/main.json`

```json
{
  "scenes": [
    {
      "id": "hub_town",
      "sceneName": "HubTown",
      "label": "Hub Town",
      "loadMode": 0,
      "audioTrackId": "town_theme",
      "loadScreenId": "default",
      "preload": false,
      "tags": ["gameplay", "hub"]
    }
  ]
}
```

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique identifier |
| `sceneName` | string | Unity Build-Settings scene name |
| `label` | string | Human-readable label |
| `loadMode` | int | `0`=Single, `1`=Additive, `2`=AdditiveActive |
| `audioTrackId` | string | AudioManager track id to play on load |
| `loadScreenId` | string | LoadScreenManager screen id to display |
| `preload` | bool | Preload in background at startup |
| `tags` | array | Arbitrary string tags |


## Optional Integration Defines

| Define | Manager | Effect |
| ------ | ------- | ------ |
| `SCENEMANAGER_MLF` | MapLoaderFramework | Trigger scene loads on chapter/map change |
| `SCENEMANAGER_STM` | StateManager | Push/pop Loading state during transitions |
| `SCENEMANAGER_GM` | GameManager | Notify GameManager on scene change |
| `SCENEMANAGER_LSM` | LoadScreenManager | Show load screen during async loads |
| `SCENEMANAGER_EM` | EventManager | Fire scene.loading / scene.loaded / scene.unloaded |
| `SCENEMANAGER_AM` | AudioManager | Crossfade music on scene load |
| `SCENEMANAGER_DOTWEEN` | DOTween Pro | DOTween-driven fade transitions |


## Runtime API

### `SceneManager`

| Member | Description |
| ------ | ----------- |
| `LoadScene(id, fadeOut, fadeIn)` | Load scene by registered id |
| `LoadSceneByName(name, mode, ...)` | Load by Unity scene name (no id required) |
| `UnloadScene(id)` | Unload an additively loaded scene |
| `GoBack()` | Load the previous scene from history |
| `PreloadFlaggedScenes()` | Background-preload all scenes flagged `preload=true` |
| `RegisterScene(def)` | Register or replace a definition at runtime |
| `GetDefinition(id)` | Return `SceneDefinition` by id, or null |
| `GetAllIds()` | All registered scene ids |
| `CurrentSceneId` | Id of the currently loaded scene |
| `LoadProgress` | Async load progress (0–1) |
| `IsTransitioning` | True while a transition is in progress |
| `History` | Read-only navigation history |
| `OnSceneLoading` | `event Action<SceneTransitionData>` — fires before loading |
| `OnSceneLoaded` | `event Action<string>` — fires after loading |
| `OnSceneUnloaded` | `event Action<string>` — fires on additive unload |
| `FadeOutOverride` | `Action<SceneTransitionData, Action>` delegate |
| `FadeInOverride` | `Action<SceneTransitionData, Action>` delegate |


## Examples

| File | Description |
| ---- | ----------- |
| `Examples/StreamingAssets/scenes.json` | Sample scene definitions |


## Editor Tools

Open via **JSON Editors → Scene Manager** in the Unity menu bar, or via the **Open JSON Editor** button in the SceneManager Inspector.

| Action | Result |
| ------ | ------ |
| **Load** | Reads all `*.json` from `StreamingAssets/scenes/`; creates the folder if missing |
| **Edit** | Add / remove / reorder entries using the Inspector list |
| **Save** | Writes to `StreamingAssets/scenes/scenes.json` and calls `AssetDatabase.Refresh()` |

With **ODIN_INSPECTOR** active, the list uses Odin's enhanced drawer (drag-to-sort, collapsible entries).


## Dependencies

| Dependency | Required | Notes |
| ---------- | -------- | ----- |
| Unity 2022.3+ | ✓ | |
| MapLoaderFramework | optional | Required when `SCENEMANAGER_MLF` is defined |
| StateManager | optional | Required when `SCENEMANAGER_STM` is defined |
| AudioManager | optional | Required when `SCENEMANAGER_AM` is defined |
| EventManager | optional | Required when `SCENEMANAGER_EM` is defined |
| DOTween Pro | optional | Required when `SCENEMANAGER_DOTWEEN` is defined |
| Odin Inspector | optional | Required when `ODIN_INSPECTOR` is defined |


## License

MIT — see [LICENSE](LICENSE).
