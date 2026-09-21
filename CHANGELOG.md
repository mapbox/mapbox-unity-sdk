## CHANGELOG

### v3.2.0 (unreleased)

Correctness work across the tile pipeline, independent of any one feature module. The theme is results that told the caller less than they knew: callbacks that were never invoked, failures reported as successes, single values standing in for several different outcomes, and - in the removals below - public surfaces that advertised access to tile data they could never return.

Adds the Landmarks Module, shipping as **preview** in this release; see `Documentation~/LandmarksModuleKnownGaps.md` for what preview does not yet cover. The module fetches 3D landmark meshes from the Mapbox 3D Buildings tileset, with overlap suppression against both the Vector and Components building pipelines. Landmark types live in the `Mapbox.LandmarksModule` namespace, with the MonoBehaviour wrappers under `Mapbox.LandmarksModule.Unity`. glTFast is consumed as a normal package dependency rather than bundled, and two new hard dependencies are declared: `com.unity.cloud.gltfast` and `com.unity.meshopt.decompress`.

#### Why the dependency shape matters

Landmark tiles are glTF binaries whose buffer views are compressed with `EXT_meshopt_compression`, and the extension is listed in the tiles' `extensionsRequired`. glTFast logs an error and aborts the load when a required extension has no decoder available, so `com.unity.meshopt.decompress` is a hard requirement, not an optional accelerator.

Feature IDs (`_FEATURE_ID_RGBA4444`) arrive in their own dedicated meshopt buffer view — roughly 7 KB compressed per tile — and are needed after glTFast has released its load-time buffers. Rather than patching glTFast to extend that buffer lifetime, the module decompresses that single buffer view directly from the tile bytes it already holds, using the same public meshoptimizer API glTFast itself calls. This duplicates none of glTFast's geometry decoding and keeps glTFast an unmodified package dependency.

The decompressed bytes are held for the duration of scene instantiation and unpacked to `Color` exactly once, straight into each mesh's final vertex buffer. On a representative z14 tile that is about 430 KB held plus a single 1.7 MB color write, rather than materializing a `Color` array per primitive and copying it again to group by mesh.

#### Terrain snapping

Landmark buildings are now driven onto elevated terrain by writing their transform, rather than baking elevation into vertices at generation time. `LandmarkTerrainSnapper`, owned by `LandmarksLayerModule`, samples terrain height per building and writes it as a local Y on the building's transform; a building generated before its terrain tile has arrived is corrected the moment terrain does land, at the cost of one assignment. Compare this to what happens to vector features today: `SnapTerrainModifier` bakes elevation into `MeshData` once, at generation time, and nothing ever re-snaps, so a vector building generated a frame too early stays at sea level on a hillside for the rest of the session.

Elevation is cached in metres rather than in Unity units, so a map scale change - which happens on every zoom step under `DynamicScalingMapInformation` - only changes the divisor used to convert metres to Unity Y, and needs no re-sampling of terrain at all. Only terrain actually arriving triggers a re-query, and then only for buildings that don't already have a reading.

A building snaps to the lowest point sampled across its footprint, not the point under its centre, so it beds into a slope the way a real foundation does: snapping to the centre height would leave the downhill edge floating above the terrain on any slope. It falls back to the anchor point when a tile published no footprint for that landmark.

The `LandmarkTour` sample's `MapFlightController` gained crossfaded climb, cruise, and descend phases in the same pass, so the camera never comes to a full stop between them while flying between landmarks.

#### Security

- `access_token` and `sku` are now appended only to requests whose host is `mapbox.com` or a subdomain, over https. Other hosts are requested without credentials. Proxy or custom endpoints that need credentials must include them in the URL template.

#### Fixed

- Callbacks that were silently dropped, each of which could park a coroutine forever. `VectorLayerModule.CreateVisual` returned without invoking its callback when mesh generation for the tile was already in flight, leaving `CreateVisualCoroutine` spinning on `Runnable` for the rest of the session and hanging `LoadTileCoverToMemory` with it, so `LoadViewCompleted` never fired. `UnitySource.RequestData` did the same for a tile id already being fetched; a second request now joins the in-flight fetch and receives its real outcome.

- Failed texture cache reads were delivered as ordinary cache items carrying a null `Texture`, indistinguishable from a hit. `MapboxCacheManager.GetImageAsync` already had the code to turn that into a miss, but `MapboxTileData.HasError` was never assigned anywhere in the SDK, so the branch was dead and the unusable item was cached as real - a permanently blank imagery tile, or a terrain tile stuck `Temporary` and re-polled every frame. `GetImageCoroutine` gained the same translation its async sibling already had.

- `DataTaskWrapper` ignored both signals `TaskManager` gives it, so a cancelled task, a task that threw, and a genuine cache miss all arrived as an identical null result - which `ImageSource` then dereferenced. Its sibling `MeshGenTaskWrapper` already made the distinction.

- Elevation queries could not express "I don't know". `QueryElevation` returned 0 for genuine sea level, for terrain not loaded yet, and for CPU elevation extraction being switched off. `TerrainLayerModule.GetElevationData` additionally dereferenced its loaded data without checking it, so a failed or cancelled terrain fetch threw inside the callback and the caller's own callback was never invoked at all.

- `CompositeLayerModule.LoadTiles` called each child and discarded the returned `IEnumerator`. Every implementation is an iterator method, so no child module ever loaded anything and the composite reported itself finished immediately. `RetainTiles` likewise discarded each child's readiness and hardcoded `true`.

- Area and building triangulation silently dropped features too large for the index buffer. The retry allocated a buffer of the same size, so a feature whose own triangulation exceeded it failed identically and was skipped without a message - visible as water disappearing in patches at low zoom, where a single coastline ring needs more indices than the pool provides.

- `MapboxMapVisualizer` could show a tile and its own children simultaneously during a fast multi-level zoom-out, producing flickering coplanar geometry at the map centre. `DelveInto`'s filler-adoption branch hoisted an adopted tile's already-visible children instead of pooling them, and `ShowTiles` then repositioned and showed pass-through tiles that were meant to stay hidden.

- Feature filters allocated on every feature of every tile, and threw on features missing the property they test. Filtering is pure overhead on top of the mesh work it gates, so the whole path is now allocation-free:
  - `VectorFilterStack.Try` used LINQ `Any`/`All` with a lambda capturing the feature, so a closure, a delegate and an enumerator were allocated per feature. Index loops now, as `ComponentFilterStack` always had.
  - `FeatureStringPropertyFilter` lowercased both the feature's value and its own filter string on every feature - three allocations per comparison - and indexed `Properties` directly, throwing `KeyNotFoundException` on any feature without the property; whole layers arrive with no tags at all. Compares with `StringComparison.OrdinalIgnoreCase` in place, and the `Contains` set uses an ordinal-ignore-case comparer.
  - `FeatureNumberPropertyFilter` did `float.Parse(properties[name].ToString())`: a string allocation and a culture-dependent parse per feature, plus the same `KeyNotFoundException`. Unboxes the numeric types the decoder produces directly, and parses strings with the invariant culture.
  - `HasPropertyFilter` dereferenced a null `Properties`.

  A feature that lacks the property is now not a match, which for an inverting filter means it passes.

- The custom imagery module's `Source Type` and `Custom Source Id` never reached the Inspector. `CustomApiLayerModuleScriptEditor` draws its fields by hand, and those two were not in the list, so choosing a Mapbox style through this module was not possible from the Editor at all - the dropdown existed on the class and nothing showed it. Both are drawn now, with the url fields shown only for source type `Custom`, since a Mapbox style needs no url.

- The custom imagery module's `Source Type` dropdown did nothing, and every custom imagery layer shared one cache namespace. `CustomApiLayerModuleScript` resolved the chosen style into `Settings.DataSettings.TilesetId` and then built its source from a fresh `ImageSourceSettings` with a hardcoded `"CustomImagery"` id, discarding the resolved style along with the retina, cache-size and data-level-clamp settings next to it. So a custom imagery layer only worked with an explicit `UrlFormat`, picking `Mapbox Streets` had no effect, and two custom layers on different URLs collided in the cache because they keyed on the same name. `DataSettings` is now passed through, falling back to `"CustomImagery"` only when nothing resolved an id (source type `None`, or `Custom` with the id left empty) so the cache still has something to key on.

- `ShiftMapForDistance` never recentred during normal walking. It compared each location fix with the previous one, and walking produces small steps that never reached `ShiftMapDistance`. The player drifted from the world origin for the whole session, accumulating float precision error. It now compares each fix with the map centre. Now that recentring fires, what it leaves behind matters: tiles reposition on a recenter, but nothing else in the scene does, so the player and camera rig were left an offset away and visibly walked back. The component gained `TransformsToShift` for the Inspector case, and the `OnRecentered` event listed under Added for everything spawned from code. The listed transforms are shifted *before* `ChangeView`, because the tile cover is built during that call and `TransformBasedTileProvider` reads its transform then. Shifted afterwards, the map would load tiles around a spot one offset away from the player. The shift also goes through `MapRoot`, because `ConvertLatLngToPosition` works in map-local space, so a moved or rotated `MapRoot` (an AR anchor, say) shifts world-space objects by the right amount.

- Asking for a vector tileset the map does not carry failed silently. `MapUnityService.GetVectorSource` returns the one shared vector source - deliberate, since the Vector Tiles API composes tilesets and a second source means a second request and cache entry per tile - but whoever calls first defines the tileset for everyone after them. A module asking for `mapbox-terrain-v2` after something took `mapbox-streets-v8` received a source that would never carry a layer it needed, with no error and its tileset id discarded. It now logs an error naming both ids, the tilesets actually missing, and the composed id to use instead. Behaviour is unchanged otherwise; composing the ids is still the caller's job.

- The Inspector's enable toggle on filters and modifiers did nothing. `FilterBaseObject`, `ScriptableMeshModifierObject` and `ScriptableGameObjectModifierObject` each carry an `m_Active` field that the filter- and modifier-stack Inspectors draw as a tickbox, and no runtime code read it - so an unticked filter still filtered and an unticked modifier still ran. Inactive entries are now dropped when the stack is built, which also costs nothing per feature. Empty slots in `GoModifiers` are dropped with them: `ModifierStack.RunGoModifiers` does not null-check, unlike its mesh counterpart.

- `GeometryTypeFilter` could not match polygons by their multi-part name, and half its cases were unreachable. It switched on `GeometryType.ToString().ToLowerInvariant()` against GeoJSON-style names, but an MVT `GeomType` is only `UNKNOWN`, `POINT`, `LINESTRING` or `POLYGON` - multi-part geometry arrives as several rings on one feature, not as a distinct type - so the `multipolygon`, `multilinestring` and `multipoint` arms could never match. The first of those also had a space inside the case literal. Now switches on the enum, which drops a `ToString` plus `ToLowerInvariant` per feature as well.

- `VectorFilterStack.Initialize()` was an empty body. The only code initializing filters was `ModifierStack.Initialize`, reaching in through the stack's `Filters` list, so a filter stack used without a modifier stack initialized none of its filters - and the reach-in dereferenced the list without a null check. The stack now initializes its own filters and the modifier stack asks it to.

- One unready module stalled every module registered after it. `MapboxMapVisualizer.CreateTile` broke out of the module loop on the first `LoadInstant` returning false, and `CompositeLayerModule.LoadInstant` returned on it, so a module still waiting for data denied every later module its chance to progress on that tile - and because the loop restarts from the beginning on each update, it kept denying them for as long as it stayed unready. Both now offer the tile to every module and combine the answers, as `CompositeLayerModule.RetainTiles` already did. "I need more time" and "abort this tile" are no longer the same signal.

- `Conversions.Tile01ToLatitudeLongitude(Vector2, CanonicalTileId)` mirrored longitude about the tile's centre meridian. It lerped `East → West` where its own forward function, `LatitudeLongitudeToInTile01`, places x=0 at West, so a round trip returned a point reflected by the full tile width - correct only at the tile centre, which is where both existing tests happened to sample. The `Vector3` overload was always right; it reads Unity tile space, a deliberately different convention now documented on both. No runtime code called the broken overload.

- Negative integer feature properties were unreadable. `vector-tile-cs` reads every varint as unsigned, so an MVT `int_value` - a signed int64 - arrives boxed as `ulong` and a contour band 200 m below sea level read as 18446744073709551416 instead of -200. Both San Francisco and the Amazon ship those bands, so `Convert.ToInt32(properties["ele"])` returned garbage on real data. `VectorLayerVisualizer.GetFeature` now normalizes integers to `long` through `MvtProperties.NormalizeIntegers`, which allocates nothing for the common case of a feature carrying no unsigned value. The Components module's own reader already boxed these as `long` and is unchanged.

- The Components module's serialized settings never reached its Inspector. `MapboxComponentsModuleScriptEditor` drew the visualizer list and its two buttons and never called `DrawDefaultInspector`, so `RejectTilesOutsideZoom`, `ClampDataLevelToMax` and `CacheSize` were invisible - subclassing remained the only way to change the zoom range, which is precisely what promoting them to serialized fields was meant to remove. The editor now enumerates the serialized properties rather than naming them, so a field added to the script appears without anyone remembering the editor file.

- Landmarks did not reliably hide the basic OSM buildings they overlap. `GltfVisualizer` published each landmark footprint as the footprint mesh's raw vertex array, which is in storage order rather than boundary order, while every consumer — `MapboxGeometryUtilities.PolygonsIntersect` via both `LandmarkToBuildingComponentConnector` and `LandmarkPolygonFilterObject` — requires consecutive vertices to be adjacent along the outline. A four-vertex quad footprint survived that by luck; a detailed one described a self-intersecting scribble, so overlap tests against it were unreliable and some overlapping buildings stayed visible. Reported at the Eiffel Tower, whose footprint is exactly the detailed case.

  Footprints are now published as true outlines. `MapboxGeometryUtilities.ExtractBoundaryRings` welds coincident positions (exporters split vertices for differing normals and UVs, and without welding every edge looks like a boundary edge), keeps the directed triangle edges whose reverse is absent, and walks them into ordered rings. A closed mesh has no boundary edge at all, so it falls back to the convex hull of the X-Z projection rather than contributing nothing.

  Two related defects in the same method are fixed alongside. Footprints are now collected in `AddPrimitive` rather than `EndScene`, which means a footprint node with more than one primitive contributes all of them instead of only primitive 0, and a footprint node nested below a root node is collected instead of being missed entirely. `EndScene` now walks the node hierarchy to deactivate nested footprint geometry, while still normalising only root-level transforms — rewriting a child's transform would detach it from its parent's placement.

