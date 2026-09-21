## Landmarks Module — Internals

Technical reference for the Landmarks Module internals. For setup instructions, see [Landmarks Module](LandmarksModule.md).

---

### Loading Pipeline

The module operates at **z14 tile level**. Each z14 tile contains glTF binary data for all landmarks in that area. When a tile loads:

1. **Fetch**: `Source<BuildingData>` downloads the glTF binary from the Mapbox 3D Buildings tileset
2. **Parse**: `GltfVisualizer` uses glTFast (`com.unity.cloud.gltfast`, a package dependency) to asynchronously import and instantiate the glTF scene, threading a `CancellationTokenSource` it keeps per tile (`_activeLoads`) through both `gltf.Load` and `gltf.InstantiateSceneAsync` — see [Cancellation](#cancellation) below for when that token is actually cancelled
2b. **Decode feature IDs**: `FeatureIdColorTable` decompresses the tile's `_FEATURE_ID_RGBA4444` buffer view straight from the GLB bytes and `FeatureIdMeshWriter` writes the result onto each mesh as a Color vertex stream
3. **Extract footprints**: During glTF instantiation, nodes tagged with `mapbox:footprint:id` in their extras are identified as footprint polygons. These polygons are extracted (as `Vector3[]` arrays in 0–1 tile space) and separated from the visible 3D geometry
4. **Position**: Instantiated GameObjects are positioned and scaled to match the map's current state
5. **Snap to terrain**: `LandmarkTerrainSnapper.Register` takes the tile's building anchors and, if a terrain module is present and elevation data has already arrived for the point under each building's footprint, writes each building's Y immediately. A building whose terrain has not arrived yet is left at ground level and is corrected later — see [Terrain Snapping](#terrain-snapping) below. Registration happens after positioning, since snapping writes each building's local Y relative to a root that positioning has already placed
6. **Announce buildings**: `BuildingLoaded` fires once per landmark mesh, walking out from each `LandmarkAnchor` rather than down from the tile root — the anchor is the only thing that knows which landmark id a given mesh belongs to, since the tile publishes the id on the building node and nothing about the resulting geometry records it. A building with several primitives, or with mesh nodes nested below its root, raises the event once per mesh, all carrying the same id
7. **Notify**: `TileFinished` fires with the tile ID and footprint polygons, enabling downstream building suppression

When a tile is no longer retained (user panned away), the cache evicts it:

1. **Destroy visuals**: GameObjects are destroyed. There is no per-building unload event — visuals are released a whole tile at a time, which the `TileDisposed` step below already reports, and a subscriber holding a landmark GameObject can compare it against null like any other destroyed Unity object
2. **Unregister**: `LandmarkTerrainSnapper.Unregister` drops the tile's entries unconditionally, since a tile can have anchors registered even on a path where the visual cache entry is already gone
3. **Notify**: `TileDisposed` fires with the tile ID, enabling downstream cleanup

If the tile's glTF load was still running when its data was evicted, eviction instead cancels that load (see [Cancellation](#cancellation)) and lets the cancelled result release itself when it arrives; there are no visuals yet for this path to destroy.

---

### Cancellation

`GltfVisualizer` keeps a `CancellationTokenSource` per in-flight tile in `_activeLoads` rather than the bare id set it used to be a `HashSet` of, and threads the token through both `gltf.Load` and `gltf.InstantiateSceneAsync`. glTFast throws `OperationCanceledException` out of either call when the token is cancelled; `Generate` catches that, releases the import and the partially-built root, and reports `TaskResultType.Cancelled` instead of running the load to completion and throwing the result away on arrival.

`GltfVisualizer.Cancel(CanonicalTileId)` is the entry point, and it is called from exactly two places:

- `LandmarksLayerModule.ClearDisposedDataVisual`, when a tile's data is evicted from the cache while its glTF load is still in flight — the bytes being decoded have just been invalidated, so finishing the load would build a tile with nowhere to go.
- `GltfVisualizer.OnDestroy`, which cancels every load still running so a torn-down map does not leave one finishing into a destroyed scene.

In `LandmarksLayerModule.GenerateTask`'s result callback, a `Cancelled` result returns immediately, before the branch that would call `RecordGenerationFailure` — so it is never counted as a failed attempt. The tile is simply left unresolved: it is not added to `_visualCache` and not added to `_unbuildableTiles`, so it looks exactly like a tile nobody has tried yet and is generated again if it comes back into the cover.

**What is deliberately not cancelled** is a tile merely dropping out of the retained set. `RetainTiles` computes the retained set and calls `EnsureRetainedTilesReady`, but `GetTileCoverCoroutines` — the path `LoadTileCoverToMemory` drives — generates tiles from the current cover regardless of whether they are in `_retainedBuildingTiles`. That is intentional: it is how the flight controller's own preloading gets tiles ready ahead of the camera arriving at them. Cancelling generation the moment a tile leaves the retained set would abort exactly that preloading, so un-retaining a tile is not treated as a reason to cancel it — only losing the underlying data is.

---

### Terrain Snapping

**Class:** `LandmarkTerrainSnapper`, owned by `LandmarksLayerModule` and constructed alongside it — the visualizer itself does not know it exists.

The snapper drives each landmark building's transform rather than baking elevation into its mesh. That choice is deliberate: elevation is not necessarily known at generation time, since the terrain tile under a building may still be in flight, and in shader mode the CPU-side elevation decode lands after the terrain tile itself reports as loaded. Baking a height into vertices at generation time would be a one-way bet — vector features do exactly that (`SnapTerrainModifier` writes elevation into `MeshData` and nothing ever re-snaps), so a building that generated one frame too early stays at sea level on a hillside for the rest of the session. Because landmarks are positioned by transform instead, a late correction costs one assignment.

Elevation is cached in **metres**, and the Unity Y is derived from it on demand rather than stored directly. This matters because the two things that can invalidate a cached Y are not equally expensive: a change to `IMapInformation.Scale` (which happens on every zoom step under `DynamicScalingMapInformation`) only changes the divisor and needs no terrain re-sampling at all — `ReapplyForViewChange` re-derives every known Y from its cached metres for exactly this case. Only terrain actually arriving requires a real re-query, and then only for buildings that don't have a reading yet.

A building snaps to the **minimum** elevation sampled over its footprint polygon, not the elevation at its footprint's centre or anchor point — see `TrySampleGroundHeight`. A flat-bottomed building placed at its centre height leaves the downhill edge floating on any slope; snapping to the lowest sampled point buries the uphill edge instead, which reads as a real foundation. If the tile published no footprint for a landmark, or every footprint sample misses elevation data, sampling falls back to the anchor's own ground UV.

Arrivals are coalesced and scoped rather than re-walking every registered tile on every `TileLoaded`: one terrain data tile is shared by up to 16 render tiles, so a single elevation decode arrives as a burst of `TileLoaded` events, which a dirty flag plus a per-frame drain loop (`DrainLoop`) coalesces into one pass. `ResolveArrivals` further narrows that pass to only the landmark tiles a just-arrived terrain tile actually covers (`IsCoveredByArrival`), rather than re-sampling every footprint vertex of every registered tile on every burst.

See the "Settled" note in [Known Gaps](LandmarksModuleKnownGaps.md) for why sampling always uses the z14 landmark tile id even though terrain data can live at a shallower zoom, and why that is correct rather than a bug waiting to happen.

---

### Building Overlap Problem

Three systems create buildings on the map:

| System | What it renders | How it works |
|--------|----------------|--------------|
| **Landmarks Module** | Detailed 3D glTF models | Fetches pre-built meshes from Mapbox API |
| **Components Module** | Basic extruded OSM buildings | Zero-allocation PBF pipeline, merged meshes per tile |
| **Vector Module** | Basic extruded OSM buildings | Modifier stack pipeline, per-feature GameObjects |

When a landmark exists at a location, the basic OSM building at the same spot must be suppressed. Otherwise both render on top of each other. Two connector scripts handle this — one for each basic building system.

---

### Connector 1: Components Module Synchronization

**Script:** `LandmarkToBuildingComponentConnector`

This connector handles overlap with the Components Module's `BuildingComponentVisualizer`. Because the Components Module generates merged meshes (all buildings in a tile baked into one mesh), individual buildings cannot be filtered before generation. Instead, overlapping buildings are hidden **after** mesh generation by zeroing their vertices.

**How it works:**

1. Subscribes to both `LandmarksLayerModule.TileFinished` (landmark polygons ready) and `BuildingComponentVisualizer.OnBuildingMeshCreated` (building mesh ready)
2. When either event fires, performs collision detection between building footprints and landmark footprints
3. Collision detection uses a multi-pass approach:
   - **Bounding box check** — fast rejection using precomputed `Bounds`
   - **Separation heuristic** — rejects near-misses
   - **Complexity ratio check** — skips wildly mismatched polygon pairs
   - **Full polygon intersection** — `PolygonsIntersect()` for remaining candidates
4. If a building intersects a landmark, all its vertices are set to `Vector3.zero`, effectively hiding it
5. The modified vertex list is pushed back to the mesh via `SetVertices()`

**Handles both arrival orders:** If buildings arrive before landmarks, they are checked when landmarks load. If landmarks arrive first, buildings are checked on creation.

**Scene setup:**
1. Add `LandmarkToBuildingComponentConnector` to your map GameObject
2. Assign:
   - **Map** → `MapBehaviourCore`
   - **LandmarksLayerModuleScript** → the landmarks module
   - **BuildingLayerVisualizerObject** → the `BuildingComponentVisualizerObject` asset

---

### Connector 2: Vector Module Synchronization

**Script:** `LandmarkToVectorConnector` + `LandmarkPolygonFilterObject`

This connector handles overlap with the Vector Module's `VectorLayerVisualizer`. The Vector Module evaluates `ILayerFeatureFilterComparer.Try()` on every feature before generating meshes, so landmark polygons can be injected as a filter that rejects overlapping features — preventing mesh generation entirely. This is cleaner and cheaper than post-generation hiding.

**How it works:**

1. `LandmarkToVectorConnector` subscribes to `LandmarksLayerModule.TileFinished`
2. When landmark footprints arrive, they are fed into `LandmarkPolygonFilterObject` via `AddLandmarkPolygons()`
3. The filter stores polygons at z14 tile space with precomputed bounding boxes
4. When the Vector Module processes a feature, `LandmarkPolygonFilter.Try()` is called:
   - Computes the feature's parent z14 tile
   - Transforms the feature's bounding box to z14 space (cheap: scale + offset)
   - Checks against landmark bounding boxes (fast rejection for 99%+ of features)
   - On bbox hit: transforms the full feature polygon to z14 space and runs `PolygonsIntersect()`
   - Returns `false` to reject the feature (skip mesh generation)
5. If the Vector Module has already processed tiles when landmarks arrive (race condition), `VectorLayerModule.ReloadTile()` re-runs the pipeline with the now-populated filter

**Scene setup:**
1. Create a `LandmarkPolygonFilterObject` asset: right-click in Project → `Create → Mapbox → Filters → Landmark Polygon Filter`
2. Add this filter asset to your vector building layer's `VectorFilterStackObject.Filters` list
3. Add `LandmarkToVectorConnector` to your map GameObject
4. Assign:
   - **Map** → `MapBehaviourCore`
   - **LandmarksLayerModuleScript** → the landmarks module
   - **VectorModuleScript** → the vector module
   - **LandmarkFilter** → the `LandmarkPolygonFilterObject` asset created in step 1

---

### Coordinate Spaces and Cross-Tile Handling

Both connectors operate in **z14 tile space**. Landmark polygons use coordinates in the 0–1 range (X: 0→1 left to right, Z: 0→-1 top to bottom). Buildings from the Components or Vector modules may be at z15, z16, or higher zoom levels.

**Zoom level transformation:** When comparing a building at z15+ against a landmark at z14, the building's coordinates are transformed to z14 space using `CalculateTopLeftScaleOffsetAtZoom()`. This returns a scale and offset (e.g., z15 → scale 0.5, z16 → scale 0.25) that maps the building's local tile coordinates into the parent z14 tile's coordinate system.

**Tile edge handling:** Landmark footprints near tile edges may extend beyond the 0–1 bounds. When this happens, the polygon is replicated into all affected neighboring z14 tiles (up to 8: 4 cardinal + 4 diagonal) with coordinates adjusted for the neighbor's space. This ensures that a building in a neighboring tile still gets suppressed even though the landmark's source tile is different.

**Cleanup:** When a landmark tile is disposed, `TileDisposed` fires. Both connectors remove all polygon data that originated from that source tile, including replicated copies in neighbor tiles. Each polygon entry tracks its source tile ID to enable selective removal without affecting polygons from other sources.

---

### Events

`LandmarksLayerModule` exposes these events:

| Event | Signature | When |
|-------|-----------|------|
| `TileFinished` | `Action<CanonicalTileId, List<Vector3[]>>` | glTF instantiation complete, footprint polygons extracted |
| `TileDisposed` | `Action<CanonicalTileId>` | Tile evicted from cache, visuals destroyed |
| `BuildingLoaded` | `Action<GameObject, string>` | One landmark mesh GameObject activated, with the landmark id the tile publishes for it |

There is no `BuildingUnloaded` event. An earlier version had one, but it fired once per tile root rather than once per building despite its name, had no subscribers, and duplicated what `TileDisposed` already reports — visuals are released a whole tile at a time, so a subscriber that wants to know a landmark is gone can watch `TileDisposed` and compare its own held GameObjects against null.

`BuildingLoaded`'s id is carried on the event rather than left for a subscriber to look up, because nothing downstream of instantiation can recover it: the id lives on the glTF node's `extras`, and the geometry that survives instantiation has no memory of which node it came from. The event fires by walking out from each `LandmarkAnchor` — the anchor is the one place that pairs a mesh's landmark id with its transform — rather than by walking down from the tile root, which is what an earlier version did and which had no way to attach an id to what it found. `LandmarkComponent.LandmarkId`, stamped from this event's second argument, is what turns a raycast hit into an answer to "which landmark is this" — see [Selecting a Landmark](LandmarksModule.md#selecting-a-landmark).

---

### Architecture Diagram

```
LandmarksLayerModule
│
├── Source<BuildingData>          ← Fetches glTF binary from Mapbox API
│
├── GltfVisualizer                ← Async glTF import via glTFast
│   ├── MapboxGltfImport          ← Custom schema chain capturing feature ID attributes
│   ├── FeatureIdColorTable       ← Decodes feature IDs from the tile's GLB bytes
│   ├── MapboxGltfInstantiator    ← Extracts footprint nodes, flips Y/Z axes
│   │   └── FeatureIdMeshWriter   ← Writes feature IDs as a Color vertex stream
│   └── MapboxGltfMaterialGenerator ← Applies base PBR material
│
├── TileFinished event ──────┬──→ LandmarkToBuildingComponentConnector
│                            │       └── Zeroes overlapping building vertices
│                            │
│                            └──→ LandmarkToVectorConnector
│                                    └── LandmarkPolygonFilter.Try()
│                                        └── Rejects features before mesh generation
│
└── TileDisposed event ──────┬──→ Component connector cleanup
                             └──→ Vector filter cleanup
```

---

### Key Files

| File | Purpose |
|------|---------|
| `LandmarksLayerModule.cs` | Core module: tile loading, caching, visual lifecycle |
| `Unity/LandmarksLayerModuleScript.cs` | MonoBehaviour wrapper for scene setup |
| `Gltf/GltfVisualizer.cs` | Async glTF loading and instantiation |
| `Gltf/GltfGenerationTaskResult.cs` | Result container with visuals + footprints |
| `Gltf/LandmarkAnchor.cs` | Per-building anchor: transform, ground UV, footprint UVs, landmark id |
| `Gltf/MapboxGltfImport.cs` | Custom glTFast importer (feature ID schema chain) |
| `Gltf/MapboxGltfMaterialGenerator.cs` | PBR material setup for glTF |
| `Gltf/FeatureId/FeatureIdSchema.cs` | Schema chain capturing `_FEATURE_ID_RGBA4444` |
| `Gltf/FeatureId/GlbBinaryChunk.cs` | Locates the BIN chunk in a GLB buffer |
| `Gltf/FeatureId/MeshoptBufferViewDecoder.cs` | Decompresses one meshopt buffer view |
| `Gltf/FeatureId/FeatureIdColorTable.cs` | Holds the tile's decompressed feature ID bytes and per-primitive offsets |
| `Gltf/FeatureId/FeatureIdDecoder.cs` | Unpacks RGBA4444 + part ID straight into a color buffer |
| `Gltf/FeatureId/FeatureIdMeshWriter.cs` | Adds the Color vertex stream to glTFast meshes |
| `LandmarkTerrainSnapper.cs` | Sits landmark buildings on elevated terrain by driving their transforms |
| `LandmarkToBuildingComponentConnector.cs` | Components module overlap suppression |
| `LandmarkToVectorConnector.cs` | Vector module overlap suppression |
| `LandmarkPolygonFilterObject.cs` | Filter asset + filter logic for vector pipeline |
| `Unity/LandmarkComponent.cs` | Marker component added to every landmark mesh, carrying the landmark id so a raycast hit can be identified |
| `Unity/LandmarkHighlightBehaviour.cs` | Raycast-to-select and highlight-tint behaviour; on the `Map` GameObject in the `LandmarkDemo` scene |
| `../BaseModule/Utilities/MapboxGeometryUtilities.cs` | Polygon intersection, bounds, point-in-polygon. Moved here from `LandmarksModule/MapboxGeometryUtilities.cs`, since none of what it does is landmark-specific |

---

### Troubleshooting (Technical)

For basic setup troubleshooting, see [Landmarks Module](LandmarksModule.md#troubleshooting).

**Landmarks at tile edges not suppressing buildings:**
- Both connectors replicate polygons to all 8 neighboring z14 tiles. If a landmark extends beyond the tile boundary but its neighbor tile's polygon data is missing, check that `AddLandmarkData`/`ReplicateToNeighbors` boundary thresholds match the footprint coordinates (X: 0–1, Z: 0 to -1)

**TileFinished not firing:**
- Verify glTF binary data is non-empty — `GltfVisualizer.Generate()` short-circuits with a `NoData` result (not a failure) if `Data` is null or empty. `TileFinished` still fires for it, just with an empty footprint list, since most tiles genuinely hold no landmarks and the tile still has to resolve rather than be regenerated forever
- Check `_buildingSource.CacheItemDisposed` is wired — if the source isn't initialized, tiles won't load
