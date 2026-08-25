## Landmarks Module — Internals

Technical reference for the Landmarks Module internals. For setup instructions, see [Landmarks Module](LandmarksModule.md).

---

### Loading Pipeline

The module operates at **z14 tile level**. Each z14 tile contains glTF binary data for all landmarks in that area. When a tile loads:

1. **Fetch**: `Source<BuildingData>` downloads the glTF binary from the Mapbox 3D Buildings tileset
2. **Parse**: `GltfVisualizer` uses glTFast (`com.unity.cloud.gltfast`, a package dependency) to asynchronously import and instantiate the glTF scene
2b. **Decode feature IDs**: `FeatureIdColorTable` decompresses the tile's `_FEATURE_ID_RGBA4444` buffer view straight from the GLB bytes and `FeatureIdMeshWriter` writes the result onto each mesh as a Color vertex stream
3. **Extract footprints**: During glTF instantiation, nodes tagged with `mapbox:footprint:id` in their extras are identified as footprint polygons. These polygons are extracted (as `Vector3[]` arrays in 0–1 tile space) and separated from the visible 3D geometry
4. **Position**: Instantiated GameObjects are positioned and scaled to match the map's current state
5. **Notify**: `TileFinished` fires with the tile ID and footprint polygons, enabling downstream building suppression

When a tile is no longer retained (user panned away), the cache evicts it:

1. **Destroy visuals**: GameObjects are destroyed, `BuildingUnloaded` fires per object
2. **Notify**: `TileDisposed` fires with the tile ID, enabling downstream cleanup

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
| `BuildingLoaded` | `Action<GameObject>` | Individual landmark mesh GameObject activated |
| `BuildingUnloaded` | `Action<GameObject>` | Individual landmark mesh GameObject about to be destroyed |

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
| `Gltf/MapboxGltfImport.cs` | Custom glTFast importer (feature ID schema chain) |
| `Gltf/MapboxGltfMaterialGenerator.cs` | PBR material setup for glTF |
| `Gltf/FeatureId/FeatureIdSchema.cs` | Schema chain capturing `_FEATURE_ID_RGBA4444` |
| `Gltf/FeatureId/GlbBinaryChunk.cs` | Locates the BIN chunk in a GLB buffer |
| `Gltf/FeatureId/MeshoptBufferViewDecoder.cs` | Decompresses one meshopt buffer view |
| `Gltf/FeatureId/FeatureIdColorTable.cs` | Holds the tile's decompressed feature ID bytes and per-primitive offsets |
| `Gltf/FeatureId/FeatureIdDecoder.cs` | Unpacks RGBA4444 + part ID straight into a color buffer |
| `Gltf/FeatureId/FeatureIdMeshWriter.cs` | Adds the Color vertex stream to glTFast meshes |
| `LandmarkToBuildingComponentConnector.cs` | Components module overlap suppression |
| `LandmarkToVectorConnector.cs` | Vector module overlap suppression |
| `LandmarkPolygonFilterObject.cs` | Filter asset + filter logic for vector pipeline |
| `MapboxGeometryUtilities.cs` | Polygon intersection, bounds, point-in-polygon |

---

### Troubleshooting (Technical)

For basic setup troubleshooting, see [Landmarks Module](LandmarksModule.md#troubleshooting).

**Landmarks at tile edges not suppressing buildings:**
- Both connectors replicate polygons to all 8 neighboring z14 tiles. If a landmark extends beyond the tile boundary but its neighbor tile's polygon data is missing, check that `AddLandmarkData`/`ReplicateToNeighbors` boundary thresholds match the footprint coordinates (X: 0–1, Z: 0 to -1)

**TileFinished not firing:**
- Verify glTF binary data is non-empty — `GltfVisualizer.Generate()` short-circuits with an empty success result if `Data` is null or empty
- Check `_buildingSource.CacheItemDisposed` is wired — if the source isn't initialized, tiles won't load