- Every landmark tile leaked its meshes, materials and textures for the rest of the session. `GltfVisualizer` created a `MapboxGltfImport` per tile and dropped the reference, and disposing that import is the only thing that destroys what glTFast built for it - Unity reclaims a `Mesh`, `Material` or `Texture2D` only on a non-additive scene load, so destroying the tile's GameObjects left the whole PBR texture set resident. On iOS and Android, the only supported build targets, panning across a city was an unbounded climb toward an out-of-memory kill. The import now travels with the visuals it produced, on `GltfGenerationTaskResult.Import`, and is disposed by `LandmarksLayerModule` at the moment it destroys them - not earlier, since Dispose destroys the very meshes the live renderers are using.

- A glTF load that finished after the map was torn down, or after its own tile's data had been evicted, was recorded as though nothing had happened. Generation cannot be cancelled, so the continuation resumed into a scene that no longer existed: it parented into a destroyed hierarchy, cached visuals into a dictionary `OnDestroy` had just cleared, and raised `TileFinished` for a tile that would never raise `TileDisposed` - leaving both overlap connectors suppressing vector and component buildings at that location permanently. Both the visualizer and the module now carry a destroyed flag checked after every await, the module records a tile whose data is disposed mid-load, and a result with nowhere to go is released on arrival instead of cached.

- A landmark tile that failed to generate was retried forever. Nothing remembered a failure, so a tile absent from the visual cache and not in work looked exactly like one nobody had tried yet, and `DataProcessingFailure` additionally invalidated the cached bytes - which drops them from memory *and* disk, so the next sweep re-downloaded them. A genuinely malformed tile therefore re-downloaded, re-failed and logged an error on every sweep for the whole session. Generation is now capped at two attempts per tile, the second failure gives up and says so once, and the counters reset if the tile's data is re-fetched. `GameObjectFailure` no longer invalidates data that parsed correctly.

- `GltfVisualizer.FlipYZ` read `matrix[0]`, `[5]`, `[12]` and `[13]` off a glTF node with no guard, and wrote a normal per vertex without checking there were any. glTF lets a node carry TRS instead of a matrix, and a primitive may have positions and no `NORMAL` attribute; either threw inside `AddPrimitive`, was swallowed by the catch in `Generate`, and blanked the entire tile. Both are guarded, and the matrix case warns rather than failing silently. `CaptureAnchor` had the matrix guard already - `FlipYZ` did not.

- `EndScene` and `HideNestedFootprints` indexed glTFast's node dictionary directly. It only holds nodes glTFast actually materialised a GameObject for, and it is free not to materialise one, so a tile shaped that way threw `KeyNotFoundException`. Both use `TryGetValue` now, as `CaptureAnchor` already did.

- Exceptions thrown by the generation result callback were lost. The callback runs outside `Generate`'s own try/catch by design, so anything it threw was captured into a `Task` that `GenerateVisualsForTile` discarded and `CreateVisualCoroutine` polled without ever reading `Exception` - and the coroutine's `Status` test treated a faulted task as finished, reporting success to the tile pipeline. Both paths now surface the fault.

- `LandmarkToVectorConnector` never received the hardening its sibling connector did. It dereferenced all four of its Inspector fields unconditionally, so an unassigned one was a `NullReferenceException` in `Awake`, and it subscribed a closure to `Map.Initialized` that `OnDestroy` could not detach - `Initialized` is a plain `Action` field, so the closure kept the destroyed component alive and ran against it. It now warns and disables itself on a missing reference, subscribes a named method, and detaches it. `LandmarkHighlightBehaviour` had the same problem twice over and no `OnDestroy` at all, so a destroyed selection component carried on adding a `LandmarkComponent` and cooking a `MeshCollider` for every landmark that loaded.

- A landmark module with no Base Material assigned fell through to `new Material(Shader.Find("Standard"))`. `Standard` is the built-in render pipeline's shader and this SDK requires URP, where `Shader.Find` returns null in a player build - so the fallback produced either a material with no shader or magenta geometry, and leaked one material per import. `LandmarksLayerModuleScript` now logs an error against the component, and the visualizer reports every tile as empty rather than throwing its way through the cover.

- Importing the `LandmarkDemo` sample on its own gave a scene with missing materials. Its ground material and three road materials resolved to assets that live in the `MapboxComponents` sample, and Package Manager samples are imported individually - so the one sample the Quick Start tells a new user to import was the one that did not work alone. It now references its own copies, which were already in the folder and already used by its road style sheet; only the scene and the road preset pointed elsewhere.

- `LandmarkDemoWithVector`'s building material carried a hidden HDRP `AssetVersion` sub-asset in a URP-only package, which logged a missing-script warning on import.

#### Changed

- `ILayerModule.Initialize` now takes an `ITileLifecycleSource`, replacing the separate `ITileLifecycleObserver` hook. A module previously had no reference to whatever was driving it - neither the constructor nor `ConstructModule` supplies one - so an extra opt-in interface existed purely to hand the visualizer over. **Breaking** for anyone implementing `ILayerModule` directly or subclassing an SDK module and overriding `Initialize`. Nothing here is serialized, so no scene or prefab data is affected.

- `CanonicalTileId.ParentAt` no longer mutates the tile it is called on. It walked `this` up through `MoveToParent` and returned itself, so on a struct `var parent = id.ParentAt(11)` silently changed `id` as well - a trap the SDK's own callers defended against by copying the struct first, and one its sibling `UnwrappedTileId.ParentAt` never had. **Breaking** only for code that relied on the side effect; every call site in the SDK used the return value. `MoveToParent()` is unchanged for anyone who wants the mutation.

- `TaskResultType` gained `AlreadyGenerated`, `AlreadyInProgress`, and `NoData`, so "nothing to do because it is done", "nothing to do because someone else is doing it", and "nothing to do because there was genuinely nothing to build" are no longer all reported as `Success` - the one member that promises generated objects are attached. `NoData` covers a source finding nothing to build for a tile - most tiles contain no landmarks - which is a legitimate, common outcome and explicitly not a failure: the data is correct and must not be invalidated or re-fetched, unlike `DataProcessingFailure`. A caller should still record the tile as resolved on `NoData`, or it will regenerate it forever.

- `MapboxComponentsModuleScript` exposes `RejectTilesOutsideZoom`, `ClampDataLevelToMax` and `CacheSize` as serialized fields. It built its `VectorModuleSettings` entirely in code, so the Components module drew nothing below zoom 12 and the only way to change that was to subclass it and reimplement the whole build method. Defaults match the previous hardcoded values, so existing scenes are unaffected. `GetVectorLayerModule` remains virtual for what the Inspector cannot express, such as a different tileset.

- Landmark selection's two Inspector fields are renamed before the API freezes: `LandmarkHighlightBehaviour.HightlightColor` loses its typo and becomes `HighlightColor`, and the public `UnityEvent` `_visualSelectedEvent` - a serialized field carrying a private field's name, declared below the private methods - becomes `VisualSelected`. Neither has shipped, so no scene data is affected; renaming either after release would have broken both source and serialized scenes.

- The module's internals are no longer public API. `GltfVisualizer`, `GltfGenerationTaskResult`, `MapboxGltfImport`, `MapboxGltfMaterialGenerator`, `LandmarkAnchor`, `LandmarkPolygonFilter`, `FeatureIdColorTable`, `FeatureIdDecoder` and `FeatureIdMeshWriter` are `internal`, joining `LandmarkTerrainSnapper`, `GlbBinaryChunk` and `MeshoptBufferViewDecoder`, which already were. Nothing outside the assembly constructed or called any of them. The glTF schema types stay public: they are Newtonsoft deserialization targets, and that is the one place where narrowing accessibility fails at runtime rather than at compile time.

- `MapboxLandmarksModule.asmdef` is gated on glTFast being installed. It carries no define constraint before now, so a project whose manifest lacks `com.unity.cloud.gltfast` - a hand-edited manifest, a stale lockfile, an air-gapped or scoped-registry setup - failed to compile *entirely*, for a user who never touched landmarks. A `versionDefines` entry raises `MAPBOX_GLTFAST_PRESENT` and `defineConstraints` requires it, so the assembly is simply left out instead. The trade is worth stating: scenes referencing landmark components then show missing-script references rather than a compile error, which is the better of the two failures.

- The landmark data zoom is one constant, `LandmarksLayerModule.LandmarkDataZoom`, rather than a literal `14` repeated across the module, the polygon filter and both overlap connectors.

- `LandmarksLayerModule.BuildingLoaded` carries the landmark id: `Action<GameObject, string>` rather than `Action<GameObject>`. It is also raised by walking out from each building's anchor rather than down from the tile root, because the anchor is the only thing that knows which id a mesh belongs to. `BuildingUnloaded` is removed with it - see Removed.

- Landmark glTF loads can be cancelled. `GltfVisualizer` keeps a `CancellationTokenSource` per in-flight tile and threads the token through both `Load` and `InstantiateSceneAsync`; a cancelled load unwinds, releases its own import and GameObjects, and reports `TaskResultType.Cancelled`, which the module deliberately does not count as a failure - the tile is left unresolved and regenerated if it returns to the cover. `Cancel` is called when a tile's data is evicted mid-load, and for everything still running at teardown.

  A tile merely dropping out of the retained set is **not** cancelled, and that is deliberate rather than an omission: `LoadTileCoverToMemory` legitimately generates tiles outside `_retainedBuildingTiles`, so cancelling on un-retain would abort the flight controller's own preloading.

#### Removed

- Five commented-out extension-point source files - `TypeFilterObject`, `PolygonCollisionFilterObject`, `FeatureIdFilterObject`, `MeshOnPointModifierObject` and `MeshOnLineModifierObject`. Each was commented out top to bottom, `CreateAssetMenu` attribute included, so they contributed no type, no menu entry and no asset - only the impression, to anyone reading the source for extension points, that these existed. Residue of the v3.0.4/3.0.6 passes; git history keeps them.

- The public surfaces that looked like the answer to "how do I read tile data" and were never wired to anything. **Breaking** as API, inert in behaviour - none of them could return data, and nothing in the SDK called them:
  - `ByteArrayTile.Data`, `LayerNames()`, `GetLayer()`, `SetVectorFromCache()` and `Dispose(bool)`. The backing field was never assigned by the pipeline - vector bytes are decoded into `VectorData` by the vector module instead - so `LayerNames()` and `GetLayer()` threw `NullReferenceException` on any tile the SDK produced, despite the class documenting a worked example of consuming `Data`. The class doc now describes what the type actually holds: undecoded bytes.
  - `Source<T>.DataFetched`, `DataFetchedWithCallback` and `FetchingError`, declared but never invoked and with no subscribers anywhere, and `SetMeshGenerationMethod`, which had neither an override nor a caller. `TileErrorEventArgs` went with `FetchingError`, its only consumer.
  - `UnityTileVectorContainer`, along with `UnityMapTile.VectorContainer`. Nothing ever populated it: `SetVectorData` had no callers, so `Data` was permanently null and `GetAndClearVectorData()` was called once, in `Recycle`, purely to discard nothing. `ImageContainer` and `TerrainContainer` are unaffected.

  A read path for vector features is worth having; these were not it, and a developer could reasonably spend hours believing otherwise.

- `LandmarksLayerModule.LandmarkFootprintCreated` and `LandmarkMeshResults`. Both were public `Action` fields that nothing ever invoked - the only code that would have raised them was commented out in the constructor - so they read as supported extension points and could never fire. `TileFinished` carries the footprints and is live.

- `GltfGenerationTaskResult.InvalidateAndRetry`, never written and never read.

- The tile-root object pool in `GltfVisualizer`. It pre-warmed twenty GameObjects, but the success path hands each root to the module, which destroys it on release rather than returning it - so the pool recycled nothing and only deferred the first twenty allocations.

- The unreferenced `Areas` visualizer assets and materials left in the `LandmarkTour` sample when its water and landuse visualizers were dropped from the scene, and two orphaned ground materials.

- `LandmarksLayerModule.BuildingUnloaded`. It had no subscribers, and despite the name it fired once per tile root rather than once per building - which `TileDisposed` already reports, at the same moment, with the tile id a caller actually needs.

#### Added

- `IMapInformation.TryQueryElevation`, which reports whether an elevation is actually known rather than returning 0 for both "sea level" and "no data". The existing `QueryElevation` remains for callers with no useful response to "unknown".

- `Mapbox.BaseModule.Utilities.MvtProperties.NormalizeIntegers`, public for anyone reading feature properties off their own decode path and hitting the same unsigned-int64 problem described above.

- `UnitySource<T>.TilesetId`, so a caller can see which tileset - or which composed, comma-separated list of tilesets - a source it was handed actually fetches.

