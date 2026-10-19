## Landmarks Module — Known Gaps

What the module does not do yet, and what is worth doing next. Ordered by how likely each is to be noticed by someone using it.

For how the module works, see [Landmarks Module](LandmarksModule.md) and [Internals](LandmarksModuleInternals.md).

---

### Dynamic scaling is not supported

**Landmark heights are wrong after any zoom step when the map uses `DynamicScalingMapInformation`.**

`GltfVisualizer.FlipYZ` bakes the map's `Scale` into each vertex's Y at generation time:

```csharp
verts[i] = new Vector3(
    (matrix[12] / 8192) + (-vert.x / 8192) * matrix[0],
    vert.z / (_mapInformation.Scale * Conversions.LatitudeElevationCompensation(_mapInformation.LatitudeLongitude.Latitude)),
    (-matrix[13] / 8192) + (vert.y / 8192) * -matrix[5]);
```

`DynamicScalingMapInformation.Scale` is `_initialScale * ScaleCurve.Evaluate(Zoom)` — it changes on every zoom step, and raises `WorldScaleChanged`. Every other consumer recomputes: tiles are repositioned through `RepositionAllTiles`, and `LandmarksLayerModule.UpdatePositioning` re-derives each landmark's base Y for exactly this reason. The geometry *above* the base does not, because it is already in the vertex buffer. A landmark generated at one zoom keeps that zoom's vertical scale for as long as it lives, so buildings grow or shrink relative to the terrain and to each other as the user zooms.

Not visible in the shipped samples: all three use plain `MapInformation` with a fixed `Scale` of 1000, so the baked value never goes stale.

Two ways out, neither small:

- **Move the divisor out of the vertices.** Store raw metres in Y and apply `1 / (Scale · cos φ)` as a transform scale on the landmark tile root, so a scale change is a transform write rather than a mesh rewrite. This is the direction the commented-out line in `LandmarksLayerModule.PositionBuilding` was reaching for. Note the constraint this creates: anything that positions individual buildings by writing a local Y on them relies on the tile root's Y scale being exactly 1, so that a child's local Y is world Y. Putting a scale there breaks that, and both would have to move together.
- **Regenerate on scale change.** Straightforward and correct, but it means re-running glTF instantiation for every visible landmark on every zoom step, which is not viable at interaction rates.

Related and worth deciding at the same time: landmark geometry uses `metres / (Scale · cos φ)` while the terrain surface uses `metres / Scale`. A landmark therefore stands correctly *on* the ground but is internally taller per metre than the relief it sits on — about 30% at latitude 40, 2× at latitude 60. Vector buildings have the identical split (`HeightModifier` cos-corrected, `SnapTerrainModifier` not), so this is an SDK-wide convention question rather than a landmarks bug. Whichever way it is settled, both paths should be settled together.

---

### Selection cannot go below a whole landmark

`LandmarkHighlightBehaviour` is now on the `Map` GameObject in `LandmarkDemo`, and a raycast hit can be resolved to a landmark id: `LandmarksLayerModule.BuildingLoaded` carries the id the tile publishes for each mesh, `LandmarkComponent.LandmarkId` stamps it onto the GameObject as it loads, and `LandmarkHighlightBehaviour.SelectedLandmarkId` exposes whichever one is currently highlighted. What remains is picking a *part* of a landmark rather than the whole thing.

Feature ids are already present at the granularity selection would need. `FeatureIdColorTable` decodes them per vertex from the tile's `_FEATURE_ID_RGBA4444` buffer view, and `FeatureIdMeshWriter.Apply` writes them onto each converted mesh as a Color vertex stream — one packed id per vertex, per primitive, already consumed by the shader through `SubpartIdTypeSelector` to vary per-part appearance. Nothing, however, maps a raycast hit back to one of those ids, so every hit today resolves to "this landmark" and never to "this wall of this landmark."

The real obstacle is the collider, not the data. Recovering a feature id from a hit needs `RaycastHit.triangleIndex` against the exact triangle that was hit, read back against the same vertex buffer `FeatureIdMeshWriter` wrote into — which only works if the collider *is* the render mesh. `AddCollidersForSelection` cooks a `MeshCollider` for exactly this reason, but `UseConvexColliders` (on by default, and the only setting that keeps landmark loading affordable) replaces the real geometry with a convex hull before cooking, and a convex hull's `triangleIndex` values do not correspond to the render mesh's triangles at all. Per-part selection would mean giving up the convex-hull collider — and paying its cost, per landmark, everywhere selection is wanted — or finding a way to resolve a hit point to a feature id without going through `triangleIndex`.

---

### Generation is cancellable only when a tile's data is evicted

