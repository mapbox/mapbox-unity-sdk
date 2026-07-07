# Mapbox Unity SDK

Map rendering, location services, directions, and geocoding for Unity. Distributed as a Unity Package Manager (UPM) package.

| | |
| --- | --- |
| **Package name** | `com.mapbox.sdk` |
| **Current version** | `3.1.0` |
| **Vendor** | Mapbox — <https://www.mapbox.com/> |
| **Minimum Unity** | `2022.3` LTS |
| **Build targets** | iOS, Android |
| **Render pipeline** | Universal Render Pipeline (URP) — required |

For release notes see [`CHANGELOG.md`](CHANGELOG.md). Upgrading from v3.0? See [`Documentation~/Migration-3.0-to-3.1.md`](Documentation~/Migration-3.0-to-3.1.md).

---

## Supported Unity editor versions

| Editor version | Status |
| --- | --- |
| **2022.3 LTS** | Primary target. Android build-settings folder shipped at `Runtime/AndroidBuildSettings/2022.3.38f1/`. |
| **2021.3 LTS** | Best-effort. Android build-settings folder shipped at `Runtime/AndroidBuildSettings/2021.3.41f1/`. |
| **6000.x (Unity 6)** | Best-effort. Android build-settings folder shipped at `Runtime/AndroidBuildSettings/6000/`. |
| **2023.x non-LTS** | Untested. |
| **< 2021.3** | Unsupported. |