- `MapboxLandmarksModule` assembly: `LandmarksLayerModule` plus its `LandmarksLayerModuleScript` MonoBehaviour wrapper, driven by `Constants.Map.LandmarksTilesetId`.
- Overlap suppression: `LandmarkToVectorConnector` + `LandmarkPolygonFilterObject` reject colliding footprints before mesh generation; `LandmarkToBuildingComponentConnector` zeroes colliding vertices after generation, since the Components module merges buildings into a single mesh.
- `Mapbox.BaseModule.Utilities.MapboxGeometryUtilities` now ships in `MapboxBaseModule`, where its namespace always said it lived. It was public already, but it was compiled into the landmarks assembly, so nothing could reach it without referencing that. Its contents are unchanged: 2D X-Z polygon intersection, point-in-polygon tests, bounds, boundary-ring extraction, and convex hull, for anyone else needing the same collision-detection primitives the landmark overlap connectors use.
- Feature ID support, split across `FeatureIdSchema` (captures the custom vertex attribute via `JsonExtensionData`), `GlbBinaryChunk`, `MeshoptBufferViewDecoder`, `FeatureIdColorTable`, `FeatureIdDecoder`, and `FeatureIdMeshWriter` (adds Color as an extra vertex stream, because glTFast builds meshes through the low-level mesh API).
- `LandmarkHighlightBehaviour`, a raycast-and-tint selection behaviour for landmark buildings. Ships as code only: it is on none of the three sample scenes, so click-to-select needs the component wired in by hand until a sample demonstrates it.
- `LandmarkDemo` and `LandmarkDemoWithVector` samples, plus `LandmarkTour`, which flies the camera between famous landmarks. `LandmarkTourController` and `LandmarkTourDestination` build and drive the destination list from a UI prefab, `MapFlightController` runs the flight itself (climb, cruise, descend, each phase crossfaded into the next), and `MapCameraClipPlaneDriver` scales the camera's near and far planes to match, for a scene spanning zoom 4 to 17.
- Documentation: `LandmarksModule.md`, `LandmarksModuleInternals.md`, and `LandmarksModuleKnownGaps.md`.

- `IMapInformation.TryGetViewCenterPosition` / `TryGetViewCenterMercator` / `TryGetViewCenterLatLng`, reporting what the camera is actually looking at - where a ray through the centre of the viewport meets the map plane. The map's own centre is not always the view centre: a camera that pans by recentring the map keeps the two together, while one that keeps the map origin fixed and moves itself does not, and then `CenterMercator` is the world origin rather than anything on screen. They report `false` instead of a placeholder when there is no such point, which is the honest answer for a camera pitched at or above the horizon, where the centre ray is parallel to the plane or points away from it. Computed on demand rather than cached on the map, so no reader can see a stale value. The plane is world y = 0, matching the rest of the SDK's screen-to-map math, so a moved, rotated or reparented MapRoot is not accounted for, and terrain height is ignored.

- A custom imagery layer can now sit on the map without drawing on it, and can fetch coarser than the map draws. `CustomApiLayerModuleScript` gains `Mode` - `Display` (unchanged), `DisplayNonBlocking` (draws, but never holds a tile back), `DataOnly` (fetches and caches, draws nothing) - and `DataZoomOffset`.

  `DataOnly` is what makes a second imagery source possible at all: a tile has one imagery slot, so two drawing layers overwrite each other and the visible map, not just the second consumer, ends up wrong. Read such a layer with the new `RasterSourceMinimapTileSource`, and a minimap can show a different style from the map it mirrors - streets on the minimap over satellite on the map.

  `DataZoomOffset` of -2 fetches one tile per 16 map tiles, cutting requests and cache entries by the same factor for a quarter of the linear detail, which a minimap drawing the imagery small does not miss. Placement needs no work at either end: consumers already derive the sub-rect from the fetched tile's own id, the same way terrain shares one data tile across sixteen render tiles. Nothing in the imagery module changed - the new `CustomImageryLayerModule` overrides only members that were already virtual.

- A minimap component in the imagery module: `MinimapScript` for drop-in use, and the pieces it delegates to for anything beyond that. `IMinimapTileSource` decides what a tile shows (`TerrainHeightMinimapTileSource` and `ImageryMinimapTileSource` ship; implement it for fog-of-war, team colours, or any per-tile texture of your own), `IMinimapRenderer` decides how it is drawn (`RawImageMinimapRenderer` draws pooled UI images; implement it for world-space or render-texture minimaps), and `MinimapProjection` is the layout math on its own, free of both. `MinimapView` ties them together and can be driven directly without the MonoBehaviour.

  `SourceModule` is a dropdown of the scene's imagery, custom-imagery and terrain modules, not an object field. It has to be: modules are components, several sit on one GameObject, and dragging that object makes Unity take whichever `ModuleConstructorScript` is ordered first. The scan covers the whole scene because a module need not be on the map - `CompositeLayerModuleScript` builds a list of module scripts wherever they live - and each entry names its owning GameObject. Composite, vector and component modules are left out, since a minimap cannot draw them. A module that is in the scene but that the map never builds is labelled and explained rather than silently producing an empty minimap. It has to be: every layer module is a component on the map's own GameObject, so dragging that GameObject onto an object field cannot say which module was meant - Unity takes whichever `ModuleConstructorScript` is ordered first. The dropdown names each module by type, numbers duplicates, and marks disabled ones as ones that will not build.

  Drop a layer module on `SourceModule` to draw that module's data rather than whatever the map left on the tile - the Inspector route to a minimap on a different style, with no code. An imagery or custom-imagery module resolves to its raster source, a terrain module to the terrain height texture, and anything else logs what it was and falls back. Leave it empty for the zero-config case, where `Source` picks between the terrain and imagery already on the tile.

  Set `ViewCenterCamera` to the map camera for any camera that moves rather than recentring the map - `Moving3dCamera` and most 3D rigs. The minimap then centres on what that camera is looking at. Prefer it over `ViewCenter` for any pitched view: the camera sits behind and above the point it is looking at, by 100 units of ground distance at 45 degrees of pitch and over 1100 at 5 degrees, so centring on the camera's own position pushes the view centre off toward the edge of the panel. `ViewCenter` remains for centring on a thing rather than a view - a player marker, say.

  Either is needed because those cameras never report a centre back to the map for any camera that moves itself rather than recentring the map - `Moving3dCamera` and most 3D rigs. Those never report a centre back to the map, so `MapInformation.CenterMercator` stays at the start position for the whole session while the view travels away from it; a minimap centred on it holds the origin while the tiles march off the panel. Leave `ViewCenter` empty for `SlippyMapCamera`, which does report its centre.

  `ZoomOffset` is fractional, not whole levels: it only reaches the tile-size exponent, so -1.5 is a valid step. Tile alignment is unaffected - that is the reference zoom's job.

  `BorderThickness` and `BorderColor` drive an optional ring around the panel. Thickness is per-edge pixels rather than a raw scale, since the border is a uniformly scaled copy of the panel and the scale that yields a given width depends on the panel's size (7.5px on a 300px panel is a scale of 1.05). Zero thickness or a transparent colour hides it, and it updates in the scene view without entering play mode.

  A `Minimap.prefab` ships with it, ready to drop under a Canvas: a circular-masked 300x300 panel with the content, rotation and mask nodes already wired to the component. Assign a map to it and it works; everything else is optional. The circular mask is a generated sprite, so swapping the shape is one field on the mask Image.

  Three things in it are less obvious than they look, and are the reason this is a component rather than a snippet in a sample. It reconciles against `MapVisualizer.ActiveTiles` every frame instead of following `TileLoaded`/`TileUnloading`: the dictionary is complete at every instant it can be read, whereas mirroring it from two events with different latencies reintroduces the one-frame hole that shows up as flicker on every zoom change. Tiles are laid out in units of one reference zoom taken from the camera, not from the active tiles, so the routine mix of coarse stand-ins and fine tiles aligns, and a tile that never unloads cannot freeze the minimap's scale. And the tile-size conversion scales by the fractional part of the zoom, so the minimap does not hold one size per zoom level and pop at each integer tick.

- `ShiftMapForDistance.OnRecentered`, raised once per recenter with a `MapRecenteredArgs` carrying the previous and new centre and the offset every geographic point moved by, in both map-local (`LocalOffset`) and world (`WorldOffset`) space. It is for objects the Inspector cannot list: markers, route lines, other players' avatars, cached world positions. It fires after the map and `TransformsToShift` are in the new frame, so a handler can either subtract the offset or place objects again with `ConvertLatLngToPosition`. It also fires after the tile cover is built, so transforms a tile provider follows still belong in `TransformsToShift`. Documented in `RecenteringLocationBasedMaps.md`. Both `LocationBasedGame` sample scenes now list the astronaut, `CharTarget` and `CameraRoot` in `TransformsToShift`, so the sample recenters without the walk-back.

- Click-to-select, wired into a scene and able to say which landmark was clicked. `LandmarkHighlightBehaviour` already raycast, tinted and raised an event, but it appeared in no sample and a hit could not be traced back to a landmark - the tile publishes an id on each building node, and nothing downstream recorded it. It is now a component on the `LandmarkDemo` map with its shipped defaults, `LandmarkComponent` carries `LandmarkId`, and the behaviour exposes `SelectedLandmarkId` for an Inspector-wired handler that only receives the GameObject. Selection is still whole-mesh: feature ids are decoded per-vertex and drive per-part appearance in the shader, but mapping a hit back to one would need the exact render mesh as a collider rather than the convex hull that keeps load cost down.

- `LandmarksLayerModuleScript.TilesetId`, so the 3D tileset the module fetches from is an Inspector field rather than a constant compiled into the SDK. It defaults to `Constants.Map.LandmarksTilesetId` and falls back to it when left empty, so a scene saved before the field existed is unaffected.

#### Tests

- `MapboxGeometryUtilitiesTests` (13 cases) - the first coverage of any kind for this code, and specifically a regression net under `ExtractBoundaryRings`, which is the fix described above and shipped with none. Covers a quad returning one ring in true boundary order (adjacency, not just membership), an annulus returning two rings walking in opposite senses, a closed mesh falling back to the convex hull of its X-Z projection, and vertices split within the weld tolerance still collapsing into one ring; plus `PolygonsIntersect`, `IsPointInPolygon` and both `BoundsOfVertices` overloads.

  Two of them pin down behaviour rather than assert it is right, and are named so. `PolygonsIntersect` reports two squares sharing exactly one edge as intersecting, but not for the reason it looks like: the shared edge is exactly collinear, which `LineSegmentsIntersect` rejects as parallel, and what actually trips is a different pair of edges meeting at the shared corner, since the segment test's parameter ranges are inclusive of their endpoints. And `IsPointInPolygon` uses the standard half-open ray-cast convention, so a vertex and one of the two edges meeting at it count as inside while the opposite edge counts as outside. Both are worth knowing before the overlap algorithm is revisited.

#### Dependencies

- `com.unity.cloud.gltfast` (6.18.0). 6.18.0 is the last release supporting Unity 2022.3; 6.19.0 and later require Unity 6.0 LTS. This is declared as a minimum, not a pin: the module uses no glTFast API that changed in 6.19.0 and no API marked obsolete, so projects on Unity 6 may raise the version in their own manifest without further changes.
- `com.unity.meshopt.decompress` (0.2.0-exp.1). Mandatory, with no degraded mode: the landmarks API is meshopt-only, the tiles list `EXT_meshopt_compression` in `extensionsRequired`, and glTFast aborts any load whose required extension has no decoder - so without this package there are no landmarks at all, not landmarks without feature IDs. `MapboxLandmarksModule.asmdef` therefore requires it through `defineConstraints`, exactly as it requires glTFast, and a project without it gets no landmarks assembly rather than a broken one. The separate `MESHOPT_IS_RECENT` `versionDefines` entry is a narrower thing: it gates only the module's own feature-ID decode path, which needs a meshopt API newer than the 0.1.x package exposed. An older-but-present package therefore still draws landmarks, just untinted by feature id.

Note that the dependency on glTFast is not code-only: `glTF-pbrMetallicRoughness-Clearcoat_MAPBOX.shadergraph` and `PBR_metallic_roughness_advanced_MAPBOX.shadersubgraph` reference seven of glTFast's shader subgraphs (`ScreenGrab`, `MetallicRoughness`, `Clearcoat`, `Normal`, `TextureTransform`, `Occlusion`, `Emission`) by asset GUID, so glTFast's `Runtime/Shader` tree is load-bearing for the landmark material.

### v3.1.1

Patch release. Reclassifies `com.unity.inputsystem` from a soft to a **hard** package dependency and switches the example-assembly `#if` gate from package presence to Player Settings, restoring compilation on Unity 6+ projects where the input system package ships preinstalled. Existing legacy-input projects updating from 3.1.0 see no runtime behavior change.

#### Why this exists

In 3.1.0 the SDK declared `com.unity.inputsystem` as a soft dependency: source code branched on a `MAPBOX_NEW_INPUT_SYSTEM` define wired via `versionDefines` (set when the package is installed), but `MapboxExamples.asmdef` did not actually reference `Unity.InputSystem`. On Unity 6+ — where the input system package is installed by default — the `#if MAPBOX_NEW_INPUT_SYSTEM` branch activated, the `using UnityEngine.InputSystem` failed to resolve at the asmdef level, and the entire Examples assembly failed to compile.

The mirror failure mode (asmdef referencing `Unity.InputSystem` without the package installed → unresolved-reference compile error in 2022.3 legacy projects) ruled out simply restoring the reference. Source-side `#if` cannot bridge this because asmdef reference resolution happens before C# preprocessing.

This release resolves the trade-off by treating the package as a hard dependency that's always installed, and gating the source-side branch on a Unity built-in define that's only set when the user explicitly opts in via Player Settings.

#### Changed

- `com.unity.inputsystem` (>= 1.7.0) is declared in `package.json` `dependencies`. UPM auto-installs it on next package refresh. The package coexists with legacy input; installation alone does not change runtime behavior.
- `PointerInput.cs` now gates on Unity's built-in `ENABLE_INPUT_SYSTEM` define (set by Player → Active Input Handling = "New" or "Both") instead of `MAPBOX_NEW_INPUT_SYSTEM`. Existing legacy-input projects keep running the `#else` branch until the user explicitly switches Active Input Handling.
- `MapboxExamples.asmdef` references `Unity.InputSystem` directly and drops the `MAPBOX_NEW_INPUT_SYSTEM` `versionDefines` block (no longer used).

#### Upgrading an existing project

For the common case — Unity 2022.3 project, Active Input Handling = "Old", no prior `com.unity.inputsystem` install — no action is required. After the SDK update, UPM installs the input system package (~2 MB), Active Input Handling stays "Old", and every code path runs the same legacy implementation it did before.

