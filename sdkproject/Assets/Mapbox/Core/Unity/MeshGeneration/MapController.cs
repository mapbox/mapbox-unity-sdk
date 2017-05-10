namespace Mapbox.Unity.MeshGeneration
{
    using UnityEngine;
    using System.Collections.Generic;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity;
    using Mapbox.Platform;
    using Mapbox.Unity.Utilities;
    using Utils;

    /// <summary>
    /// MapController is just an helper class imitating the game/app logic controlling the map. It creates and passes the tiles requests to MapVisualization.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        public RectD ReferenceTileRect;
        public float WorldScaleFactor = 1;

        public MapVisualization MapVisualization;
        public float TileSize = 100;

        [SerializeField]
        private bool _snapYToZero = true;

        [Geocode]
        public string LatLng;
        public int Zoom;
        public Vector4 Range;

        public GameObject Root;
        private Dictionary<Vector2, UnityTile> _tiles;
        private Vector2 _refTile;


        /// <summary>
        /// Resets the map controller and initializes the map visualization
        /// </summary>
        public void Awake()
        {
            var parm = LatLng.Split(',');
            var v2 = Conversions.GeoToWorldPosition(double.Parse(parm[0]), double.Parse(parm[1]), new Vector2d(0, 0));
            _refTile = Conversions.MetersToTile(v2, Zoom);
            ReferenceTileRect = Conversions.TileBounds(_refTile, Zoom);

            MapVisualization.Initialize(MapboxAccess.Instance);
            _tiles = new Dictionary<Vector2, UnityTile>();
        }

        public void Start()
        {
            Execute();
        }

        /// <summary>
        /// Pulls the root world object to origin for ease of use/view
        /// </summary>
        public void Update()
        {
            if (_snapYToZero)
            {
                var ray = new Ray(new Vector3(0, 1000, 0), Vector3.down);
                RaycastHit rayhit;
                if (Physics.Raycast(ray, out rayhit))
                {
                    Root.transform.position = new Vector3(0, -rayhit.point.y, 0);
                    _snapYToZero = false;
                }
            }
        }

        public void Execute()
        {
            var parm = LatLng.Split(',');
            Execute(double.Parse(parm[0]), double.Parse(parm[1]), Zoom, Range);
        }

        /// <summary>
        /// World creation call used in the demos. Destroys and existing worlds and recreates another one. 
        /// </summary>
        /// <param name="lat">Latitude of the requested point</param>
        /// <param name="lng">Longitude of the requested point</param>
        /// <param name="zoom">Zoom/Detail level of the world</param>
        /// <param name="frame">Tiles to load around central tile in each direction; west-north-east-south</param>
        public void Execute(double lat, double lng, int zoom, Vector4 frame)
        {
            //frame goes left-top-right-bottom here
            if (Root != null)
            {
                foreach (Transform t in Root.transform)
                {
                    Destroy(t.gameObject);
                }
            }

            Root = new GameObject("worldRoot");
            WorldScaleFactor = (float)(TileSize / ReferenceTileRect.Size.x);
            Root.transform.localScale = Vector3.one * WorldScaleFactor;

            for (int i = (int)(_refTile.x - frame.x); i <= (_refTile.x + frame.z); i++)
            {
                for (int j = (int)(_refTile.y - frame.y); j <= (_refTile.y + frame.w); j++)
                {
                    var tile = new GameObject("Tile - " + i + " | " + j).AddComponent<UnityTile>();
                    _tiles.Add(new Vector2(i, j), tile);
                    tile.Zoom = zoom;
                    tile.RelativeScale = Conversions.GetTileScaleInMeters(0, Zoom) / Conversions.GetTileScaleInMeters((float)lat, Zoom);
                    tile.TileCoordinate = new Vector2(i, j);
                    tile.Rect = Conversions.TileBounds(tile.TileCoordinate, zoom);
                    tile.transform.position = new Vector3((float)(tile.Rect.Center.x - ReferenceTileRect.Center.x), 0, (float)(tile.Rect.Center.y - ReferenceTileRect.Center.y));
                    tile.transform.SetParent(Root.transform, false);
                    MapVisualization.ShowTile(tile);
                }
            }

            OnWorldCreated(Root);
        }

        public void Execute(double lat, double lng, int zoom, Vector2 frame)
        {
            Execute(lat, lng, zoom, new Vector4(frame.x, frame.y, frame.x, frame.y));
        }

        public void Execute(double lat, double lng, int zoom, int range)
        {
            Execute(lat, lng, zoom, new Vector4(range, range, range, range));
        }

        /// <summary>
        /// Used for loading new tiles on the existing world. Unlike Execute function, doesn't destroy the existing ones.
        /// </summary>
        /// <param name="pos">Tile coordinates of the requested tile</param>
        /// <param name="zoom">Zoom/Detail level of the requested tile</param>
        public void Request(Vector2 pos, int zoom)
        {
            if (!_tiles.ContainsKey(pos))
            {
                var tile = new GameObject("Tile - " + pos.x + " | " + pos.y).AddComponent<UnityTile>();
                _tiles.Add(pos, tile);
                tile.transform.SetParent(Root.transform, false);
                tile.Zoom = zoom;
                tile.TileCoordinate = new Vector2(pos.x, pos.y);
                tile.Rect = Conversions.TileBounds(tile.TileCoordinate, zoom);
                tile.RelativeScale = Conversions.GetTileScaleInMeters(0, Zoom) /
                    Conversions.GetTileScaleInMeters((float)Conversions.MetersToLatLon(tile.Rect.Center).x, Zoom);
                tile.transform.localPosition = new Vector3((float)(tile.Rect.Center.x - ReferenceTileRect.Center.x),
                                                           0,
                                                           (float)(tile.Rect.Center.y - ReferenceTileRect.Center.y));
                MapVisualization.ShowTile(tile);
            }
        }


        public delegate void MapControllerEventArgs(MapController sender, GameObject root);
        public event MapControllerEventArgs WorldCreated;
        protected virtual void OnWorldCreated(GameObject root)
        {
            var handler = WorldCreated;
            if (handler != null) handler(this, root);
        }
    }
}