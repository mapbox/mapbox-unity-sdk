## Landmarks Module

The Landmarks Module adds detailed 3D landmark buildings to your map. These are high-quality, architect-modeled buildings with PBR materials — significantly more detailed than the basic extruded buildings produced by the Vector or Components modules.

When landmarks are enabled, the SDK automatically hides any basic OSM buildings that overlap with a landmark, so you don't get double geometry.

![Landmark buildings on the map](Images/LandmarksModule.png)

---

### Quick Start

The easiest way to get started is to import the demo scene. Open the **Package Manager** window (`Window → Package Manager`), find the **Mapbox Unity SDK** package, and go to the **Samples** tab. Click **Import** next to the Landmark Demo — this gives you a fully configured scene you can open and play immediately.

To add landmarks to your own scene, follow the steps below.

---

### Setting Up Landmarks

**Prerequisites:** A working map with `MapboxMapBehaviour` and at least one building module (Components Module or Vector Module). Zoom level must be 14 or higher.

Landmarks are delivered as compressed glTF, so the SDK depends on **glTFast** (`com.unity.cloud.gltfast`) and **meshoptimizer decompression** (`com.unity.meshopt.decompress`). The Package Manager installs both automatically — no action needed. If you are on Unity 6 you may raise the glTFast version in your own project manifest; the module is built against APIs that are unchanged in later releases.

#### Step 1: Add the Landmarks Module

1. Select your map GameObject (the one with `MapboxMapBehaviour`)
2. Add the `LandmarksLayerModuleScript` component
3. Assign a **Base Material** — this is the PBR material used for all landmark models. The default is `LandmarkMaterialGLTF.mat` included in the package under `LandmarksModule/Materials/`

That's it — landmarks will now appear on the map. But you'll likely see basic OSM buildings overlapping with the landmarks. The next steps fix that.

#### Step 2: Suppress Overlapping Buildings

You need one connector for each building module you're using. Most projects use either the Components Module or the Vector Module for buildings, not both — so you typically only need one connector.

**If you're using the Components Module for buildings:**

1. Add `LandmarkToBuildingComponentConnector` to your map GameObject
2. Assign:
   - **Map** → your `MapBehaviourCore`
   - **LandmarksLayerModuleScript** → the landmarks module you added in Step 1
   - **BuildingLayerVisualizerObject** → the `BuildingComponentVisualizerObject` asset your Components Module uses

**If you're using the Vector Module for buildings:**

1. Create a filter asset: right-click in Project → `Create → Mapbox → Filters → Landmark Polygon Filter`
2. Open your vector building layer's filter stack asset and add the new filter to its **Filters** list
3. Add `LandmarkToVectorConnector` to your map GameObject
4. Assign:
   - **Map** → your `MapBehaviourCore`
   - **LandmarksLayerModuleScript** → the landmarks module you added in Step 1
   - **VectorModuleScript** → your vector module
   - **LandmarkFilter** → the filter asset you created in step 1

#### Step 3: Play

Enter Play Mode. Landmark buildings appear as detailed 3D models. Basic OSM buildings at the same locations are automatically suppressed.

---

### How the Overlap Suppression Works

The two connectors use different strategies based on how each building module works:

- **Components connector** — The Components Module bakes all buildings into a single merged mesh per tile, so individual buildings can't be skipped during generation. Instead, the connector detects overlapping buildings after they're generated and hides them by zeroing their vertices.

- **Vector connector** — The Vector Module processes buildings one at a time through a filter stack before generating meshes. The connector injects landmark footprint polygons into this filter stack, so overlapping buildings are simply never created. This is more efficient since no geometry is wasted.

Both connectors handle the case where landmarks and buildings load in either order. If buildings load first, they're checked (or reprocessed) when landmarks arrive. If landmarks load first, the data is ready when buildings are processed.

---

### Troubleshooting

**Landmarks don't appear:**
- Confirm `LandmarksLayerModuleScript` is on the same GameObject as `MapboxMapBehaviour`
- Check that **Base Material** is assigned
- Make sure the map zoom level is 14 or higher
- Check the Console for errors