If the upgrade goes wrong, in roughly decreasing order of likelihood:

- **Stale compile errors that mention `UnityEngine.InputSystem` or `MAPBOX_NEW_INPUT_SYSTEM`** — Unity's `Library/` cache hasn't picked up the new package. Close Unity, delete `Library/`, reopen. Unity re-resolves packages and reimports.
- **Package Manager reports "cannot resolve com.unity.inputsystem"** — the project is on Unity < 2022.3, which the SDK does not support, or a project-side `Packages/manifest.json` override pins an incompatible version. Bump the project's Unity version or relax the version constraint.
- **Map cameras stop responding to mouse / touch after the update** — should not happen if Active Input Handling matches what it was before; if it does, verify Project Settings → Player → Active Input Handling is still set to your prior choice. Unity does not change this setting when a package is auto-installed, but a manual setting flip during the update would explain the symptom.
- **Yellow Inspector warning on `StandaloneInputModule` asking to "Replace with InputSystemUIInputModule"** — cosmetic, appears only under Active Input Handling = "New" / "Both". One-click swap on the EventSystem inspector if you want to clear it; runtime is unaffected.
- **UGUI throws `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class…` under Active Input Handling = "New" only** — Unity's default `StandaloneInputModule` reads through `UnityEngine.Input.*`, which throws under New-only. Either set Active Input Handling to "Both" (legacy stays available for UGUI), or click "Replace with InputSystemUIInputModule" on the EventSystem inspector.
- **You want to remove `com.unity.inputsystem` entirely from your project** — UPM blocks removal because the SDK declares it as a dependency. The de facto opt-out is to leave it installed and set Active Input Handling = "Old"; the package is then dead weight on disk but runtime is identical to legacy-only. For a true physical removal, edit the SDK's `package.json` to drop the `com.unity.inputsystem` line and remove the matching GUID reference from `MapboxExamples.asmdef`. Requires the SDK to be in an editable location (e.g. embedded under `Packages/`, not in `Library/PackageCache/`).

### v3.1.0

Minor-version bump per semver: this release contains source-breaking API changes (listed below) alongside substantial feature work in camera/input, tile LOD, and terrain rendering. The 3.0.7 designation was previously used in development; published 3.0.7 builds, if any, are superseded by 3.1.0.

#### New dependencies
- Added `com.unity.burst@1.8.12`. The terrain-RGB elevation decoder and the collider vertex-fill jobs are now Burst-compiled. First-time domain reload pays a one-shot AOT compile cost; runtime decoding is significantly faster.
- `com.unity.inputsystem` is recognized as a **soft dependency**. Projects without it continue using `UnityEngine.Input`; projects with it automatically use the new Input System via a `MAPBOX_NEW_INPUT_SYSTEM` define wired through `versionDefines`. No project-side action required either way. See `Documentation~/CameraSystem.md` for details.

#### Breaking changes

**Source-level API surface:**

- `IMapInformation` gained a new member: `TerrainInfo Terrain { get; }`. Any project that ships its own `IMapInformation` implementation must add this property — return an instance of `TerrainInfo` (defaults are fine for a non-terrain map). The built-in `MapInformation` already implements it.
- `TerrainData.ElevationValuesUpdated` is now declared `public event Action` (was a plain `public Action` field). External code can subscribe with `+=` / unsubscribe with `-=` as before, but **direct assignment (`= myCallback`) and direct invocation (`ElevationValuesUpdated()`) no longer compile**. Migrate to `+=` for subscription. The previous single-setter `SetElevationChangedCallback` (which silently wiped other subscribers) has been removed.
- `MapboxTileData.SetDisposeCallback` was replaced with `public AddDisposeCallback(Action)` / `RemoveDisposeCallback(Action)` (multicast). External code that previously assigned through reflection or an internal-visible-to dependency should switch to the add/remove pair; subscriptions are now safely shared across the up-to-16 render tiles that consume the same `TerrainData` instance.
- `TerrainData.IsDisposed` is a new `public bool` (read-only). Async readback paths check this before assigning into the data; external consumers may also want to consult it.
- `MapboxMapVisualizer` now implements two new interfaces:
  - `IMapVisualizer` (existing, no change).
  - `ITileLifecycleSource` (new, in `Mapbox.BaseModule.Map`): exposes `TileLoaded` / `TileUnloading` events, `MapInformation` property, and `ActiveTiles` as `IReadOnlyDictionary<UnwrappedTileId, UnityMapTile>` via explicit interface implementation. The public concrete `ActiveTiles` property still returns `Dictionary<,>` — no break for existing consumers.
- New opt-in marker interface `ITileLifecycleObserver` (in `Mapbox.BaseModule.Map`). Layer modules that need a visualizer reference for bookkeeping can implement it and the visualizer calls `AttachToMapVisualizer(ITileLifecycleSource source)` on them during `Initialize`. `TerrainLayerModule` implements it for its terrain-bounds tracker; other modules are unaffected.

**Custom imagery module:**

- `CustomTMSTile` constructor extended from 4 args → 6 args: added `bool invertY` and `bool isMapboxService`. The legacy 4-arg constructor (`urlFormat`, `tileId`, `tilesetId`, `useNonReadableTexture`) is preserved as an overload defaulting to `invertY: true, isMapboxService: false` — external subclasses keep compiling. New code should use the 6-arg form to control invert-Y behaviour and Mapbox-signed request routing explicitly.
- `CustomSource.CreateTile` now always returns a `CustomTMSTile` when `UrlFormat` is set, regardless of `InvertY`. Previously `InvertY=false` returned a plain `RasterTile`. If you relied on the plain-raster path with a non-empty `UrlFormat`, set `UrlFormat = ""` to keep that behavior — empty `UrlFormat` falls back to plain `RasterTile`.
- `CustomTerrainSource.CreateTile` got the same empty-`UrlFormat` fallback to plain `RasterTile` to match `CustomSource`.
- `CustomTerrainLayerModuleScript` now passes the user-configured `Settings.DataSettings` (cache size, retina flag, non-readable textures, data-zoom clamp) through to `CustomTerrainSource`. Previously a throwaway `ImageSourceSettings` was used and the Inspector values had no runtime effect. **This is a behavioral break for users who had `DataSettings` set to non-default values** — the values now actually apply.

**Terrain settings:**

- `TerrainInfo` removed two fields that were written-but-never-read: `IsEnabled` (was unused) and `Exaggeration` (no consumer). Remaining fields: `MinElevation`, `MaxElevation`. Plus new constants `DefaultMinElevation = 0f` and `DefaultMaxElevation = 5000f`.
- The previous `SetElevationChangedCallback` method on `TerrainData` was removed (replaced by the multicast event — see above).

**Camera API:**

- New `MapCameraBehaviour<T>` open generic abstract base class. Concrete subclasses (`SlippyMapCameraBehaviour`, `Moving3dCameraBehaviour`) ship with the SDK. Custom cameras must subclass with a closed-type generic parameter.
- New `MapInput.Teardown(IMapInformation)` virtual hook. Existing subclasses of `MapInput` must override and unsubscribe any events they registered in `Initialize` — without the override the default is a no-op (no break, but recommended).
- New `MapInput.TouchCountDecreasedThisFrame` protected property (read inside `UpdateCamera` to detect 2→1 finger pinch-end and re-seed drag origin).
- New camera state mutator: `MapInput.CurrentTwoFingerGesture` removed from earlier review iterations — moved entirely inside `TouchInputHandler` as a private enum. External code can't depend on it (was never publicly exposed in v3.0.x).

**Tile provider:**

- `UnityTileProvider.MaximumZoomLevel` is now silently clamped to **30** internally (was unbounded). Higher values would have overflowed `1 << (_maxZoom - zoom)` past 30. Practically unreachable in Mercator (pyramid maxes at 22-24), but any Inspector misconfig now produces a log warning instead of an overflow.
- `UnityTileProvider` now warns at construction if `MinimumZoomLevel > MaximumZoomLevel`.
- `TileNode.Set` internal-shape signature changed from `(id, worldCenter, scale, boundsHeight)` to `(id, worldCenter, scale, boundsBottom, boundsHeight)`. Internal API — only relevant if you subclassed `UnityTileProvider` and touched `TileNode` directly.

**Map visualizer:**

- `MapboxMapVisualizer.MaxMercatorZoom` is a new `public const int` (`= 22`). Replaces the magic literal in `DelveInto`. Available for external consumers that need to align with the SDK's pyramid cap.
- `MapboxMapVisualizer.MapInformation` is a new public property exposing the map info. Was previously a protected field only.

**Removed from package:**

- `TileProviderBenchmark.cs` was removed from the SDK package. It was an internal tool that shouldn't have been included. The file is `.gitignore`d so a local copy can be kept by developers who need it; users will not see it in the SDK.

#### New features

**Camera & input:**

- New camera system with two concrete behaviours:
  - `SlippyMapCameraBehaviour` — fixed camera, map moves underneath. Best for 2D-style maps, navigation overlays, AR/VR.
  - `Moving3dCameraBehaviour` — camera orbits a target point. Best for 3D city exploration, games, simulations.
  Both wrap a `MapCameraBehaviour<T>` generic base that handles the `IMapInformation` event subscription lifecycle.
- Touch input support: single-finger pan, two-finger pinch zoom, two-finger same-direction vertical tilt. Pinch/tilt are mutually exclusive via a per-frame gesture decision in `UpdateInputState`. Touch rotate is not implemented (mouse-only via right-click drag).
- Input System soft-dependency support. The SDK reads through an `IPointerInput` abstraction (legacy vs new system) selected at compile time by `MAPBOX_NEW_INPUT_SYSTEM`, with two `IInputHandler` implementations (mouse vs touch) selected by build platform — Android/iOS device builds use the touch handler; Editor and Standalone builds use the mouse handler. There is no runtime fallback.
- `Documentation~/CameraSystem.md` walks through the camera setup, gesture mapping, and the Input System opt-in.

**Tile provider / LOD:**

- LOD overhaul:
  - Forward-projected distance replaces euclidean distance.
  - Exponential split threshold replaces screen-fraction.
  - Acute-angle compensation (`DistToSplitScale`) reduces detail for tiles viewed at grazing angles, ported from the native SDK's `tile_cover.cpp`.
  - Result: tile counts at high pitch are 40–60% lower vs the prior algorithm with comparable visual fidelity near the camera.
- `FrustumBuffer` setting (world-space units; scales with `mapInformation.Scale`). Pre-loads tiles just outside the screen edges so panning doesn't pop in.
- Reusable scratch arrays (`_corners`, `_planes`, `_quadrants`, `_pool`) eliminate per-frame allocations in `GetTileCover` and `InternalUpdateCoroutine`.
- Sub-sea-level terrain handled correctly: `boundsBottom` derived from `MinElevation/scale`, so tiles below Y=0 (Death Valley, Dead Sea) no longer frustum-cull.

**Terrain rendering:**

