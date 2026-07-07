
# Camera System

The Mapbox SDK includes a camera system for navigating the map with mouse and touch input. There are two camera modes, each suited to different use cases.

---

## Camera Modes

### 3D Camera (Moving3dCameraBehaviour)

The camera moves through 3D space while the map stays in place. Best for:
- 3D city exploration
- Games and simulations
- Scenes where the camera needs to orbit around a point of interest

The camera orbits a target point on the map. Panning moves the camera, zooming adjusts how far the camera is from the ground, and rotation orbits around the target.

### Slippy Map Camera (SlippyMapCameraBehaviour)

The camera stays in a fixed position while the map moves underneath. Best for:
- Traditional 2D map interfaces
- Navigation and routing views
- Data visualization overlays
- AR/VR applications where the camera is controlled by head tracking

Panning shifts the map's geographic center. Zooming changes the map's scale level. The camera itself doesn't move — the world moves under it.

---

## Setting Up

1. Your scene needs a **MapboxMapBehaviour** on a GameObject (this is the map itself).
2. Add a camera behaviour component to any GameObject in the scene:
   - **Moving3dCameraBehaviour** for 3D camera, or
   - **SlippyMapCameraBehaviour** for slippy map camera
3. Assign the **Map Behaviour** field to the GameObject that has MapboxMapBehaviour.
4. Assign the **Camera** field to the scene camera used for rendering. If left empty, the main camera is used automatically.

The camera will initialize itself once the map is ready. No additional setup is required.

---

## Settings Reference

### 3D Camera Settings

| Setting | Description |
|---------|-------------|
| **Pitch** | Camera tilt angle. 90 = looking straight down, 15 = near the horizon. |
| **Bearing** | Camera rotation in degrees. 0 = north is up. |
| **Camera Curve** | An animation curve that controls how camera height changes with zoom level. The X axis is zoom, Y axis is distance from the ground. |
| **Distance Scale** | Multiplier on the camera curve output. Increase to raise the camera without re-editing the curve. Default: 2. |
| **Zoom Sensitivity** | How much each scroll step changes the zoom level. Default: 0.25. |
| **Rotation Speed** | How fast right-click drag rotates the camera. Default: 50. |

### Slippy Map Camera Settings

| Setting | Description |
|---------|-------------|
| **Center Latitude Longitude** | Initial map center as a geographic coordinate. Example: 40.7128, -74.0060 for New York. |
| **Pitch** | Camera tilt angle. 90 = looking straight down, 15 = near the horizon. |
| **Bearing** | Camera rotation in degrees. 0 = north is up. |
| **Camera Distance** | How far the camera is from the ground target. Typical range: 100 to 1000 depending on your map's scale. Default: 200. |
| **Pan Enabled** | Toggle left-click / single-finger drag to pan the map. |

**Rotation Settings** (nested):

| Setting | Description |
|---------|-------------|
| **Enabled** | Toggle right-click drag to rotate. |
| **Speed** | Rotation sensitivity. Default: 50. |
| **Rotation Mode** | *Rotate The Camera* orbits the camera around the map. *Rotate The Map* keeps the camera fixed and rotates the map underneath — useful for navigation-style views. |
| **Map Root** | The root transform of the map. Auto-detected if left empty. |

**Zoom Settings** (nested):

| Setting | Description |
|---------|-------------|
| **Enabled** | Toggle scroll wheel / pinch zoom. |
| **Speed** | Zoom sensitivity per step. Default: 0.25. |
| **Zoom At Cursor** | When enabled, the map zooms toward the cursor or pinch position. When disabled, zoom is always centered on the map. |

---

## Input Controls

Both cameras support mouse and touch input. The controls are:

| Action | Mouse | Touch |
|--------|-------|-------|
| Pan | Left-click drag | Single-finger drag |
| Rotate | Right-click drag | Not available via touch |
| Zoom | Scroll wheel | Two-finger pinch |
| Tilt | Not available via mouse | Two-finger vertical drag |

When using two fingers, the system automatically detects whether you are pinching (zoom) or dragging vertically (tilt) and applies the dominant gesture. Both gestures will not activate at the same time.

Zoom at cursor works with both mouse and touch — when using pinch, the zoom centers on the midpoint between your two fingers.

---

## Input System Support

The cameras work with both Unity's legacy Input Manager and the new Input System package. `com.unity.inputsystem` is a hard dependency of the SDK (UPM installs it automatically), but the active code path is selected by Player Settings, not by package presence:

- **Active Input Handling = "Input Manager (Old)" (default)** — uses the legacy `UnityEngine.Input` API. No action needed, and this is unchanged by the package being installed.
- **Active Input Handling = "Input System Package (New)" or "Both"** — Unity defines the built-in `ENABLE_INPUT_SYSTEM` symbol, which activates the new-input branch. Pointer state is read via `Mouse.current` / `Touch.activeTouches`.

Installing `com.unity.inputsystem` alone does not change which path runs — only flipping Active Input Handling does. See the [CHANGELOG](../CHANGELOG.md#v311) for why the SDK moved from a soft dependency gated on package presence to this model.

---

## Writing a Custom Camera

If neither camera mode fits your needs, you can create your own by writing a class that extends `MapInput` and a MonoBehaviour that extends `MapCameraBehaviour<T>`.

Your custom camera class needs to implement one method — `UpdateCamera` — where you handle input and return camera state (center, zoom, pitch, bearing, scale). The base class provides ready-made helpers for input detection, coordinate conversion, and value clamping.

The behaviour wrapper handles initialization and communicating changes to the map automatically. A minimal custom camera behaviour only needs a few lines of code beyond the camera logic itself.

---

## AR / MR setups

As of v3.1.0, the SDK supports parenting the map under an external transform — an `ARAnchor`, an XR rig, or any rotated/translated parent — and having the entire map (terrain tiles, buildings, roads, areas) compose correctly with that parent transform. This makes it possible to place the map on a real-world surface in AR, anchor it to a tracked plane, or rotate the whole map as a unit.

### What works

- **Translating `MapRoot`** in world space — the whole map moves as a unit.
- **Rotating `MapRoot`** around any axis — terrain and vector content follow.
- **Parenting `MapRoot`** under an `ARAnchor`, an `XROrigin` child, or any custom rig — the parent's translation and rotation propagate.

All of this works because tile placement and vector-content placement are done in **local space** relative to `MapRoot`, and runtime-created roots use `SetParent(parent, worldPositionStays: false)` so they don't pin themselves to world coordinates.

### What doesn't work (yet)

- **Non-unit `MapRoot.localScale`.** The quadtree LOD math (`UnityTileProvider.ShouldSplit`) reads the camera's world-space position and compares against tile bounds derived from `MapInformation.Scale`. Scaling `MapRoot` doesn't propagate to that math, so the LOD will pick zoom levels appropriate for the unscaled tile size, not the visible one.

  **Use `MapInformation.Scale` to drive map size instead.** A `Scale` of 1000 means each Web Mercator unit is rendered as 1000 Unity units. For a tabletop-size diorama, set `Scale` to a small value (e.g. 0.01) — the LOD math will follow. For an animated tabletop → world-size transition, the `DynamicScalingMapInformation` class drives `Scale` from an animation curve indexed by zoom.

### Recommended setup

For AR/MR apps you generally want:

1. **Replace the built-in camera behaviours.** `SlippyMapCameraBehaviour` and `Moving3dCameraBehaviour` both write directly to `Camera.transform` every frame (and in *Rotate The Map* mode, `SlippyMapCamera` writes to `MapRoot.rotation`). In an AR/MR setup ARKit/ARCore is already driving the camera transform; you don't want the SDK fighting it. Remove these components from the scene.

2. **Write a thin custom `MapCameraBehaviour<T>`** that does the inverse: instead of moving the camera based on input, it reads the AR-tracked camera's pose and pushes the relevant state into `MapboxMap.ChangeView(...)`. You typically only need to push `Center` (lat/lon) and `Scale` — pitch and bearing can stay at their initial values since the AR camera handles its own orientation.

3. **Let the quadtree LOD see the AR camera.** `UnityTileProvider.Settings.Camera` can be set explicitly via the `UnityTileProviderBehaviour` inspector. Point it at your AR camera. The tile cover will frustum-cull against the AR camera's actual view.

4. **Animate scale, not transform.** For a "diorama grows to world size" effect, lerp `MapInformation.Scale` over time. The visualizer reacts to `WorldScaleChanged` and re-lays-out tiles without re-fetching as long as zoom is held constant. Pre-warm the cache for the destination zoom range if you cross zoom boundaries during the animation.

### Limitations

This is the first release with AR-style parent transforms supported architecturally. It hasn't been validated against a full ARKit / ARCore sample, and it has not been profiled on AR-class devices. Treat it as **early access**: the bones are in place, but expect to do your own integration work and report rough edges.