**Basic buildings still showing under landmarks:**
- Make sure you added the correct connector for your building module
- For the Vector connector: confirm the `LandmarkPolygonFilterObject` asset is in the vector building layer's filter stack
- For the Components connector: confirm the `BuildingComponentVisualizerObject` is assigned

---

### The Landmark Tour Sample

`LandmarkTour` is a third sample that shows the landmark set rather than a single landmark. It opens looking straight down on continental Europe and flies the camera to a chosen landmark when you press one of the buttons on the left. Satellite imagery supplies the ground, vector buildings and landmarks fill in close up, and there is no terrain module.

Four things in that scene are worth understanding before you copy the pattern.

**Module order decides whether a tile can show its texture.** `MapboxMapVisualizer.CreateTile` asks each layer module in turn whether it is ready and **breaks on the first one that says no**, so a module earlier in the list can stop a later one from running at all. `StaticApiLayerModule.LoadInstant` is the only code that binds a tile's own imagery, so if it sits behind a module that is still fetching, the tile shows a placeholder for as long as that module is busy. Modules are collected with `GetComponents`, which returns inspector order — so in this scene the imagery module is deliberately placed **above** the vector and landmark modules. This ordering is invisible in the Inspector and easy to lose by adding a component; if a scene ever goes uniformly flat-coloured while loading, check it first.

**The Components module's zoom range is a trade.** `RejectTilesOutsideZoom` is `(12, 16)` here, its shipped default. `VectorLayerModule.LoadInstant` short-circuits to "ready" for tiles outside that range, so below zoom 12 the module is transparent and never holds a tile up. Widening it to draw vector content at continental zooms is possible — that is what `LandmarkTourComponentsModuleScript` exists for — but it turns the module into a gate across every zoom in the range, and Mapbox Streets has little to offer below zoom 12 beyond water and landuse. Widen it only if you actually need what it draws there.

**Flights step through zoom and wait for tiles.** `MapFlightController` climbs, cruises and descends, but the climb and descent advance in steps of `ZoomLevelsPerStep` and wait up to `TileWaitTimeout` after each one for that level's tiles to arrive, via `MapboxMap.LoadTileCoverToMemory`. A continuous sweep does not work: the tile cover changes at every integer zoom crossing, while the fetch pipeline holds a queued request for 0.2 s before it may start and then starts only one per frame, so a fast sweep has each level's requests aborted at roughly the moment they become eligible. Nothing lands, and the placeholder falls back to the pinned zoom 0–2 world tiles, which magnified that far is a single flat colour. Stepping keeps detail within `ZoomLevelsPerStep` of the camera — at the default 2 that is about 256 source pixels across the view, which still reads as sharp. A typical flight is 2 to 3 seconds when the levels are cached and up to 7 or 8 when every step has to wait. `TileWaitTimeout: 0` restores the old racing behaviour if you want to see the difference.

**The camera is never moved directly.** Every frame the flight pushes an interpolated view into `MapboxMap.ChangeView`, and `Moving3dCamera` follows because it subscribes to the map information's view events when it initializes. The flight disables `Moving3dCameraBehaviour` for its duration to keep user input out of the way, and writes the arrival zoom back into `Core.ZoomValue` afterwards so the first scroll does not jump. `MapCameraClipPlaneDriver` scales the near and far planes with the camera's distance from the map, which a scene spanning zoom 4 to 17 needs — and it runs off `ViewChanged` rather than `Update`, because the tile provider culls against the far plane and `ViewChanged` fires before the map redraws.

Landmarks only load at zoom 14 and above, so every destination uses 17 or higher.

---

### Further Reading

For technical details on the collision detection algorithms, coordinate space transforms, tile edge handling, event lifecycle, and architecture, see [Landmarks Module Internals](LandmarksModuleInternals.md).

For what the module does not do yet — including the fact that **dynamic map scaling is unsupported**, and that click-to-select ships as code but is not wired into any scene — see [Known Gaps](LandmarksModuleKnownGaps.md).