- Burst-compiled decode of terrain-RGB into elevation float[] via `Sync/AsyncExtractElevationArray` Burst `IJob`s. Min/Max computed inline during decode (saves a second C# pass).
- Burst-compiled collider vertex-fill (`BuildColliderVerticesJob`) writing into a reusable `NativeArray<Vector3>`. Triangles regenerated only when `SampleCount` changes.
- Async PhysX collider bake (when `asyncBakeCollider = true`): `Physics.BakeMesh` runs on a worker thread; `Mesh.sharedMesh` assignment happens one frame later when the cooked-data cache is ready. Main-thread cost is near-zero.
- Shader-mode tiles share a single flat render mesh (`TerrainSharedFlat`) across the entire tile pool. Per-tile state (`_HeightTexture_ST`, `_TileScale`, etc.) goes via `MaterialPropertyBlock` instead of per-tile `Material` instances — saves `Material` clone allocations across pool churn.
- Bilinear elevation sampling in `TerrainData.QueryHeightData`, the collider vertex job, and the shader bilinear subgraph. Replaces the prior nearest-neighbor `(int)xx` truncation that produced visible stepping when render-tile mesh vertices sampled a denser sub-region than the data tile's effective resolution.
- `ElevatedTerrainShader.shadergraph` and `MapboxTerrainBilinearSampling.shadersubgraph` were restructured during the bilinear-sampling and normal-banding work (ridge-band fix uses a 2-texel offset, per Option A in the prior investigation). Lighting and displacement look correct on eye-test against the example scenes. If a project ships custom shader variants derived from these graphs, re-verify after upgrading.
- `TerrainInfo` exposes observed `MinElevation` / `MaxElevation` (in meters) on `IMapInformation.Terrain`. Updated automatically as terrain tiles load/unload. The tile-provider's AABB and `UnityMapTile.SetFallbackMeshBounds` consume this to size the frustum-cull volume.
- Conservative bounds fallback (`UnityMapTile.SetFallbackMeshBounds`, 10000m) applied immediately on terrain-data attach so shader-displaced geometry doesn't get culled before real Min/Max arrive — or permanently, when CPU extraction is disabled.
- Per-tile `MeshCollider` configurable via `TerrainColliderOptions` (in `ElevationLayerProperties`). Supports placing the collider on a dedicated child GameObject + Unity Layer.
- Bounds tracking is now an opt-in concern of `TerrainLayerModule` (via `ITileLifecycleObserver` → `TerrainBoundsTracker`). `MapboxMapVisualizer` itself is terrain-agnostic.

**Custom imagery:**

- New `CustomSourceSettings.InvertY` toggle: when `true`, Y is `(2^z - Y - 1)` instead of `Y` in URL substitution (TMS-style).
- New `CustomSourceSettings.IsMapboxService` toggle: when `true`, the tile request is signed with the Mapbox access token; when `false`, it's a plain HTTP request.

**Editor / Inspector:**

- New custom drawer for `[SimplificationFactor]` attribute: dropdown of distinct-grid presets with a live HelpBox describing the resulting vertex grid + per-tile quad count. Legacy non-preset values now show a "migrate to a preset" warning instead of being silently snapped. Multi-object editing and prefab-override-safe via `BeginProperty`/`EndProperty`.
- New custom drawer for `TerrainColliderOptions`: foldout with grayed-out sub-options when `addCollider` is disabled, further gated when `useDedicatedColliderLayer` is disabled.
- New custom drawer for `TerrainSideWallOptions`: same reveal pattern for skirt-wall settings.
- New custom drawer for `[GameObjectLayer]` attribute: Unity Layer dropdown for `layerId` fields.
- `TerrainSettingsInspectorHelper` warns when extraction is force-enabled by other settings (UseShaderTerrain off, or collider on) and renders the `ExtractCpuElevationData` checkbox as disabled+ticked. The previous OnGUI silent write to `boolValue` was removed — no more silently dirtying inspected assets. Scene-scan for vector / components modules is cached and invalidated on `EditorApplication.hierarchyChanged`.

**Other:**

- New demo scene demonstrating how to use POI information.
- New documentation files added.
- Reorganized Mapbox context menu for creating Mapbox ScriptableObjects.

#### Performance

- Burst-compiled terrain-RGB decode: significantly faster than the prior C# loop.
- Burst-compiled collider vertex fill with persistent `NativeArray` buffers.
- `ElevationArrayPool`: pools `float[]` buffers across cache evictions, capped at 32 per-size (`MaxPerSizeDepth`). Avoids ~256 KB / ~1 MB allocations per tile decode for 256² / 512² heightmaps. Returns past the cap drop to GC. In Editor, the pool clears on `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` so stale buffers don't survive play-mode stop.
- `DelveInto` rewritten as iterative explicit-stack: eliminates `new bool[4]` + `new UnwrappedTileId[4]` allocations per cache miss (up to ~85 recursive calls × ~32 bytes before).
- `MapboxMapVisualizer.Load` reuses `unityMapTile.Children` lists across pool cycles instead of allocating a fresh list per cache miss.
- `TerrainBoundsTracker` recomputes Min/Max from `ActiveTiles` once per coroutine tick (deferred via a dirty flag) instead of on every tile event.
- `TerrainData._cachedWidth` caches `sqrt(ElevationValues.Length)` once on `SetElevationValues` — `QueryHeightData` no longer recomputes per query.
- `SimplificationFactorDrawer` caches preset+label arrays per `(Min, Max, VertexBase)` tuple and reuses one `GUIContent` for HelpBox height calculation (was allocating per repaint).
- `TerrainSettingsInspectorHelper` scene scan no longer runs `FindObjectsByType<MonoBehaviour>` on every OnGUI repaint.

#### Fixes

**Tile pipeline:**

- Fixed multicast dispose notification — shared `TerrainData` now notifies all consumers on eviction (previously the single-setter callback silently dropped 15 of 16 sharing tiles).
- Fixed `TerrainData.ElevationValuesUpdated` previously being a plain `public Action`, which let `SetElevationChangedCallback` silently wipe other subscribers. Now a proper multicast event.
- Fixed `TerrainSource.ExtractElevationValues` using the wrong overload of `ExtractHeightData` — the `Action<float[]>` overload + 1-arg `SetElevationValues` left `Min/MaxElevation` at 0 on every tile loaded through the initial async path. Now uses the `TerrainData` overload (which sets Min/Max inline).
- Fixed `TerrainSource.ExtractElevationValues` hanging forever when async readback completes after `TerrainData.Dispose` — the coroutine now subscribes to both `ElevationValuesUpdated` and the dispose multicast and completes on either signal.
- Fixed `Async/SyncExtractElevationArray` not guarding against `TerrainData.IsDisposed` — late-arriving GPU readback callbacks would assign into evicted data and leak the rented buffer. Now both check before renting and after the decode; the rented buffer is returned to the pool when dispose arrives in the window.
- Fixed `MapboxMapVisualizer.RecomputeTerrainBounds` (now in `TerrainBoundsTracker`) not being wired to `ElevationValuesUpdated` for shader-mode tiles — those tiles finish on texture-ready before CPU decode arrives, so the bounds never widened until extraction landed. Now a one-shot watcher per shared `TerrainData` triggers the recompute.
- Fixed terrain bounds remaining pinned at 0 when all loaded tiles were at/above sea level (previous accumulate-only `< terrain.MinElevation` never fired).
- Fixed terrain bounds monotonically inflating over a long session — `TerrainBoundsTracker` now recomputes from `ActiveTiles` on tile pool, so bounds tighten as the camera moves to lower-elevation regions.
- Fixed `UnityTileTerrainContainer.SetTerrainData(null)` silently leaking the prior `TerrainData`'s `ElevationValuesUpdated` and dispose-callback subscriptions. Detach now runs unconditionally before the null-return.
- Fixed `UnityTileTerrainContainer.OnDestroy` not removing the dispose callback (only the `ElevationValuesUpdated` handler) — a later eviction would fire the multicast into a destroyed tile.
- Fixed `UnityTileImageContainer.OnDestroy` being empty — same multicast-leak pattern as the terrain container.

**Terrain strategy / collider:**

- Fixed CPU-elevation render mesh leak via `MeshFilter.mesh` clone path. `CreateElevatedMesh` now reads `.sharedMesh` (the `.mesh` getter implicitly cloned the assigned shared mesh and orphaned the original). Per-tile mesh is now explicitly named `TerrainCpuMesh` so `UnityMapTile.OnDestroy` correctly disposes it.
- Fixed `_sharedFlatMesh` being `Clear()`'d on a shader→CPU transition when `sampleCount` or `_useTileSkirts` changed at runtime — would wipe geometry for every other shader-mode tile pointing at the shared mesh. Now allocates a fresh per-tile mesh first.
- Fixed `_sharedFlatMesh` leaking on a second `Initialize` call. `Initialize` now destroys the previous one before allocating.
- Fixed `MeshCollider.cookingOptions` only being set at component creation — when an existing collider's cooking options diverged from the bake's, PhysX silently re-cooked synchronously on assignment, defeating the async-bake path. Now assigned every call.
- Fixed `CompleteBakeAndAssign` not null-guarding the new mesh after `JobHandle.Complete()` — a parallel bake completion could `Destroy()` our mesh as its "previous" before we got to assign it.
- Fixed `_elevationNativeMirror` cache key being the `float[]` reference. `ElevationArrayPool` recycles buffers, so the same `float[]` could be re-rented for different data and produce a false cache hit. Now keyed by `TerrainData` reference.
- Fixed `RegisterCollider` deferred path adding a closure for every temp→final re-invocation, doubling async-bake work. Now de-dups by `(tile, data)`.
- Fixed deferred-rebuild closures rooting `tile` and `data` when `TerrainData.Dispose` arrived before `ElevationValuesUpdated` fired. Closures now self-detach on both signals.
- Fixed `_lastColliderBuild` (collider build cache) growing with dead `MeshCollider` keys — Unity's overloaded `==` doesn't reach `Dictionary<K,V>`'s reference equality. Opportunistic sweep + `Clear()` on `OnDestroy`.
- Fixed mesh-name string duplication: `TerrainColliderMeshName`, `SharedFlatMeshName`, `TerrainCpuMeshName` are now `public const` on `ElevatedTerrainStrategy`. The dedicated-collider-layer GameObject name renamed from `"TerrainCollider"` to `"TerrainColliderChild"` to disambiguate from the mesh name.

**Tile provider:**

- Fixed `UnityTileProvider.ShouldSplit` overriding the per-corner `offset.y` to `+cameraHeight` — correct in the native SDK's tile-coordinate system but flipped the sign of the vertical dot-product contribution in Unity's world units, triggering "always split" for downward-pitched cameras. Now uses ground-plane projection (`offset.y = 0f`).
- Fixed `UnityTileProvider.DistToSplitScale` dividing by `dz` with no zero guard — camera-at-ground (`dz=0`) produced NaN/Inf and pinned the tile to `MinimumZoomLevel`. Now short-circuits with `return 1f` when `dz < 0.0001f`.
- Fixed `UnityTileProvider.ShouldSplit` always-split for behind-camera corners recursing all the way to `_maxZoom` on tilted views with `FrustumBuffer` expansion. Now capped at `zoom < 18` (`AlwaysSplitSafeCap`).
- Fixed `UnityTileProvider.FrustumBuffer` tooltip not mentioning that the value is in world-space units that depend on `mapInformation.Scale`.

**Camera & input:**

- Fixed `MAPBOX_NEW_INPUT_SYSTEM` define gating being broken — the `versionDefines` entry that defined the symbol was uncommitted. Projects with "Active Input Handling = New" only would crash on every legacy `UnityEngine.Input.*` call.
- Fixed `MapboxExamples.asmdef` requiring `Unity.InputSystem` as a hard reference, breaking projects without the package. Now relies on the InputSystem asmdef's `autoReferenced: true` resolution; absent → no reference, no compile error.
- Fixed scroll-zoom delta magnitude differing ~10× between legacy and new input paths on Windows (legacy `Input.GetAxis` ≈ 0.1/notch, new `scroll.y` ≈ 120/notch). Normalized to ~0.1/notch on both.
- Fixed pan jump after a 2-finger pinch drops to 1 finger — the new "primary" touch could be at a different screen position than the lifted one. Now re-seeds drag origin on `TouchCountDecreasedThisFrame`.
- Fixed pinch and tilt detectors both firing on the same frame (each computed dominance independently and `GetPinchZoomDelta` updated `_previousPinchDistance` regardless of which won). Decision now made once per frame in `UpdateInputState`.
- Fixed pinch/tilt thresholds being in pixel units (DPI-dependent). Both magnitudes now normalized to `Screen.height`; pinch wins ties (no dropped frames on near-vertical pure-tilt drags).
- Fixed pinch state corruption across 2→3→2 finger transitions. EnhancedTouch reorders `activeTouches` on add/remove, so the next frame would compare distance against a different physical pair. Now tracks the touch ID pair and reseeds on change.
- Fixed `Moving3dCamera.Zoom()` NaN propagation through `preDistance`, `camDistanceToMouse`, and the lerp output. Top-down ortho / first-frame / `mapInformation.Scale=0` / camera curve evaluating to 0 all hit the guards now.
- Fixed `MapCameraBehaviour.Awake` NRE when no `MapBehaviourCore` exists in the scene. Logs an error and disables the component instead.
- Fixed `MapCameraBehaviour` never unsubscribing from `MapBehaviour.Initialized` — leak across scene reload.
- Fixed `MapCameraBehaviour` not tearing down `IMapInformation` event subscriptions registered by the camera core (`LatitudeLongitudeChanged`, `ViewChanged`, `SetView`) — the closures rooted the destroyed camera and its `Camera` Transform. New `MapInput.Teardown` virtual hook + `OnDestroy` wiring.
- Fixed `MapCameraBehaviour.OnMapInitialized` not tearing down a previous `MapInformation` if the event fires twice (defensive — not currently exercised by example scenes).
- Fixed `SlippyMapCamera.Initialize` dereferencing `FindObjectOfType<MapBehaviourCore>().transform` without a null check.
- Fixed `SlippyMapCamera.CenterLatitudeLongitude` being overwritten every frame even when nothing moved, with a `Mathf.Clamp((float)Latitude, …)` downcast losing precision at z18+. Now gated on `_output.HasChanged` and uses `System.Math.Clamp` on doubles.

**Map visualizer:**

- Fixed `MapboxMapVisualizer.RemoveUnnecessaryTiles` previously exempting `Filler` tiles from removal — caused orphaned-filler z-fighting. Now eligible for pooling; visual continuity preserved by the temp-tile filler protection pass in `Load()`.
- Fixed `MapboxMapVisualizer.OnDestroy` not detaching pending elevation-decode watchers — closures kept shared `TerrainData` rooted past the visualizer's lifetime. Tracker (now in `TerrainBoundsTracker`) handles this in its `Dispose`.
- Fixed `LoadingState.Filler` children of currently-loading temp tiles being pooled when they're still needed for visual continuity. Filler protection pass in `Load()` removes their ids from `_toRemove`.

**Custom imagery:**

- Fixed `CustomSource.CreateTile` throwing `ArgumentNullException` from `string.Format(null, …)` when `UrlFormat` was empty. Empty `UrlFormat` now falls back to plain `RasterTile`.
- Fixed `CustomTerrainSource.CreateTile` missing the same empty-`UrlFormat` fallback.
- Fixed `CustomTerrainLayerModuleScript` not passing the user's `DataSettings` through to the source — Inspector values had no runtime effect.
- Restored 4-arg `CustomTMSTile` constructor as a backward-compat overload (binary break for external subclasses otherwise).

**Editor / Inspector:**

- Fixed `SimplificationFactorDrawer` silently snapping legacy non-preset values to the nearest preset during `OnGUI` — dirtied every inspected asset on first paint.
- Fixed `SimplificationFactorDrawer` missing `BeginProperty`/`EndProperty` — broke prefab-override and multi-object editing.
- Fixed `SimplificationFactorDrawer.GetPropertyHeight` returning a different height than the rendered HelpBox content for legacy values — caused HelpBox to clip into the next Inspector row.
- Fixed `TerrainColliderOptionsDrawer.GetPropertyHeight` returning one extra `line + spacing` (cosmetic gap at the bottom of the foldout).
- Fixed `TerrainSettingsInspectorHelper` writing `child.boolValue = true` during `OnGUI` — silently dirtied every inspected asset. Now display-only (runtime evaluates `NeedsCpuElevation` directly).
- Fixed `TerrainSettingsInspectorHelper.SceneHasFeaturesNeedingCpuElevation` running `FindObjectsByType<MonoBehaviour>` on every Inspector repaint. Cached and invalidated on `EditorApplication.hierarchyChanged`.
- Fixed `Runtime/Mapbox/VectorModule/ComponentSystem/Editor/` missing an asmdef — editor scripts (`BuildingLayerVisualizerObjectEditor`, `ExtrusionOptionsDrawer`, `MapboxComponentsModuleScriptEditor`) were being folded into the runtime `MapboxComponentSystem` assembly, pulling `UnityEditor` references into player builds. New `MapboxComponentSystem.Editor.asmdef` (Editor-only, references the runtime asm + `MapboxBaseModule`) keeps them out.
- Fixed `MapboxMapBehaviour.CreateMapVisualizer` silently producing a magenta tile material in player builds when no `TileCreatorBehaviour` was assigned. The previous fallback used `Shader.Find` to construct a Material at runtime, but `Shader.Find` only resolves shaders referenced from a shipped Material or listed in *Project Settings → Graphics → Always Included Shaders* — so the shader was stripped from builds and `new Material(null)` produced magenta tiles. Replaced with a new `[SerializeField] protected Material _defaultTileMaterial` on `MapboxMapBehaviour`; the asset reference keeps the shader alive in builds. Demo scenes (`LocationExample.unity`, `ApiTest.unity`) ship with this field wired to `ElevatedTerrainMaterial.mat`. If both `_tileCreatorBehaviour` and `_defaultTileMaterial` are null, an `InvalidOperationException` is thrown with actionable instructions.
- Fixed `UnityTileProviderSettings.SubdivisionBias` defaulting to `0` in scenes saved before the field existed — Unity assigns `default(float)` for missing serialized fields and does NOT run the C# field initializer in that case, which collapsed `distToSplit` and forced the tile provider to render only the `MinimumZoomLevel` tile. New C# default is `0.6f`; `ISerializationCallbackReceiver.OnAfterDeserialize` upgrades legacy serialized `<= 0` values to `0.6f`.

**Other:**

- Fixed `TerrainData.QueryHeightData(Vector2)` and `(float, float)` overloads not guarding against null `ElevationValues` — shader-only mode (`ExtractCpuElevationData=false`) hit the NRE.
- Fixed `TerrainData.Dispose` not being idempotent — explicit `IsDisposed` bail at top.
- Fixed `MapboxTileData.SetDisposeCallback` previously silently overwriting other subscribers when one `TerrainData` was shared across 16 render tiles (the single-callback API). Replaced with multicast `AddDisposeCallback` / `RemoveDisposeCallback`.
- Fixed `1 << (_maxZoom - zoom)` shift overflow risk in `UnityTileProvider.ShouldSplit` when `_maxZoom` exceeded 30. Now clamped internally.
- Fixed vector layer range limits not being applied correctly during data processing.
- Fixed incorrect limit validation and bounds checking in the `LatitudeLongitude` struct.
- Fixed `MapInformation.Initialize` not resetting `Terrain.Min/MaxElevation` to defaults — previous run's values could bleed into a fresh session.
- Fixed Android IL2CPP player builds with **Medium** Managed Stripping Level silently dropping every SQLite tile cache insert with a `ConstraintForeignKey` error. Sqlite-net populates row values via reflection (`PropertyInfo.SetValue`), but IL2CPP stripped the auto-property accessor methods (`set_id`, `set_name`, etc.) on the cache's model classes — `SetValue` then became a silent no-op, every read returned `id = 0`, and downstream `tiles` inserts failed the foreign key on `tile_set`. The four SQLite model classes (`tiles`, `tilesets`, `offlineMaps`, `tile2offline`) and every one of their persisted properties now carry `[UnityEngine.Scripting.Preserve]`, and the same types are declared in `Plugins/link.xml` as a secondary safety net. Map rendering was unaffected (memory cache covered it) but the SQLite disk cache wasn't actually persisting under Medium. **Stripping support after the fix:** Low (and below) is the tested target; Medium has been briefly verified and seems to work; High is untested but expected to work given the same preservation. Additionally, the `Error inserting …` log line now includes SQLite's extended error code (`ConstraintUnique` / `ConstraintForeignKey` / `ConstraintNotNull` / etc.) so future cache-side failures are diagnosable from the first line.
- Fixed `MapboxMapVisualizer` writing `tile.transform.position` (world space) on every tile cover update, which silently reset any parent transformation on the `MapRoot` / `BaseTileRoot` hierarchy. Tiles are now positioned via `transform.localPosition`, so translating or rotating the map's root (e.g. anchoring it to an `ARAnchor`) is honored. For unchanged scenes — `MapRoot` at world origin with identity rotation — behavior is identical. Non-unit `MapRoot.localScale` is still unsupported; drive map scale through `MapInformation.Scale` instead.
- Fixed vector content (buildings, areas, roads from `VectorLayerVisualizer` and the ComponentSystem `AreaComponentVisualizer` / `RoadComponentVisualizer`) failing to follow the same MapRoot parent transform as the terrain tiles. Two distinct issues compounded each other. (1) Four call sites were writing world-space positions on the vector hierarchy: the per-layer `GameObjectOffset` y-offset in both component visualizers, the `Settings.Offset` application + `_layerRootObject.transform.position.x/z` reads inside `VectorLayerVisualizer.UpdateForView`, and `MapShifterCore.Update` writing `RuntimeGenerationRoot.position -= viewCenterPosition`. All converted to `localPosition`. (2) `Transform.SetParent(parent)` defaults to `worldPositionStays: true`, which makes Unity preserve world transform by writing inverse-parent-rotation into the child's `localRotation`. Auto-created roots (`BaseTileRoot`, `RuntimeGenerationRoot` in `UnityContext`, and every per-layer `_layerRootObject` + pooled entity GameObject in the vector visualizers) hit this path on initial parenting, ending up with a `localRotation` that counter-rotated the entire vector subtree against MapRoot. All five `SetParent` call sites now pass `worldPositionStays: false`, with explicit `localRotation = Quaternion.identity` on the visualizer roots as defensive belt-and-suspenders. Buildings, areas, and roads now rotate / translate with the rest of the map under AR/MR-style parent transforms.

#### Documentation
- New [`Documentation~/Migration-3.0-to-3.1.md`](Documentation~/Migration-3.0-to-3.1.md) walks through every source-level break, every behavioral change, and step-by-step verification when upgrading from v3.0.
- `README.md` rewritten and now serves as the single landing page (Overview.md merged in and removed). Adds a quick-facts header table, dependency listing, demo-scenes table, iOS / Android build sections with the v3.1.0 stripping notes, modules table, and a complete documentation index.
- `Documentation~/GettingStartedWithMapboxMapObject.md` gained a "Place a GameObject at a latitude / longitude" recipe — the most commonly asked indie use case (drop a marker / character / POI at specific coordinates), with the correct `localPosition` + `MapRoot` parenting pattern and the optional `TryGetElevation` step.

#### Tests
- Added editor-mode tests for `ElevationArrayPool` (rent / return / pool cap / null safety), `TerrainInfo` (defaults), and `MapInformation` (Initialize resets terrain bounds, lat/lon, idempotency). First test coverage for v3.1.0 surface — more coming in v3.2.

### v3.0.6

#### Fixes
- Fixed the Prefab Modifier to correctly scale generated objects to fit world size, depending on a setting.
- Fixed Height and Chamfer Height modifiers to consider the current location’s latitude when calculating building height.
- Fixed Line Mesh Modifier to scale line width according to map scale and latitude.
- Fixed a bug in the Prefab Modifier that blocked mesh generation when no prefab was assigned.

#### Changes
- Moved the `Layer Type` setting from the Layer Visualizer to the Modifier Stack to better support multiple stacks per visualizer.
- Added `MapInformation.GetLatitudeCompensationForLocation`, a method for getting more realistic distance estimates from Mercator units.
- Added `Conversions.TileEdgeSizeInMercator`, a simpler helper method for scale-related calculations.
- Improved the inspector UI for the Height and Chamfer Height modifiers.
- Updated filters:
  - Removed the Feature Data filter.
  - Introduced a Geometry Type filter.
  - Refined String and Number Property filters for better usability.
- Removed the Point Layer Visualizer, which became unnecessary after the structural changes above.
- Removed the Terrain Type setting, as it now only had a single valid option after recent updates.

### v3.0.5
Bug fixes, performance improvements, vector module changes, and core library updates for Android 16KB page size compatibility.

#### Fixes
- Fixed a bug in Cache Manager where it didn't fail safely when no file cache was available.
- Fixed low-performing events and callbacks in UnityMapTile class and its data container objects.
- Fixed an issue where the memory cache failed when runtime mesh generation on worker threads modified non-thread-safe lists.
- Fixed a bug where Source classes did not fully verify if requested data was already being processed.
- Fixed a bug where the Vector Module did not handle all mesh generation task results properly.

#### Improvements
- Updated MapboxCommon library to ndk27-24.10.0 for 16KB page size support.
- Updated SDK license to Mapbox License.
- Improved performance in the CanonicalTileId struct.
- Refined the Modifier Stack inspector UI.
- Changed the default map script to start with sample location and view parameters for an easier setup.
- Updated the default map script and tile creator to use the default Mapbox terrain shader without requiring manual assignment.
- Refined Vector Module’s tile retention and unloading logic.
- Added a new Layer Visualizer setting `Layer Type` to define positioning and scaling strategies for runtime-generated meshes.
- Added `PointLayerVisualizer` class for better handling of point-type data (such as POIs).
- Improved the UI and debugging tools of `LoggingDataFetchingManagerBehaviour` and `LoggingCacheManagerBehaviour` scripts.
- Enhanced the inspector UI for `LayerModifier` and introduced the new `TagModifier` GameObject modifier.
- Moved the LatitudeLongitude struct to its own file and added proper equality methods.
- Replace C# 9.0 syntax with C# 7.0 syntax for better backward compatibility.

#### Documentation
- Added new starter guides:
  - Getting Started with MapboxMap Object
  - Coordinate Conversions
  - Working with Modules
  - Working with POIs

#### Vector Module Changes
- Merged `MeshGenerationUnit` back into the Vector Module as it no longer provided value.
- Updated Vector Module behavior to avoid caching GameObjects outside of the current view.
- Vector Module now dynamically creates and destroys objects as they enter or leave the camera frustum for improved memory and cache management.
- Removed `OnVectorMeshTurnVisible` and `OnVectorMeshTurnInvisible` events, and fixed `OnVectorMeshCreated` and `OnVectorMeshDestroyed` events.

### v3.0.4
Performance (cpu and memory usage) improvements, bug fixes, library and Android/iOS compatibility updates, improved test coverage

Fixes
- Fix terrain shader (ElevatedTerrainShader) to create normals for terrain in runtime
- Fix a bug where VectorModule.GetTileCoverCoroutines only covered the data load but not the visual creation
- Fix file data fetcher to set data status properly
- Fix a bug where texture non-readable flag set wrong
- Fix a bug where settings memory cache size to zero didn't work
- Fix UnityContext to use BaseTileRoot and RuntimeGenerationRoot settings properly
- Fix an issue where static and terrain layer modules didn't do a complete tile rejection check like vector module does.
- Fix MapVisualizer to handle null coroutine pointers

Improvements
- Change Mapbox shaders to support built-in rendering pipeline and HDRP along with URP
- Remove type parameter from IMapVisualizer.TryGetLayerModule method
- Refine Temp/Final tile creation process and methods
- Refine cache manager methods both for clarity and support better testing
- Update Mapbox Attribution prefab for clarity
- Rework polygon collision filter and meshOnLine modifier
- Remove terrain strategy from tile creator script (it's back to terrain module)
- Change elevated terrain strategy script to update tile mesh bounds after elevation is applied
- Change elevated terrain strategy script and terrain data script to keep track of min and max elevation values in the tile
- Restructure elevation data extraction scripts for clarity and easier usage

Other Changes
- Update Mapbox Common Library to 24.10.2 to support latest xCode
- 
### v3.0.3
Fixes
- Resolved an issue where completed vector mesh generation tasks were not properly cleaned up.
- Fixed a problem with basic linear/trilinear texture sampling in terrain which caused spikes at certain elevation ranges.
- Fixed a bug where the initial map loading process fetched an incorrect set of tiles, requiring a second pass to load the correct data.
- Resolved an issue where tile mesh sidewalls/skirts were not generated correctly.
- Fixed a bug where the Data Fetching Manager and Cache Manager did not invoke callbacks in some scenarios.
- Corrected the Vector module’s use of the rejection limit setting for tile level calculations; it now correctly uses the data clamp limit.
- Addressed compatibility issues with certain core Android libraries that caused errors on older Android devices.
- Fixed CPU-based terrain generation (disabling the `Use Shader Terrain` setting). While slower than the GPU shader approach, this option remains useful for certain scenarios.
- 
Improvements
- Updated the terrain system: the elevated terrain and its settings have been moved back to the Terrain module. The flat terrain option has been removed—omitting the Terrain module now results in a flat map, while including it generates elevated terrain.
- Removed the `Load Background Data` setting from the Vector module. It is now always enabled.

Other Changes
- Removed several debug-related string fields that were no longer in use.
- Merged the previously separate `get` and `readExpiration` procedures in the SQLite implementation.
- Restructured and simplified data classes related to the task system.
- Removed the GPU-based Linear/Gamma colorspace check, which is no longer necessary following terrain shader fixes. 

### v3.0.2
Critical fixes and improvements for developers using Windows OS and developers using Unity 6.0 

Fixes
- Fix the android build problems with Unity 6.0. Settings files and android build procedure has changed, you can check the ReadMe file for the new instructions.
- Fix a bug where long-term cache wasn't working as intended during development time on Windows machines.
- Fix a bug where caching system caused app freezes on launch.
- Fix a bug where JsonUtility couldn't parse response JSON on IL2CPP builds.
- Fix LoadView procedure which allows developers to (controllably) jump to different locations on map.
- Fix the compilation issues in the test module.
- Fix and update the MapDebug module.
- Fix a bug where vector module didn't respect zoom level limits on first view load.
- Fix a bug where the root object for runtime generated vector entity visuals didn't position correctly.
- Fix a bug where vector layer module didn't update position of visuals from unloading tiles
- Fix a bug where TileJson was calling the callback method twice
- Fix a bug where file and sqlite caches didn't handle custom database and folder names correct 

New
- Add TileLoaded and TileUnloading events to MapboxMap class, along with LoadViewStarting and LoadViewCompleted events.
- Add MapboxAttribution prefab 
- Add ability to enable/disable telemetry collection through attribution prefab

Improvements
- Change the scheduling system in data fetching and task management systems to decrease the complexity, making it easier to control and improve performance on highly dynamic maps.
- Add new settings under image, terrain and vector modules to help limit and control their work.
- Change TileCreator class to work through an interface.

### v3.0.1
### v3.0.0

### v2.1.2
### Bug Fixes
- Fixes a bug where add collider feature didn't work for flat terrain mesh.
- Fix a bug where vector tile processing fired multiple times depending on Terrain/Image data states
### Improvements
- Improves Directions factory and prefabs to provide better UX and support for all types of maps

### v2.1.1
10/15/2019
### Bug Fixes
- Fixes a bug where users were being double counted on mobile platforms
- Fixes a bug which send incomplete telemetry events.

### v.2.1.0
09/20/2019
##### New Features
- *Editor Preview* - Adds ability to preview maps in editor at design time. AbstractMap now has an `Enable Preview` toggle which displays the map with all current settings, and provides ability to modify settings while viewing the map outside of Play mode.
- Pricing Update - Introduces new pricing framework.
##### Improvements
- Improves line mesh generation, adds options for join and cap types. Users have greater control over quality and styles of line meshes.
- Improves terrain tile loading times by optimizing terrain pipeline.
- Remove UV Modifier class and merge the functionality into polygon mesh modifier.
- Optimized Vector feature textures, resulting in smaller file sizes.
#### Bug Fixes
- Fixes a bug with UV calculation which caused textures to be stretched instead of tiled.
- Fixes a bug where Vector feature layers that used the same texture style would not preserve layer-specific style parameter settings.
- Fixes a bug where decoration spawning (SpawnInsideModifier) placed objects at wrong locations on recycled tiles.
- Fixes a bug where point placement didn't pate point location properly when zoom level changes.

### v.2.0.0
10/15/2018
##### New Features
- *Runtime editing of map features* - Map elements like Imagery, Terrain, Feature Properties can now be updated during runtime, using AbstractMap UI in the Editor , or by using the API methods.
- *Map API’s* - Maps SDK for Unity now has API methods to build and edit maps entirely using scripts.
##### Improvements
- Improves fallback for terrain tiles with missing or no data. Image will be now rendered on flat tiles instead of blank tiles.
- Improves terrain height query by using local position/rotation/scale of a tile. Use `QueryElevationInUnityUnitsAt` or `QueryElevationInMetersAt` API methods to query height.
- Terrain module “Sample Count” property changed to a slider.
- Improves tile loading and recycling performance.
##### Bug Fixes
- Fixes issue where Terrain was not turned off with `None` option.
- Fixes issue where Node Editor script was throwing a compilation error due to deprecated API in Unity 2018.2
- Fixes issue where SnapToZero moved the map for each new tile loaded instead of only using the first one.
- Fixes issue with the height modifier where floor height calculations were wrong causing overlapping floors.
- Fixes issue where dispose buffer setting was being ignored by `RangeAroundTransform` extent type.
- Fixes builds for UWP/ Hololens environments.
- Fixes issue where ARTableTop map would not snap to the detected plane on zooming in/out.
- Fixes issue where `BuildingsWithUniqueIds`  setting was getting applied incorrectly, resulting in missing buildings.

##### Breaking Changes
- Any `Feature` layers under `Map Layers` using a `Custom` style type will need to be re-assigned through the AbstractMap UI. This change was necessary to persist custom style options when switching between different styles.

### v.1.4.5
08/20/2018
##### New Features
- Added a **ReplaceFeatureCollectionModifier** class that allows multiple ReplaceFeatureModifiers to be run from one modifier.
- Added seven hero structures - Transamerica Pyramid, Coit Tower, Salesforce Tower, Empire State Building, Chrysler Building, One World Trade Center, Statue of Liberty.
##### Improvements
- Added color option to texturing style
- Added color palettes drop down to Simple texturing option. Allows users to choose between different color palettes.
- Improvements to Tile states and map states. Map extent finished state is robust and deterministic.
- Removed dependency of Tile Providers on Update methods.

### v.1.4.4
*07/10/2018*
##### New Features
- Selecting `Simple` Texturing Style in a `Vector Layer Visualizer` exposes a drop down menu which allows users to select a color palette for that layer.
##### Improvements
- Added 2 examples to the setup dialog
- AstronautGame - enhanced version of the Location Based game with custom styling and Astronaut asset
- TrafficAndDirections - example built around using Mapbox's traffic data layer and directions API
- Fix an issue with factories where a racing condition causing tiles without imagery
- Fixes the issue with Replacement Modifier to prevent duplicate prefab spawning and blocking wrong features.
- Fixes the issue with custom styles that go missing after building to a device.
- Fix an issue with factories where a racing condition causing tiles without imagery
- Added a separate Traffic and Directions demo scene
- Changed loft modifier to stretch texture horizontally
- Changed Directions Factory to check waypoints on timer and recalculate path if there's a chance

### v.1.4.3
*06/18/2018*
##### New Features
- Added a **Feature Replacement Modifer** that allows replacement of a vector feature at a given Latitude,Longitude with a supplied prefab. Enables replacing a procedurally generated building at a given Latitude,Longitude with a custom 3D model.
- Texturing Style dropdown now allows users to select from prepackaged texture styles or custom, which allows for user-defined texturing.
- Mesh and GameObject modifiers can now be created and added to AbstractMap via `Add New` buttons.
- Example scene `LocationProvider` can be used to log GPS traces. See documentation on how to copy logs from devices.
- GPS traces can be played back in the Editor with `EditorLocationProviderLocationLog`.
- `UserHeading` and `DeviceOrientation` values can be smoothed via different smoothing algorithms.
##### Improvements
- Improvements to UV mapping for side walls of extruded polygons when using atlas textures. Enables better handling of leftover areas when creating columns.
- DeviceLocationProvider should work on devices. Tested with iPhone 7/8/X, Google Pixel 2, Samsung S8, Samsung Galaxy Note 8, OnePlus 3
##### Bug Fixes
- Fix issue with UvModifier which caused wrong roof uv positions calculations.
- Fix issue with data fetcher to handle failed connections properly, which caused IndexOutOfRangeException during tile loading/unloading.
- Fix issue with ambient file caching on Windows with .NET4.6 runtime and long file names. https://github.com/mapbox/mapbox-unity-sdk/pull/825 https://github.com/mapbox/mapbox-unity-sdk/issues/815
##### Known Issues
- ARInterface related errors and crashes on Unity 2018.1.1f1 related to bug in Unity  https://issuetracker.unity3d.com/issues/vuforia-gameobject-transforms-are-not-being-disabled-when-the-player-is-stopped

 ### v.1.4.2
*05/14/2018*

##### New Features
- Layer names and property names are preloaded from the data source into a dropdown.
- Add `Location Prefabs` section to `AbstractMap`. Enables users to spawn prefabs at points-of-interest on the map directly from the `AbstractMap` UI.
- Add options to spawn prefabs on points-of-interest using `Mapbox Category`, `Name`, or a list of `Address or LatLon`.
- Add methods on `AbstractMap` to spawn prefabs. Enables users to add layers via script.
- Add the `LocationPrefabs.unity` example scene. Demonstrates the basics of using the `Location Prefabs` feature.
- Add `OnUpdated` event to `AbstractMap`. Enables subscribers to get a notification when the map location and/or zoom gets updated.
- `DeviceLocationProviderAndroidNative`: a native location provider for Android that directly accesses location and orientation provided by the operating system not using Unity's `Input.location` which has some shortcomings, eg coordinates truncated to `float` precision.
  **Only available on Android 7.0 (Nougat, API Level 24) and up!**
- `EditorGpsLogLocationProvider`: an Editor location provider to replay real world GPS traces logged with [GNSSLogger](https://github.com/google/gps-measurement-tools/tree/master/GNSSLogger)
- New example scene showing `UserHeading` and `DeviceOrientation`

##### Improvements
- `UnityARInterface` updated to [commit 67351b6](https://github.com/Unity-Technologies/experimental-ARInterface/commit/67351b66ff9af28380e7dd5f5b2f4ba1bf802ca8) from March 1st 2018
- Additional properties on `RotateWithLocationProvider` to account for new `Location` properties `UserHeading` and `DeviceOrientation`.
- Changes to terrain factory (#623)
  - Create a new base class
  - Introduce all different terrains as strategies
  - Change sidewalls as a property on elevated terrain strategy
- Data Fetching Changes (#622)
  - Move data calls from factories to DataFetcher classes
  - Factories register to events in DataFetchers
-  `Layer Name` , `Extrusion Property Name` and `Filter Key` are now selectable dropdowns indicating the layers and properties available in the current `Data Source`. Layers and properties no longer require manual string entry.

##### Bug Fixes
- Added checks to prevent NRE in `GeocodeAttributeSearchWindow` when searching with an invalid token or no connection.
- Fix issue where side wall mesh generation did not work with elevated terrain.
- Fix issue with scaling prefabs for POI's. Enables correct scaling of objects with map.

##### Known Issues
- `Filters` with empty key or value parameters will exclude all features in a layer.
-  SDK will throw `[Physics.PhysX] cleaning the mesh failed` error if a MapFeature is used with the following options: {Primitive Type: `Line`, LayerName: `Road`, Collider Type: `Mesh Collider`}

##### BREAKING CHANGES
- Property `Heading` on `Location` object has been split into `UserHeading` and `DeviceOrientation`.
  - `UserHeading`: the direction the user is moving. This is calculated from the latest position. If the user stops moving the last heading value is kept.
  - `DeviceOrientation`: value obtained from the device compass. Where the device is looking to.

### v.1.4.1
*04/17/2018*

##### New Features
- Add two new modules, KdTreeCollection and AddToCollection Gameobject modifier.
- Add Collider option for vector features.
- Add scale factor for extrusion value derived from feature property.
- Add camera script with zoom & pan support for TabletopAR scene.

##### Bug Fixes
- Remove `buildingsWithUniqueIds` setting for `Mapbox Streets` data source.
- Change `Style Name` to `Data Source`
- Fix to make filter values case insensitive.
- Fix issue where position vector features was not being set.
- Fix `Range Property` extrusion option for vector features.
- Select newly created layer visualizer.
- Fix typo in colorPalette.
- Add defaults for all sublayer properties to not carry over any options from the previous layer.
- Don't allow empty names for layer visualizers.
- Fix foldouts not retaining states between play mode & editor.
- Add missing tooltips.
- Fix issue with Satellite TextureType.
- Added a check to prevent NRE on tile update because map was not initialized.
- Added method to disable `InitializeOnStart` in the `Initialize With Location Provider` script.
- Fix loop counter in `SpawnInsidePrefabModifier` which was causing an infinite loop.

### v.1.4.0
*03/20/2018*

#####
- Drag and drop prefabs for most common use cases.
- New Abstract Map UI
- Style development - colorization and atlas template generator
- Use texture atlas for building geometries.
- Merge buildings with unique ids using the experimental 3D buildings tileset.
- Added a API call on AbstractMap to query height at a certain latitude longitude.
- Included EditorConfig file to enforce coding style
- Integration of previously seperate AR support https://github.com/mapbox/mapbox-unity-sdk/pull/544

### v.1.3.0
*12/18/2017*

##### Upgrade Instructions
- As always, if you already have a project with a previous version of the SDK, please remove older versions before installing this new one!
- `FeatureBehaviour` is no longer added to feature gameobjects by default. Use a `FeatureBehaviourModifier` to add this component (optimization).
- `TextureModifier` is obsolete. Please use `MaterialModifier`.
- `LocationProvider` has been heavily modified to support additional features. Please update you references accordingly.
- `MeshFactory` and `PoiVisualizer` have been deprecated (previously obsolete).

##### Memory/Performance Upgrades
- Added ability to use coroutines inside `VectorLayerVisualizer`—this option will help prevent locking main thread during tile creation. Note that this will normally result in longer map load times, however.
- Objects generated from vector tiles (meshes, game objects, colliders, renderers, etc.) are now pooled and re-used accordingly. This expedites feature creation and helps prevent memory fragmentation.
- Fixed some memory leaks.
- General performance improvements—maps should now load much faster, in general.

##### New Features
- Added convenience methods to `AbstractMap`  to convert between Lat/Lon ↔ Vector3. This also works for maps that have been translated, rotated, or scaled! #387
- Added tile error exception events that you can hook into to know when specific tiles fail to load. We provide an example `TileErrorHandler` object that logs these to console.
- Added C# wrapper for Mapbox Token API (to validate tokens).
- Added transparency shader for raster tiles—this enables you to convert black to alpha from Mapbox studio styles (to render just road labels, for example).
- Added new `LoftModifier`—use this to make complex geometry from line features.
- Added prefabs for location provider and “player” avatar for convenience. #382
- Added Mapbox Map Matching API #302

![image](images/changelog/map-matching.gif)

##### Bug Fixes
- It should now be safe to nest the Mapbox folder (SDK) in your own projects.
- You can now properly change/load scenes at runtime. #381
- Fixed token validation issues (validate token when initializing `MapboxAccess`). #430
- Custom inspectors now properly serialize assets assigned via built-in object browser.
- Fix for certain tile providers not updating properly when re-initializing a map.
- Fixed issue where clipped features would result in several new features. #398
- Fixed one potential race condition with `DeviceLocationProvider` on iOS devices, where location would not properly send locations as expected.
- `Conversions.cs` are now properly supported in zoomable map implementations.
- Fixed bug where certain tile providers would give out of sync exception.
- Fixed `duplicate keys` exception related to some slippy maps.
- Fixed bugs related to snap modifiers not working properly depending on `feature position` of the stack.
- Removed duplicate event registration in `MapVisualizer`.
- Fixed bugs associated with `Filters` and string comparisons in `VectorLayerVisualizer`.

##### Improvements
- Made several methods virtual to better support extension.
- Improvements to map editor. #343
  - Map editor will refresh automatically when window receives focus.
  - VectorTileFactory now has checkboxes to toggle specific layer visualizers.
-  Use `InitializeMapWithLocationProvider` to initialize a map at a specific location (device location), but don’t forget to disable `InitializeOnStart` on your map object!
- LocationProvider has been abstracted for easier serialization/assignment in the Unity editor.
- LocationProvider now supports sending additional data with `Location` object.
- It is now easier to mock locations in the editor.
  - Array support to mock a specific route (`LocationArrayEditorLocationProvider`).
  - Transform support on `EditorLocationProvider` to create simple offsets.
  - Use Unity Remote app to send location updates directly to the Unity editor from your device!
- `DynamicZoom` example has been improved, see new `Zoomable` map example.
- Added string to lat/lon conversion method.
- Various nodes in the map factory framework now have public fields to support run-time styling or modification before map generation (based on settings, for example).
- Maps now support float values for zoom level—you can use this to inform camera controllers, for example.
- Added UnityTile getter to AbstractMapVisualizer. This enables `UnityTile` lookup at runtime. #382

##### Examples
- Optimized and consolidated examples.
  - Removed `Drive` example.
  - Removed `DynamicZoom` example (replaced with `ZoomableMap`).
  - Removed `LowPoly`.
  - Removed `MapReload` (many examples now support this functionality).
  - Removed `SlippyTerrain` example (see `ZoomableMap` for similar and improved functionality).
  - Removed `TerracedWorld`.
  - Added `Explorer` example—vector map example covering a variety of layers and features.
  - Added `ZoomableMap` example.
  - Combined MeshGeneration Examples into BasicVectorMap folder.
- See here for the latest.

##### Known Issues
- `ZoomableMap` does not constrain map position to camera bounds. At some zoom levels, the map will go above the camera.
- iOS/Unity 2017.1+ sometimes location updates are delayed or not sent properly. We suspect this is an iOS/Unity issue.
- If you have issues with missing libraries in Xcode, please check that you are not using `symlink` in your Unity build settings and that your min. target OS is 8.
- Requesting too many tiles at once (on client) will often lead to many failed tile requests. We recommend that you distribute these requests over many frames!

### v.1.2.0

*09/26/2017*

*Please note: this release marks the beginning of official support for the Unity 2017 lifecycle.*

##### Upgrade Instructions
- As always, please remove older versions before installing!
- `AbstractMap` has officially been abstracted! This means that you must substitute, in your own scenes, a concrete implementation. We offer `BasicMap`, `MapAtSpecificLocation` and `DynamicZoomMap`.
- MapVisualizer has also been abstracted for tile placement purposes. You may need to point your existing maps to the new concrete implementation (`MapVisualizer`) and re-serialize your references (`Factories`).

##### Memory/Performance
- General performance optimizations.

##### New Features
- `DynamicZoomMap`, `DynamicMapVisualizer` and `DynamicZoomTileProvider` demonstrate how to build web-like maps that can pan and zoom smoothly.
- You can now convert latitude/longitude to a position on the sphere (see `Conversions.cs` and the `Globe` example).
- Use the new `MapAtSpecificLocation` component to create a map that is centered precisely at a given location.
- Use the new `RangeAroundTransformTileProvider` component to procedurally load tiles as a transform changes. This is useful for loading map content as a player avatar moves based on physical location. Check the `LocationProvider` example for more information.
- Map Editor (Mapbox --> Map Editor)
  - Allows navigation of any Map Factory Frameworks referenced in the current scene
  - Selecting a node will select the corresponding asset for manipulation in the inspector
  - Added new asset browsing capabilities to these nodes (factories, modifiers, etc.)

![image](images/changelog/node-editor.gif)

##### Bug Fixes
- We now catch exceptions when parsing Android library file names (duplicate library check).
- Fixed some wrong `Conversions` values and methods that would lead to improper positioning or gaps between tiles.
- The Mapbox configuration window no longer gets stuck validating a token.
- Wolfulus line ending fixer issues in Unity 2017 have been resolved.
- Fixed broken `VectorTileExample`.
- `AbstractMapVisualizer` state change events no longer fire improperly when loading tiles from cache.
- Tiles should no longer appear all white or all black when using very small `UnityTileSize`.
- `Map.cs` no longer executes the notify callback twice.

##### Improvements
- Maps can now be `Initialized` with a latitude/longitude and a zoom level, at any time!
    - This means that you can re-use a map for a new location or zoom.
- Scaling is now performed at a vertex level, which helps prevent floating-point imprecision and leads to a cleaner hierarchy.
- `Vector2d` is now  serializable (for inspector purposes).
- `MapId` and `Factories` are now public so that you can change them at runtime.

##### New Examples
- [See here for the latest.](https://www.mapbox.com/unity-sdk)

### v.1.1.0

*08/01/2017*

*Please note: our next release will end support for the Unity 5.x lifecycle. We look forward to taking advantage of Unity 2017 optimizations and features.*

##### Upgrade Instructions
- As always, please remove older versions before installing!
- `TypeFilter` now uses an array. You will need to update your filters!
- `MeshFactory` is obsolete. Please replace with `VectorTileFactory` or `StyleOptimizedVectorTileFactory`.
- `PoiVisualizer` is obsolete. Please use a standard `VectorLayerVisualizer` with a `PrefabModifier`.
- Triangle.NET has been replaced with `Earcut`. If you were using Triangle features, you will need to [import that library yourself](https://github.com/mapbox/triangle.net-uwp).
- If you were using a `RangeTileProvider`, you will need to update its parameters, which are now described in terms of North, East, South, and West (for readability).
- `ChamferModifier` has been replaced with `ChamferHeightModifier`. This new version looks and performs better.
- Ensure your `MapImageFactories` have the expected `Map Id`! We've updated the way Ids are serialized, and your old Ids may have been lost--sorry!

##### Memory/Performance
- Added support for style-optimized vector tiles! [Read more here.](https://www.mapbox.com/api-documentation/maps/#request-style-optimized-tiles)
    - You will need to use the new `StyleOptimizedVectorTileFactory`.
    - This can result in far less data transfer/data processing.
- Replaced Triangle.NET with `Earcut` which results if much faster geometry construction.
- Removed expensive string concatenation process in `FeatureBehaviour`.
- `MeshFactory` no longer waits for `Terrain` or `Raster` results before making its own web request.

##### New Features
- Added ability to cache successful tile requests to disk (via SQL database). If a tile is found in the database, it will not make a web request.
    - Note that tiles in this database only expire when the cache fills up!
- Want to create a low-poly landscape? Use the new `LowPolyTerrainFactory`!
- Maps can now be snapped to `y=0` (this prevents the need to reposition your camera at higher elevations).
- Added ability to choose pivot of objects generated with the `ModifierStack` (tile center, first vertex, or vertex average).
- Use the new `GameObjectModifier` `AddMonoBehavioursModifier` to "generically" add `Components` to your game objects.
- Added ability to snap to terrain/other objects with `SnapTerrainRaycastModifier`. This is more accurate (but slower) than `SnapTerrainModifier`.
- Added "example" `SpawnInsideModifier` which can be used to add procedural decoration inside a mesh (for example, `Landuse`).
- Added `ChamferHeightModifier` which, obviously, combines `Chamfer` and `Height` modifiers.
- Added `GlobeTileProvider` to request the entire world at once (be advised that this may result in MANY tile requests!).
- Project your tiles on a sphere! Use the new `GlobeTerrainFactory` to make a globe.
- Added ability to pick a custom style from the Mapbox Styles API.
    - Use the search button and enter your user name (for Mapbox Studio).
    - Note that this requires you to [create an API token](https://www.mapbox.com/studio/account/tokens/) that supports `styles:list`!

##### Bug Fixes
- WebGL builds now work as expected!
- Fixed Unity Cloud Build and iOS signing issues that were related to native iOS libraries.
- Map tiles are now parented correctly to the map root (on device). If you previously had trouble rotating/moving/scaling a map on device, fret no longer!
- Fixed some bad height calculations for buildings.
- `PolygonModifier` now correctly generates holes and better handles multiple `parts`.
- Tiles should now wrap at world boundaries correctly (rather than producing invalid tile requests).

##### Improvements
- Unity/C# warnings have been addressed.
- Added preprocess build step and a unit test that checks for duplicate Android libraries.
- Example `CameraMovement` script has been improved to allow for mouse wheel translational zoom and precise touch drag (for panning).
- Added option to remove sidewalls from height-extruded geometry (for roads, for example).
- Use `VertexDebugger` to debug vertices for procedurally generated meshes.
- Added timeout exception to `HttpRequest`.
- RangeTileProvider is now more readable.

##### New Examples
- See `Globe` example for one method of spherically projecting tiles.
- `MeshGenerationBasics` has new interactive elements to demonstrate some of the new features we've added.
- `StylingDemoMeshGeneration` uses `SpawnInsideModifier` to add "bushes" to `landuse:park`.
- Check `TerracedWorld` to see an example of how to use contour data to generate Godus-like worlds.

### v.1.0.0

*05/26/2017*

##### Memory/Performance

- Added support for runtime texture compression (DXT) in the `MapImageFactory`
- `MapVisualizer` now pools gameobjects/textures/data to avoid instantiation and destruction costs
- TerrainFactory now allocates less memory when manipulating geometry
- Elevation textures are no longer held in memory and height data parsing and access is much faster
- Added new `FlatTerrainFactory` that is optimized specifically for flat maps
- Tiles can now be cached in memory—configure the cache size in `MapboxAccess.cs` (default size is 500)
- Slippy maps now dispose tiles that determined to be "out of range"
  - Tiles that are out of range before completion are properly cancelled
- Terrain generation in Unity 5.5+ should be much faster and allocate less memory

##### New Features

- Added new retina-resolution raster tiles
- Added mipmap, compression, and retina-resolution support to `MapImageFactory`
- The `PoiGeneration` example now includes clickable 3D world-space gameobjects—use these as reference for placing objects in Unity space according to a latitude/longitude
- `MapVisualizer` and `TileFactories` now invoke state change events—use these to know when a map or specific factory is completed (loaded)

  - See an example of implementing a loading screen in `Drive.unity`
- You can now specify GameObject `Layer` for tiles in the `TerrainFactory`
- Add colliders to your terrain by checking the `Add Collider` flag in the `TerrainFactory`
- Add colliders or specify GameObject `Layer` for buildings, roads, etc. with `ColliderModifier` and `LayerModifier`

##### Bug Fixes

- Building snapped to terrain are now rendered correctly (check `Flat Tops` in the `HeightModifier`)
- Web request exceptions are now properly forwarded to the `Response` (should fix `Unknown tile tag: 15`)
- Complex building geometry should now be rendered correctly (holes, floating parts, etc.)
- Materials assigned to a `TerrainFactory` are now properly applied at runtime
- Because of `UnityTile` pooling, you should no longer encounter `key already exists in dictionary` exceptions related to tile factories—this means you can change map attributes (location, zoom, terrain, etc.) at runtime without throwing exceptions

##### Improvements

- Map configuration values are no longer static, and an `OnInitialized` event is invoked when the `AbstractMap` reference values have been computed (prevents temporal coupling)
- Snapping to terrain has been simplified—just add a `SnapToTerrainModifier` to your `ModifierStack`
- `Slippy.cs` has been refactored to `CameraBoundsTileProvider.cs` and the backing abstraction enables you to write your own tile provider system (zoomable, path-based, region, etc.)
- `MapController.cs` has been refactored to `AbstractMap` —this is not yet abstract, but should provide an example of how to construct a map using a `MapVisualizer` and a `TileProvider`
- `UnityTile` has been refactored to support reuse and has the ability to cancel its backing web requests
- `DirectionsFactory` no longer relies on a `MapVisualizer` or `DirectionsHelper`, but can still use existing `MeshModifiers`

### v0.5.1

*05/01/2017*

- Terrain height works as intended again (fixed out of range exception)
- Fixed issue where visualizers for `MeshFactories` were not being serialized properly
- Fixed null reference exception when creating a new `MeshFactory`

### v0.5.0

*04/26/2017*

- Added support for UWP
    - Share your Hololens creations with us!
- Fixed precision issue with tile conversions
    - Replaced `Geocoordinate` with `Vector2d`
- Mapbox API Token is now stored in MapboxAccess.txt
    - `MapboxConvenience` has been removed
- Added `LocationProviders` and example scene to build maps or place objects based on a latitude/longitude
- Mesh Generation:
    - General performance improvements (local tile geometry)
    - Custom editors for map factories
    - Added new `MergedModifierStack` which will reduce the number of transforms and draw calls in dense maps
    - Continuous UVs for building facades
    - `DirectionsFactory` now draws full geometry, not just waypoints
    - Fixed occasional vertex mismatch in `PolygonMeshModifier.cs` (which caused an index out of range exception)

### v0.4.0
- Updates mapbox-sdk-unity-core to v1.0.0-alpha13; features vector tile overzooming
  - Updates to attribution guidelines in README.MD
  - Added Conversions.cs and VectorExtensions.cs to enable simple conversions from geocoordinate to unity coordinate space

### v0.3.0
- Added new infrastructure for mesh generation
  - Added new demos for basic, styled, point of interest vector mesh generation
  - Added new demo for vector tiles + terrain with a slippy implementation (dynamic tile loading)
  - Added a new demo for Mapbox Directions & Traffic
  - Deprecated old slippy demo
  - Deprecated old directions component demo

### v0.2.0
- Added core sdk support for mapbox styles
  - vector tile decoding optimizations for speed and lazy decoding
  - Added attribution prefab
  - new Directions example
  - All examples scripts updated streamlined to use MapboxConvenience object

### v0.1.1
- removed orphaned references from link.xml, this was causing build errors
  - moved JSON utility to Mapbox namespace to avoid conflicts with pre-exisiting frameworks