When building for Android, copy the `Plugins/` folder from the matching (or closest lower) editor version's subfolder under `Runtime/AndroidBuildSettings/` into your project's `Assets/`. See [Building for Android](#building-for-android) for the full procedure.

## Supported build platforms

Only **iOS** and **Android** are supported build targets. Use the Unity Editor (Windows or macOS) for development and Play-mode testing. Standalone, WebGL, console, and VisionOS are not supported.

| Platform | Status | Notes |
| --- | --- | --- |
| **iOS** | Supported | XCFrameworks under `Runtime/Mapbox/BaseModule/Plugins/iOS/`. ARM64. IL2CPP. |
| **Android** | Supported | Native libs under `Runtime/Mapbox/BaseModule/Plugins/sqlite/Android/libs/` for `arm64-v8a`, `armeabi-v7a`, `x86`. Project-side configuration required — see [Building for Android](#building-for-android). |
| **Unity Editor (Play mode)** | Supported on Windows / macOS for development. | Not a shipping target. |
| **All other platforms** | Not supported. | Standalone, WebGL, console, VisionOS, etc. |

## Dependencies

Resolved automatically by UPM:

| Package | Version | Required | Used for |
| --- | --- | --- | --- |
| `com.unity.render-pipelines.universal` | `10.10.1` | Yes | Terrain and building shaders ship as URP Shader Graphs. |
| `com.unity.nuget.newtonsoft-json` | `3.2.1` | Yes | JSON parsing for tile metadata and API responses. |
| `com.unity.burst` | `1.8.12` | Yes | Burst-compiled terrain decode and collider job. First-time domain reload pays a one-shot AOT compile. |
| `com.unity.inputsystem` | `1.7.0` | Yes | Camera input backend selection is controlled by Player Settings → Active Input Handling, not by this dependency; "Old" (default) keeps legacy `UnityEngine.Input` behavior. See `Documentation~/CameraSystem.md`. |

---

## Installing the package

1. **Clone or download the SDK.** This package is distributed from the SDK repository.
2. **Open Unity Package Manager** in your Unity project (`Window → Package Manager`).
3. **Add the package from disk.** Click the `+` in the top-left, choose `Add package from disk…`, and select the SDK's `package.json`.
4. **Verify.** The Mapbox Unity SDK should now appear in the list of installed packages.

> Editor-only evaluation? You can stop here. iOS and Android each have one extra step at build time — see [Building for iOS](#building-for-ios) and [Building for Android](#building-for-android) when you're ready to ship.

---

## Access token

Every Mapbox API call (tiles, geocoding, directions) requires a Mapbox access token.

- **Set it** via the editor menu: **Mapbox → Setup**.
- **Stored** at `Assets/Resources/Mapbox/MapboxConfiguration.txt`, loaded at runtime via `Resources.Load`.
- An SKU token is appended to signed requests automatically.
- **Get a token** at <https://account.mapbox.com/access-tokens>.

---

## Demo scenes

Five samples ship with the SDK. To import: open the Package Manager, select the Mapbox Unity SDK, go to the **Samples** tab, and click **Import** next to the one you want.

| Sample | What it demonstrates |
| --- | --- |
| **WorldMapViewer** | Minimal map setup — pan / zoom an interactive world map. Best first-time evaluation target. |
| **LocationBasedGame** | Device GPS → in-scene avatar movement. The Pokemon-Go-style starting point. |
| **MapboxComponents** | High-performance component-system-based layer rendering (Buildings + Areas + Roads). |
| **DirectionsApiDemo** | Calling the Directions API + rendering a route on the map. |
| **GeocodingApiDemo** | Forward and reverse geocoding. |

After import each sample lives under `Assets/Samples/Mapbox Unity SDK/<version>/<SampleName>/`. You can copy the relevant pieces into your own project layout and delete the `Samples` folder.

The **LocationBasedGame** sample includes a `LocationBasedMap` prefab that's a useful starting point for any GPS-driven game: drop it into a scene and you have a working location-aware map with a follow-camera and a character that follows the device location.

---

## Working with location

Two ways to drive the map's center / orientation:

### 1. Fixed lat/lon from the Inspector

`MapboxMapBehaviour` has a `MapInformation` field with editable latitude, longitude, zoom, pitch, bearing, and scale. With `Initialize On Start` checked, the map opens at those coordinates at startup.

Good for development and for apps where the map's location is fixed or driven by your own UI (a city tour, a real-estate app filtered by area, etc.).

### 2. Device GPS via the `LocationModule`

The `LocationModule` system swaps in device GPS at build time and predefined test coordinates in the editor. This is what the `LocationBasedGame` sample uses.

To wire it up on a new map:

1. **Disable `Initialize On Start`** on `MapboxMapBehaviour` — the location provider will initialize the map asynchronously when the first GPS fix comes in.
2. **Add `Snap Map To Location Provider`** to your map GameObject. It will move the map center to follow the device location.
3. **Attach `Snap Transform To Location Provider`** to your character / avatar transform to make it follow the device location in world space.

For arbitrary placement (drop a marker at a specific lat/lon, not driven by GPS), see [Place a GameObject at a latitude / longitude](Documentation~/GettingStartedWithMapboxMapObject.md#place-a-gameobject-at-a-latitude--longitude) in the Getting Started guide.

---

## Building for iOS

| Setting | Value |
| --- | --- |
| **Scripting Backend** | IL2CPP (required) |
| **Minimum iOS Version** | 11.0+ recommended |
| **Architecture** | ARM64 |
| **Location permission** | Set `NSLocationWhenInUseUsageDescription` (or `NSLocationAlwaysUsageDescription`) in Info.plist before requesting location. |

XCFrameworks (`MapboxCommon`, `Turf`) ship multi-slice and are picked up automatically by Unity's plugin importer. No further project-side configuration required.

---

## Building for Android

### Step 1 — Copy the build-settings plugin folder

The SDK ships per-editor-version Gradle templates and manifest entries that need to live in your project's `Assets/` folder.

1. Open `Runtime/AndroidBuildSettings/`. There are subfolders named after Unity editor versions.
2. Pick the one matching (or closest lower than) the Unity version you're using.
3. Copy the `Plugins/` folder inside it into your project's `Assets/`.
4. If your project already has custom Android settings, merge them with the copied files rather than overwriting.

Verify the settings landed: in **Project Settings → Player → Publish Settings**, the custom files should be referenced.

### Step 2 — Managed Stripping Level

In Player Settings → Android → Other Settings → Optimization → **Managed Stripping Level**:

- **Disabled / Minimal / Low** — the primary tested target. Recommended for shipping.
- **Medium** — briefly tested in v3.1.0 and appears to work. The SQLite cache's model classes carry `[UnityEngine.Scripting.Preserve]` plus a backup `Plugins/link.xml` declaration so reflection-based row mapping survives stripping. We haven't exercised every code path under Medium; report anything that misbehaves.
- **High** — not tested. Likely works given the `[Preserve]` coverage, but no guarantees. If you need High in production, validate on your specific build before committing.

If you observe `Error inserting … (extended=ConstraintForeignKey)` in logcat at any stripping level, drop to **Minimal / Low** as a workaround and file an issue — the preserve-attribute combination should have prevented it.

### Step 3 — Location permissions

For GPS, your manifest should request:

- `android.permission.ACCESS_FINE_LOCATION`
- `android.permission.ACCESS_COARSE_LOCATION`

Runtime permission prompts are handled by the SDK's `UniAndroidPermission` helper — no project-side code required.

### Step 4 — Build

After the steps above, Build & Run from Unity's Build Settings as normal.

---

## Modules

The SDK is organized into module assemblies. Each is a separate `.asmdef` and can be referenced or excluded independently:

| Assembly | Path | Responsibility |
| --- | --- | --- |
| `MapboxBaseModule` | `Runtime/Mapbox/BaseModule/` | Tile management, caching (memory/file/SQLite), data fetching, coordinate conversions. Foundation for every other module. |
| `UnityMapModule` | `Runtime/Mapbox/UnityMapService/` | Unity-specific map service (`MapUnityService`), quadtree tile provider. |
| `MapboxLocationModule` | `Runtime/Mapbox/LocationModule/` | GPS / compass via `AbstractLocationProvider`. Unity + static (editor) implementations. |
| `MapboxImageModule` | `Runtime/Mapbox/ImageModule/` | Raster tile imagery (satellite, streets). Terrain strategies (flat / elevated). |
| `MapboxVectorModule` | `Runtime/Mapbox/VectorModule/` | Vector tile rendering — buildings, roads, areas. Modifier-stack mesh generation + Component System. |
| `MapboxCustomImageryModule` | `Runtime/Mapbox/CustomImageryModule/` | Custom tileset support (non-Mapbox sources via TMS or arbitrary URL templates). |
| `MapboxDirections` | `Runtime/Mapbox/DirectionsApi/` | Directions API wrapper. |
| `MapboxGeocoding` | `Runtime/Mapbox/GeocodingApi/` | Forward / reverse geocoding API wrapper. |
| `MapboxDebug` | `Runtime/Mapbox/MapDebug/` | Debug logging and benchmarking pipelines. |
| `MapboxExamples` | `Runtime/Mapbox/Example/` | Camera behaviours, input handling, demo support scripts. |

Editor-only counterparts exist for several modules (e.g. `MapboxBaseModule.Editor`).

---

## Documentation

In-package documentation lives under `Documentation~/`. Grouped by what you're trying to do:

**Upgrading from a previous version**
- [`Migration-3.0-to-3.1.md`](Documentation~/Migration-3.0-to-3.1.md) — step-by-step upgrade from v3.0 to v3.1. Read this first if you're upgrading.

**Getting started**
- [`GettingStartedWithMapboxMapObject.md`](Documentation~/GettingStartedWithMapboxMapObject.md) — first-time setup, the `MapboxMap` object, and a place-a-pin-at-lat/lon recipe.
- [`AccessingTheMapObject.md`](Documentation~/AccessingTheMapObject.md) — getting a reference to the map at runtime.
- [`WorkingWithMapObject.md`](Documentation~/WorkingWithMapObject.md) — runtime API.

**Map composition**
- [`WorkingWithModules.md`](Documentation~/WorkingWithModules.md) — module composition pattern, architecture diagrams.
- [`ChangingMapLocation.md`](Documentation~/ChangingMapLocation.md) — moving the map to a new lat/lon.
- [`ChangeImageryStyleOnRuntime.md`](Documentation~/ChangeImageryStyleOnRuntime.md) — switching tile styles at runtime.
- [`CoordinateConversions.md`](Documentation~/CoordinateConversions.md) — lat/lon ↔ Mercator ↔ Unity world space.

**Camera, input, and AR/MR**
- [`CameraSystem.md`](Documentation~/CameraSystem.md) — camera behaviours, touch / mouse input, Input System soft-dep, AR/MR parent-transform guidance.

**Vector content**
- [`VectorLayerModule.md`](Documentation~/VectorLayerModule.md) — building / road / area visualizers, modifier stacks.
- [`ComponentsModule.md`](Documentation~/ComponentsModule.md) — high-performance component-based vector pipeline.
- [`WorkingWithPois.md`](Documentation~/WorkingWithPois.md) — point-of-interest features.

**APIs**
- [`UsingDirectionsApi.md`](Documentation~/UsingDirectionsApi.md) — Directions API wrapper.
- [`UsingGeocodingApi.md`](Documentation~/UsingGeocodingApi.md) — Geocoding API wrapper.
- [`UsingMapMatchingApi.md`](Documentation~/UsingMapMatchingApi.md) — Map Matching API wrapper.

---

## Reporting issues / contributing

This package is maintained by Mapbox internally. Issues and feature requests should be reported through your standard Mapbox support channel.
