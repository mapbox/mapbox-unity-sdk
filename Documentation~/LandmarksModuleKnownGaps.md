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

### Selection is implemented but wired to nothing

`LandmarkHighlightBehaviour` raycasts, tags hit meshes with `LandmarkComponent`, and tints the hit building. It appears in **no scene** — not `LandmarkDemo`, `LandmarkDemoWithVector`, or `LandmarkTour` — so it has never been exercised. Anyone wanting click-to-select today has to add the component themselves and discover the wiring from source.

Missing to make it usable:

- A sample that demonstrates it.
- **Per-part selection.** Feature IDs are already decoded per-vertex (`FeatureIdColorTable` → `FeatureIdMeshWriter`) and consumed by the shader through `SubpartIdTypeSelector`, but only to pick per-part appearance. Nothing maps a raycast hit back to a feature id, so selection is whole-mesh only even though the data for finer granularity is present in the vertex stream.
- **A way to ask what was clicked.** The tile publishes a landmark id (`mapbox:footprint:id` on the footprint node, `id` on the building node). `LandmarkAnchor.LandmarkId` captures it, but nothing surfaces it, so "which landmark is this" is unanswerable from a hit.

---

### Generation is not cancellable

`GltfVisualizer.Generate` has no cancellation. When `RetainTiles` drops a tile, the vector and pbf sources cancel their in-flight fetches, but a glTF load already under way runs to completion and reports a tile the map no longer wants; the module caches a visual for it until the next sweep releases it. Wasted work rather than incorrect output, and most visible during fast zoom sweeps where the cover churns — the `LandmarkTour` flight is exactly that pattern.

Doing this properly needs a real task handle stored per tile and an un-retain hook to trigger it. `_activeTasks` is a `HashSet<CanonicalTileId>` precisely so it does not imply otherwise.

---

### Optimizations not taken

- **No GPU instancing.** `LandmarkMaterialGLTF` has `m_EnableInstancingVariants: 0`. Landmarks are distinct meshes so instancing cannot merge draw calls between them, but it also rules out the property-block batching path, which matters if per-landmark tinting is ever used at scale.
- **`FlipYZ` round-trips managed arrays.** It reads `mesh.vertices` and `mesh.normals` (a full copy each), rewrites them, assigns both back, and calls `RecalculateBounds` — once per primitive, on the load path. Going through the mesh data API, or folding the transform into what glTFast already applies, would remove two array allocations per mesh.
- **Colliders could use the footprint mesh.** Collider cooking is now opt-in and defaults to a convex hull, which is cheap. Using the already-extracted planar footprint instead would be cheaper still, but the footprint geometry is not reachable from `LandmarkHighlightBehaviour` today — it arrives on the module's `TileFinished`, keyed by tile, while the footprint GameObjects are deactivated children.

---

### Zoom range

`LandmarkToBuildingComponentConnector` suppresses buildings at any render zoom, but the Components module itself is capped: `MapboxComponentsModuleScript.RejectTilesOutsideZoom` defaults to `(12, 16)` and `ClampDataLevelToMax` to 15. Landmarks will not suppress buildings above z16 until those are raised, because no buildings are produced up there to suppress. The code carries no depth assumption; the settings do.
