using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarkModule.Unity;
using Mapbox.VectorModule;
using Mapbox.VectorModule.Unity;
using UnityEngine;

namespace Mapbox.LandmarkModule
{
    /// <summary>
    /// Connects the LandmarksModule to the VectorModule to prevent overlapping OSM buildings.
    ///
    /// PROBLEM:
    /// Three systems create buildings on the world map:
    ///   1. LandmarksModule — detailed 3D glTF landmark buildings
    ///   2. Components module (BuildingComponentVisualizer) — basic OSM buildings
    ///   3. Vector module (VectorLayerVisualizer) — basic OSM buildings via filter/mesh/GO pipeline
    /// When a landmark exists, basic OSM buildings at the same location must be suppressed.
    ///
    /// COMPONENTS SOLUTION (existing, see LandmarkToBuildingComponentConnector):
    /// Listens to both landmark and building component events, detects polygon intersections,
    /// and hides overlapping buildings by zeroing their mesh vertices post-generation.
    /// Works asynchronously — handles buildings arriving before or after landmarks.
    ///
    /// VECTOR SOLUTION (this script):
    /// Instead of post-generation vertex hiding, this connector feeds landmark footprint polygons
    /// into a LandmarkPolygonFilter that sits in the vector building layer's filter stack.
    /// When the vector module processes features, the filter's Try() method rejects any feature
    /// whose geometry intersects a landmark polygon — preventing mesh generation entirely.
    /// This is possible because the vector module evaluates ILayerFeatureFilterComparer.Try()
    /// on every feature before generating meshes (see VectorLayerVisualizer.MeshModifications).
    ///
    /// TIMING:
    /// Landmarks are expected to load before vector tiles. When landmark footprints arrive via the
    /// TileFinished event, the filter is populated immediately. If the vector module has already
    /// processed tiles in the same area (race condition), VectorLayerModule.ReloadTile() is called
    /// to re-run the pipeline with the now-populated filter.
    ///
    /// COORDINATE SPACES:
    /// Landmark polygons are stored at z14 tile space inside the filter. During Try(), each
    /// vector feature's geometry is transformed to z14 space using CalculateTopLeftScaleOffsetAtZoom.
    /// This is cheap (a few multiply-adds per vertex for the bbox check) and avoids duplicating
    /// polygon data across zoom levels. Works at any zoom level (z15, z16, z20, etc.).
    /// </summary>
    public class LandmarkToVectorConnector : MonoBehaviour
    {
        public MapBehaviourCore Map;
        public LandmarksLayerModuleScript LandmarksLayerModuleScript;
        public VectorLayerModuleScript VectorModuleScript;
        public LandmarkPolygonFilterObject LandmarkFilter;

        private VectorLayerModule _vectorLayerModule;
        private LandmarksLayerModule _landmarksLayerModule;
        private readonly List<CanonicalTileId> _tilesToReload = new List<CanonicalTileId>();

        private void Awake()
        {
            Map.Initialized += _ =>
            {
                _vectorLayerModule =
                    (VectorLayerModule)VectorModuleScript.ModuleImplementation;

                _landmarksLayerModule = (LandmarksLayerModule)LandmarksLayerModuleScript.ModuleImplementation;
                _landmarksLayerModule.TileFinished += OnLandmarkTileFinished;
                _landmarksLayerModule.TileDisposed += OnLandmarkTileDisposed;
            };
        }

        private void OnLandmarkTileFinished(CanonicalTileId z14TileId, List<Vector3[]> polygons)
        {
            // Feed landmark polygons into the filter (stored at z14, with neighbor replication)
            LandmarkFilter.AddLandmarkPolygons(z14TileId, polygons);

            // Check if the vector module has already processed tiles in this z14 area.
            // If so, reload them so the filter can reject overlapping features.
            if (_vectorLayerModule == null) return;

            // Snapshot the ready tiles to avoid modifying the collection during iteration
            // (ReloadTile may alter the ready tiles set internally)
            _tilesToReload.Clear();
            foreach (var readyTile in _vectorLayerModule.GetReadyTiles())
            {
                // IMPORTANT: ParentAt() mutates the struct in place.
                // Create an explicit copy to avoid corrupting the iteration variable.
                var parentTile = new CanonicalTileId(readyTile.Z, readyTile.X, readyTile.Y)
                    .ParentAt(14);
                if (parentTile.Equals(z14TileId))
                {
                    _tilesToReload.Add(readyTile);
                }
            }

            foreach (var tile in _tilesToReload)
            {
                _vectorLayerModule.ReloadTile(tile);
            }
        }

        private void OnLandmarkTileDisposed(CanonicalTileId z14TileId)
        {
            LandmarkFilter.RemoveLandmarkPolygonsFromSource(z14TileId);
        }

        private void OnDestroy()
        {
            if (_landmarksLayerModule != null)
            {
                _landmarksLayerModule.TileFinished -= OnLandmarkTileFinished;
                _landmarksLayerModule.TileDisposed -= OnLandmarkTileDisposed;
            }
        }
    }
}
