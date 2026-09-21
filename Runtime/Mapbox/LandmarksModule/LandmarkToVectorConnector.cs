using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarksModule.Unity;
using Mapbox.VectorModule;
using Mapbox.VectorModule.Unity;
using UnityEngine;

namespace Mapbox.LandmarksModule
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
        [Tooltip("Map whose initialization gates the wiring below. Required - leave it unassigned and this connector does nothing.")]
        public MapBehaviourCore Map;

        [Tooltip("Landmarks module supplying the footprint polygons fed into the filter. Required.")]
        public LandmarksLayerModuleScript LandmarksLayerModuleScript;

        [Tooltip("Vector module whose building layer is reloaded when landmark footprints arrive after it has already processed a tile. Required.")]
        public VectorLayerModuleScript VectorModuleScript;

        [Tooltip("Filter asset sitting in the vector building layer's filter stack that rejects features overlapping a landmark. Required - without it landmark footprints have nowhere to go and OSM buildings will keep overlapping landmarks.")]
        public LandmarkPolygonFilterObject LandmarkFilter;

        private VectorLayerModule _vectorLayerModule;
        private LandmarksLayerModule _landmarksLayerModule;
        private readonly List<CanonicalTileId> _tilesToReload = new List<CanonicalTileId>();

        private void Awake()
        {
            if (Map == null || LandmarksLayerModuleScript == null || VectorModuleScript == null || LandmarkFilter == null)
            {
                Debug.LogWarning($"{nameof(LandmarkToVectorConnector)} needs Map, Landmarks Layer Module Script, Vector Module Script and Landmark Filter assigned. Landmark/vector-building overlap suppression is disabled.", this);
                enabled = false;
                return;
            }

            // Named method rather than a lambda so OnDestroy can detach it. As a closure it could not
            // be unsubscribed, so destroying this component before the map initialized left the body
            // to run against a dead component.
            Map.Initialized += OnMapInitialized;
        }

        private void OnMapInitialized(MapboxMap map)
        {
            _vectorLayerModule =
                (VectorLayerModule)VectorModuleScript.ModuleImplementation;

            _landmarksLayerModule = (LandmarksLayerModule)LandmarksLayerModuleScript.ModuleImplementation;
            _landmarksLayerModule.TileFinished += OnLandmarkTileFinished;
            _landmarksLayerModule.TileDisposed += OnLandmarkTileDisposed;
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
                var parentTile = readyTile.ParentAt(LandmarksLayerModule.LandmarkDataZoom);
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
            if (Map != null) Map.Initialized -= OnMapInitialized;

            if (_landmarksLayerModule != null)
            {
                _landmarksLayerModule.TileFinished -= OnLandmarkTileFinished;
                _landmarksLayerModule.TileDisposed -= OnLandmarkTileDisposed;
            }
        }
    }
}
