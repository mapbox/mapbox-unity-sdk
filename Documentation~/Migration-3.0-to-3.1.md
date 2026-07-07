# Migration guide — v3.0 → v3.1

v3.1.0 is a substantial release that introduces a new camera system, a new tile-provider LOD algorithm, a Burst-compiled terrain pipeline, and a multicast cache disposal contract. Most projects will upgrade with no source changes. A handful of API shapes have changed; this document walks through each so you know exactly what to touch.

For the full release notes see [`CHANGELOG.md`](../CHANGELOG.md). This guide is the *narrative upgrade path* — the CHANGELOG is the reference.

---

## Before you upgrade

1. Commit your working state.
2. Note your **Managed Stripping Level** in Player Settings → Android → Other Settings → Optimization. v3.1.0's tested target is **Low** (or Minimal / Disabled). **Medium** has been briefly tested and seems to work — the SDK now preserves its SQLite model classes with `[Preserve]` attributes plus a `link.xml`. **High** is untested and *might* work given the same preservation, but no guarantees. If you were on Low or below, stay there. If you want to try Medium / High, validate on your build before committing.
3. If your project pinned a specific Burst version, note it — v3.1.0 adds `com.unity.burst@1.8.12` as a new dependency.

---

## Step 1 — Install v3.1.0

Replace the v3.0 package with v3.1.0 via Package Manager (or re-clone the repo into your `Packages` folder). The first launch will trigger Burst AOT compilation (one-shot, ~10–60s depending on the platform). Subsequent launches are normal.

The new dependency is resolved automatically by UPM:

| Dependency | Status | Why |
| --- | --- | --- |
| `com.unity.burst@1.8.12` | **New required dep** | Terrain-RGB decode and collider vertex fill are now Burst jobs. |
| `com.unity.inputsystem@1.7.0` | **New required dep (as of v3.1.1)** | Hard dependency, auto-installed by UPM. Which input path runs is controlled by Player Settings → Active Input Handling, not by package presence — see [CameraSystem.md](CameraSystem.md#input-system-support). Projects on "Input Manager (Old)" (the default) see no behavior change. |

---

## Step 2 — Source-level breaks to fix

The compile errors you might see are all narrow. If a section doesn't apply, your project doesn't trip the break.

### Custom `IMapInformation` implementations

If you ship your own `IMapInformation` (i.e., not just using the built-in `MapInformation`), the interface gained a new property:

```
public TerrainInfo Terrain { get; }
```

Add it. For non-terrain maps, return `new TerrainInfo()` — defaults (`MinElevation = 0`, `MaxElevation = 5000`) are fine. The built-in `MapInformation` already implements it.

### Direct assignment to `TerrainData.ElevationValuesUpdated`

The field is now a proper `event Action`. Old code that did `data.ElevationValuesUpdated = myCallback` no longer compiles, nor does `data.ElevationValuesUpdated()` to fire it. Subscribe with `+=` and unsubscribe with `-=`. The previous helper `SetElevationChangedCallback` (which silently wiped other subscribers) is removed.

### Calls to `MapboxTileData.SetDisposeCallback`

Removed in favor of multicast:

- `AddDisposeCallback(Action)` — register a handler
- `RemoveDisposeCallback(Action)` — deregister

Up to 16 render tiles share a single `TerrainData` instance; the old single-setter API silently dropped 15 of them on every replacement. If you assigned via reflection or `InternalsVisibleTo`, switch to the public add/remove pair.

### Subclasses of `MapInput`

If you wrote a custom `MapInput` (e.g., to add input behaviours not in the built-in set), you should override the new virtual:

```
public override void Teardown(IMapInformation mapInfo)
{
    // Unsubscribe anything you wired in Initialize
}
```

It's a no-op by default — you won't see a compile error, but without the override your subscriptions root the destroyed camera + its Transform when `IMapInformation` outlives the camera (DontDestroyOnLoad / scene reload).

### Subclasses of `UnityTileProvider`

`TileNode.Set` signature changed:

- Old: `Set(id, worldCenter, scale, boundsHeight)`
- New: `Set(id, worldCenter, scale, boundsBottom, boundsHeight)`

Update the call site if you subclassed and reached into `TileNode` directly. The new `boundsBottom` lets the AABB reach sub-sea-level for terrain that dips below Y=0 (Death Valley, Dead Sea).

### Subclasses of `CustomTMSTile`

The constructor went from 4 args to 6:

- Old: `(urlFormat, tileId, tilesetId, useNonReadableTexture)`
- New: `(urlFormat, tileId, tilesetId, useNonReadableTexture, invertY, isMapboxService)`

The 4-arg constructor is preserved as an overload that defaults `invertY: true, isMapboxService: false` — your existing subclasses still compile. New code should use the 6-arg form to control the two booleans explicitly.

### Camera behaviours subclassed from `MapCameraBehaviour`

There's now an open generic abstract base `MapCameraBehaviour<T>` where `T` is the camera-core type. The shipped behaviours (`SlippyMapCameraBehaviour`, `Moving3dCameraBehaviour`) close the generic. If you subclassed the old non-generic base, change to:

```
public class MyCustomBehaviour : MapCameraBehaviour<MyCustomCameraCore> { ... }
```

---

## Step 3 — Behavioral changes to verify

These don't cause compile errors but may change runtime behavior.

### Custom imagery: `CustomSource.CreateTile` always returns `CustomTMSTile` when `UrlFormat` is set

Previously `InvertY=false` with a non-empty `UrlFormat` would silently fall back to a plain `RasterTile`. v3.1.0 always uses `CustomTMSTile`. If you depended on the plain-raster path, set `UrlFormat = ""` — empty `UrlFormat` falls back to plain `RasterTile` for both `CustomSource` and `CustomTerrainSource`.

### Custom terrain: `Settings.DataSettings` now actually applies

`CustomTerrainLayerModuleScript` used to throw away the Inspector-configured `DataSettings` and use defaults. v3.1.0 passes them through. **If you had Inspector values set, they now take effect** — cache size, retina flag, non-readable textures, data-zoom clamp. Verify they were intentional.

### `TerrainInfo.IsEnabled` and `Exaggeration` removed

Both fields were written-but-never-read in v3.0. v3.1.0 drops them. If your code set either field, the assignment no longer compiles — but the runtime behavior is unchanged, since nothing consumed those fields anyway. Delete the assignments.

### Tile provider — new defaults, new clamps

- `UnityTileProviderSettings.SubdivisionBias` default changed from `1.0f` to `0.6f`. New scenes get `0.6` as the inspector default. Legacy scenes saved before this field existed deserialize to `0` (Unity assigns `default(float)` for new fields and skips C# initializers); `ISerializationCallbackReceiver.OnAfterDeserialize` upgrades anything `≤ 0` to `0.6f` automatically.
- `UnityTileProvider.MaximumZoomLevel` is now silently clamped to **30** internally with a log warning if higher. Practically unreachable in Mercator but worth knowing.

### `MapInformation.Initialize` resets terrain bounds

`Initialize(LatitudeLongitude)` now resets `Terrain.MinElevation` / `MaxElevation` to `TerrainInfo` defaults. If you intentionally pre-set these before `Initialize` and relied on them surviving, move that assignment to *after* `Initialize`. New `MapInformation` instances behave identically.

### Tile placement is now local-space

Tiles are positioned via `transform.localPosition` (was `transform.position`). On every existing scene with `MapRoot` at the world origin with identity rotation and unit scale this is *identical* behaviour. If your project transforms `MapRoot` (AR anchor, custom rig), the SDK now respects that translation and rotation. **Non-unit `MapRoot.localScale` is not supported** — drive map scale through `MapInformation.Scale` instead. See `Documentation~/CoordinateConversions.md` for the local-space conversion contract.

---

## Step 4 — Camera setup

v3.1.0 replaces the old camera setup with two concrete behaviours plus the generic base. Most projects use one of the shipped behaviours unchanged:

- `SlippyMapCameraBehaviour` — fixed camera, map moves underneath. Good for 2D-style maps, navigation overlays, AR/VR.
- `Moving3dCameraBehaviour` — camera orbits a target. Good for 3D city exploration, games.

Touch support is automatic on iOS/Android device builds (compile-time platform split via `(UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR`). No project-side configuration. Touch path can't be tested in the Editor — see `Documentation~/CameraSystem.md` for the workaround.

---

## Step 5 — Default tile material (Android-relevant)

`MapboxMapBehaviour` has a new serialized `Default Tile Material` field under the **Settings** foldout in its custom Inspector. When no `TileCreatorBehaviour` is assigned, this Material is used as the fallback.

For scenes built from scratch in v3.1.0, drag `ElevatedTerrainMaterial.mat` (`Runtime/Mapbox/BaseModule/Unity/Visuals/Materials/`) into this field. The demo scenes ship with it pre-wired.

For scenes upgraded from v3.0 without a `TileCreatorBehaviour`, the previous fallback path used `Shader.Find` — which is unreliable in player builds (the shader gets stripped unless it's in *Always Included Shaders* or referenced from a shipped Material). v3.1.0 throws an `InvalidOperationException` with actionable instructions if both fields are null. Wire the Material reference and you're done.

---

## Step 6 — Rebuild

1. Clear the Library/ cache if Unity gets confused after the dependency change (rare but possible with Burst's first AOT pass).
2. Editor playmode should work without further changes.
3. iOS build: no project-side changes needed beyond making sure your `Info.plist` has `NSLocationWhenInUseUsageDescription` (unchanged from v3.0).
4. Android build: any Managed Stripping Level works. The SQLite cache model classes are preserved via per-property `[Preserve]` attributes and a backup `Plugins/link.xml` declaration — both shipped in the package.

---

## Verification checklist

- [ ] Project compiles
- [ ] Sample scene (LocationBasedGame or WorldMapViewer) renders identically to v3.0
- [ ] Touch pan / pinch zoom / two-finger tilt all work on device
- [ ] iOS build runs and renders tiles
- [ ] Android build runs and renders tiles, **with the SQLite cache populating** (search logcat for `Error inserting … (extended=ConstraintForeignKey)` — should be absent)
- [ ] If you customized `IMapInformation`, `MapInput`, `UnityTileProvider`, `CustomTMSTile`, or `MapCameraBehaviour`: the relevant subclasses still compile and behave correctly
- [ ] If you used `TerrainData.ElevationValuesUpdated` directly or `MapboxTileData.SetDisposeCallback`: migrated to the new event / multicast pattern

If any of the above fails, check the [Reporting issues](../README.md#reporting-issues--contributing) section and file with reproduction details.