`GltfVisualizer` keeps a `CancellationTokenSource` per in-flight tile (`_activeLoads`, a dictionary now rather than the bare `HashSet<CanonicalTileId>` it used to be) and threads the token through both `gltf.Load` and `gltf.InstantiateSceneAsync`. A cancelled load unwinds through a caught `OperationCanceledException`, releases the import and the partially-built root it had created, and reports `TaskResultType.Cancelled` — which `LandmarksLayerModule.GenerateTask`'s result callback returns on before it would ever reach `RecordGenerationFailure`, so a cancelled load is never counted as a failed attempt; the tile is simply left unresolved and regenerated if it comes back into the cover. `GltfVisualizer.Cancel(CanonicalTileId)` is called from exactly two places: `LandmarksLayerModule.ClearDisposedDataVisual`, when a tile's data is evicted from the cache while its load is still running, and `GltfVisualizer.OnDestroy`, which cancels everything still in flight on teardown.

What is deliberately **not** cancelled is a tile merely dropping out of the retained set. `LoadTileCoverToMemory` (the path behind `GetTileCoverCoroutines`) legitimately generates tiles that are not in `_retainedBuildingTiles` — that is how the flight controller's own preloading gets a tile ready before the camera arrives at it — so cancelling generation the moment a tile leaves the retained set would abort that preloading rather than avoid wasted work. This is a deliberate scope boundary, not an oversight: a glTF load already under way for a tile that is un-retained but not evicted runs to completion and reports a tile the map may not currently want, and the module caches its visual until the next sweep either reuses or releases it. Wasted work rather than incorrect output, and most visible during fast zoom sweeps where the cover churns — the `LandmarkTour` flight is exactly that pattern.

---

### The overlap test is approximate at the edges

Both connectors decide whether a basic building is hidden by `MapboxGeometryUtilities.PolygonsIntersect`, and two of its edge cases are now pinned down by `MapboxGeometryUtilitiesTests` rather than left to be rediscovered. Neither is wrong enough to be visible today, and both matter if the algorithm is revisited.

- **Two polygons sharing exactly one edge report as intersecting, but by accident.** The shared edge is exactly collinear, and `LineSegmentsIntersect` rejects parallel segments outright, so that pair contributes nothing. What actually returns true is a different pair of edges meeting at the shared corner, because the segment test's parameter ranges include their endpoints. Buildings abutting a landmark footprint - very common - are therefore suppressed on a coincidence rather than on a rule.

- **`IsPointInPolygon` uses the standard half-open ray-cast convention**, so a polygon's own vertex and one of the two edges meeting at it count as inside, while the opposite edge counts as outside. Asymmetric, conventional, and fine for area tests; worth knowing before it is used for anything that cares about the boundary itself.

`PolygonsIntersect` also tests only the first vertex of each polygon for containment before walking edges, so a ring wholly contained inside another whose first vertex happens to fall in a hole is missed. Its doc comment states the other precondition explicitly: callers must have already passed a bounding-box test.

---

### Optimizations not taken

- **No GPU instancing.** `LandmarkMaterialGLTF` has `m_EnableInstancingVariants: 0`. Landmarks are distinct meshes so instancing cannot merge draw calls between them, but it also rules out the property-block batching path, which matters if per-landmark tinting is ever used at scale.
- **Colliders could use the footprint mesh.** Collider cooking is opt-in and defaults to a convex hull, which is cheap. The already-extracted planar footprint would be cheaper still, but it is not reachable from `LandmarkHighlightBehaviour` today — it arrives on the module's `TileFinished`, keyed by tile, while the footprint GameObjects are deactivated children.
- **`FlipYZ` round-trips managed arrays.** It reads `mesh.vertices` and `mesh.normals` (a full copy each), rewrites them, assigns both back, and calls `RecalculateBounds` — once per primitive, on the load path. Going through the mesh data API, or folding the transform into what glTFast already applies, would remove two array allocations per mesh.

---

### Settled: the snapper's z14 elevation query is correct at every render zoom

Recorded because it looks wrong and is not. `LandmarkTerrainSnapper` samples elevation using the **z14** landmark tile id, and `TryQueryElevation` only ever walks *upward* from the id it is given. Terrain data tiles are normally `render zoom - 2`, which at render z17+ would be deeper than z14 and so never consulted — landmarks would silently stay at y=0 at exactly the zooms they are most visible.

That does not happen because `TerrainLayerModuleScript` sets the terrain source's `ClampDataLevelToMax` to 14, and `TerrainLayerModule.GetDataId` returns `tileId.ParentAt(maxZoom)` once `render - 2` reaches it. Terrain data is therefore never deeper than z14 at any render zoom, and the upward walk always reaches it (at render z14 the data is z12, two steps up, well inside the 5-step limit).

The dependency is worth knowing: **raising the terrain source's `ClampDataLevelToMax` above 14 would silently break landmark snapping.** If that ever changes, the snapper must sample with a tile id at or below the terrain data zoom, with UVs remapped into it, rather than with the z14 landmark id.

Note also that the "move the divisor out of the vertices" option above is constrained by this class: it writes each building's local Y assuming the tile root's Y scale is exactly 1.

---

### Zoom range

`LandmarkToBuildingComponentConnector` suppresses buildings at any render zoom, but the Components module itself is capped: `MapboxComponentsModuleScript.RejectTilesOutsideZoom` defaults to `(12, 16)` and `ClampDataLevelToMax` to 15. Landmarks will not suppress buildings above z16 until those are raised, because no buildings are produced up there to suppress. The code carries no depth assumption; the settings do.
