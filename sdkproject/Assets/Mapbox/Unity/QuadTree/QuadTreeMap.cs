using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Unity.Collections;
using UnityEngine;

namespace Mapbox.Unity.QuadTree
{
    public class QuadTreeMap : MapCore
    {
        public QuadTreeCameraController QuadTreeCameraController;
        public QuadTreeGenerator QuadCameraSettings;
        public AbstractMapVisualizer MapVisualizer => _mapVisualizer;
        public float WorldScale = 1000f;

        private Camera _camera;

        public override void Start()
        {
            base.Start();
            _camera = Camera.main;
            QuadTreeCameraController.Initialize(WorldScale, _camera, this);
            QuadCameraSettings.Initialize(WorldScale, _camera, this);

            var rect = QuadCameraSettings.UpdateQuadTree();
            RequestTiles(rect);
            UpdateTilePositions(rect);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            var viewChanged = QuadTreeCameraController.UpdateCamera();
            if (viewChanged || _isDirty)
            {
                RedrawMap();
            }

            // var elevationUnity = 0f;
            // if (_centerRect != null && Map.MapVisualizer.ActiveTiles.ContainsKey(_centerRect.Id))
            // {
            //     var centerTile = Map.MapVisualizer.ActiveTiles[_centerRect.Id];
            //     var _meters = Map.CenterMercator;
            //     var _rect = centerTile.Rect;
            //     if (centerTile != null)
            //     {
            //         var centerElevation= centerTile.QueryHeightData((float)((_meters - _rect.Min).x / _rect.Size.x), (float)((_meters.y - _rect.Max.y) / _rect.Size.y));
            //         //var centerElevation = centerTile.QueryHeightData(pointInTile);
            //         //var elevationMeters = Map.QueryElevationInMetersAt(Map.CenterLatitudeLongitude);
            //         elevationUnity = centerElevation * GetWorldScale();
            //     }
            // }
            // _camera.transform.position += new Vector3(0, elevationUnity,0);
        }

        private void RedrawMap()
        {
            var rect = QuadCameraSettings.UpdateQuadTree();
            RequestTiles(rect);
            UpdateTilePositions(rect);
        }

        private void UpdateTilePositions(QuadTreeView view)
        {
            foreach (var mapVisualizerActiveTile in _mapVisualizer.ActiveTiles)
            {
                var tile = mapVisualizerActiveTile.Value;
                var rect = view.Tiles[tile.UnwrappedTileId];
                tile.transform.localPosition = rect.Center;
            }
        }

        private void RequestTiles(QuadTreeView view)
        {
            var disposeList = new List<UnwrappedTileId>();
            foreach (var tilePair in _mapVisualizer.ActiveTiles)
            {
                if (!view.Tiles.ContainsKey(tilePair.Key))
                {
                    disposeList.Add(tilePair.Key);
                }
            }

            foreach (var id in disposeList)
            {
                _mapVisualizer.DisposeTile(id);
            }

            foreach (var rect in view.Tiles)
            {
                foreach (var edge in rect.Value.Edges())
                {
                    Debug.DrawLine(edge.Item1, edge.Item2);
                }

                if (!_mapVisualizer.ActiveTiles.ContainsKey(rect.Key))
                {
                    var tile = _mapVisualizer.LoadTile(rect.Key);
                    if (tile != null)
                    {
                        tile.transform.position = rect.Value.Center;
                        tile.transform.SetParent(transform);
                        tile.transform.localScale = Vector3.one * rect.Value.UnityBounds.size.x / 100;
                        tile.MeshRenderer.sharedMaterial.SetFloat("_ObjectScale", rect.Value.UnityBounds.size.x / 100);
                        tile.gameObject.SetActive(true);
                    }
                }
            }

            if (view.CenterRect != null)
            {
                foreach (var edge in view.CenterRect.Edges())
                {
                    Debug.DrawLine(edge.Item1, edge.Item2, Color.red);
                }
            }
        }
    }

    public class MapCore : MonoBehaviour, IMap
    {
        //not used in new system but reqiured because IMap
        public float WorldRelativeScale { get; }
        public float UnityTileSize => _options.scalingOptions.unityTileSize;

        protected Vector2d _centerMercator;
        protected Vector2d _centerLatitudeLongitude;
        protected bool _isDirty = false;

        public Vector2d CenterLatitudeLongitude => _centerLatitudeLongitude;
        public Vector2d CenterMercator => Conversions.LatLonToMeters(_centerLatitudeLongitude);

        [SerializeField] protected AbstractMapVisualizer _mapVisualizer;
        [SerializeField] private MapOptions _options = new MapOptions();
        public MapOptions Options
        {
            get => _options;
            set => _options = value;
        }

        public int InitialZoom { get; set; }
        public float Zoom => Options.locationOptions.zoom;
        public int AbsoluteZoom => (int)Math.Floor(Options.locationOptions.zoom);

        public Transform Root => transform;
        public Material TileMaterial => _options.tileMaterial;

        public virtual void Start()
        {
            Options.locationOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
            {
                UpdateMap(Conversions.StringToLatLon(_options.locationOptions.latitudeLongitude), _options.locationOptions.zoom);
            };

            _centerLatitudeLongitude = Conversions.StringToLatLon(_options.locationOptions.latitudeLongitude);
            InitialZoom = (int)_options.locationOptions.zoom;
            _mapVisualizer.Initialize(this);
        }

        #region SET STUFF
        public void SetZoom(float zoom)
        {
            Options.locationOptions.zoom = zoom;
        }
        public void SetWorldRelativeScale(float scale)
        {
            throw new NotImplementedException();
        }
        public virtual void SetCenterMercator(Vector2d centerMercator)
        {
            _centerMercator = centerMercator;
        }
        public virtual void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
        {
            _options.locationOptions.latitudeLongitude = string.Format("{0}, {1}", centerLatitudeLongitude.x.ToString(CultureInfo.InvariantCulture), centerLatitudeLongitude.y.ToString(CultureInfo.InvariantCulture));
            _centerLatitudeLongitude = centerLatitudeLongitude;
        }
        #endregion

        public Vector2d WorldToGeoPosition(Vector3 realworldPoint)
        {
            throw new NotImplementedException();
        }
        public Vector3 GeoToWorldPosition(Vector2d latitudeLongitude, bool queryHeight = true)
        {
            throw new NotImplementedException();
        }

        public void UpdateMap(Vector2d latLon, float zoom)
        {
            SetCenterLatitudeLongitude(latLon);
            SetZoom(zoom);
            _isDirty = true;
        }

        public void ResetMap()
        {
            throw new NotImplementedException();
        }

        public bool IsAccessTokenValid
        {
            get
            {
                bool isAccessTokenValid = false;
                try
                {
                    var accessTokenCheck = MapboxAccess.Instance;
                    if (MapboxAccess.Instance.Configuration == null || string.IsNullOrEmpty(MapboxAccess.Instance.Configuration.AccessToken))
                    {
                        return false;
                    }

                    isAccessTokenValid = true;
                }
                catch (System.Exception)
                {
                    isAccessTokenValid = false;
                }
                return isAccessTokenValid;
            }
        }

        public event Action OnInitialized;
        public event Action OnUpdated;

    }

    public class UnityRectD
    {
        public UnwrappedTileId Id;
        public RectD TileBounds;
        public Vector3 BottomLeft;
        public Vector3 BottomRight;
        public Vector3 TopLeft;
        public Vector3 TopRight;
        public Vector3 Center;
        public Bounds UnityBounds;
        public float DistanceToCamera;

        private Vector2d _offset;
        private float _worldScale;

        public bool OneCornerInView(Camera camera)
        {
            if(IsPointInView(camera, BottomLeft)) return true;
            if(IsPointInView(camera, BottomRight)) return true;
            if(IsPointInView(camera, TopLeft)) return true;
            if(IsPointInView(camera, TopRight)) return true;
            return false;
        }

        private bool IsPointInView(Camera camera, Vector3 point)
        {
            var viewport = camera.WorldToViewportPoint(point);
            return Vector3.Dot(camera.transform.forward, point) < 0 &&
                   viewport.x > 0 &&
                   viewport.x < 1 &&
                   viewport.y > 0 &&
                   viewport.y < 1;
        }

        private void UnitySphereSpaceCalculations(RectD bounds, float radius)
        {
            TopLeft = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x, bounds.TopLeft.y)), radius);
            TopRight = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x + bounds.Size.x, bounds.TopLeft.y)), radius);
            BottomLeft = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x, bounds.TopLeft.y - bounds.Size.y)), radius);
            BottomRight = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x + bounds.Size.x, bounds.TopLeft.y - bounds.Size.y)), radius);
            Center = (BottomLeft + TopRight) / 2;
            //UnityBounds = new Bounds(new Vector3((float) Center.x, 0, (float) Center.y), new Vector3(Mathf.Abs((float) bounds.Size.x), 0, Mathf.Abs((float) bounds.Size.x)));
            UnityBounds = GeometryUtility.CalculateBounds(new[]
            {
                TopLeft, TopRight, BottomLeft, BottomRight
            }, Matrix4x4.identity);
        }

        private void UnityFlatSpaceCalculations(RectD bounds, float worldScale)
        {
            TopLeft = ((bounds.TopLeft) / worldScale).ToVector3xz();
            TopRight = ((new Vector2d(bounds.TopLeft.x + bounds.Size.x, bounds.TopLeft.y)) / worldScale).ToVector3xz();
            BottomLeft = ((new Vector2d(bounds.TopLeft.x, bounds.TopLeft.y - bounds.Size.y)) / worldScale).ToVector3xz();
            BottomRight = ((new Vector2d(bounds.TopLeft.x + bounds.Size.x, bounds.TopLeft.y - bounds.Size.y)) / worldScale).ToVector3xz();
            Center = (BottomLeft + TopRight) / 2;
            //UnityBounds = new Bounds(new Vector3((float) Center.x, 0, (float) Center.y), new Vector3(Mathf.Abs((float) bounds.Size.x), 0, Mathf.Abs((float) bounds.Size.x)));
            UnityBounds = GeometryUtility.CalculateBounds(new[]
            {
                TopLeft, TopRight, BottomLeft, BottomRight
            }, Matrix4x4.identity);
        }

        private Vector3 LatLngToSphere(Vector2d ll, float radius = 100)
        {
            var latitude = (float) (Mathf.Deg2Rad * ll.x);
            var longitude = (float) (Mathf.Deg2Rad * ll.y);

            float xPos = (radius) * Mathf.Cos(latitude) * Mathf.Cos(longitude);
            float zPos = (radius) * Mathf.Cos(latitude) * Mathf.Sin(longitude);
            float yPos = (radius) * Mathf.Sin(latitude);

            return new Vector3(xPos, yPos, zPos);
        }

        public UnityRectD(UnwrappedTileId id, Vector2d vector2d, float worldScale)
        {
            _offset = vector2d;
            _worldScale = worldScale;
            Id = id;
            var bound = Conversions.TileBounds(Id);
            TileBounds = new RectD(new Vector2d(bound.TopLeft.x + vector2d.x, bound.TopLeft.y + vector2d.y), bound.Size);
            //UnitySphereSpaceCalculations(TileBounds, worldScale);
            UnityFlatSpaceCalculations(TileBounds, worldScale);
        }

        public List<Tuple<Vector3, Vector3>> Edges()
        {
            return new List<Tuple<Vector3, Vector3>>()
            {
                new Tuple<Vector3, Vector3>(BottomLeft, BottomRight),
                new Tuple<Vector3, Vector3>(BottomLeft, TopLeft),
                new Tuple<Vector3, Vector3>(TopLeft, TopRight),
                new Tuple<Vector3, Vector3>(BottomRight, TopRight),
            };
        }

        public UnityRectD Quadrant(int i)
        {
            var childX  = (Id.X << 1) + (i % 2);
            var childY  = (Id.Y << 1) + (i >> 1);

            return new UnityRectD(new UnwrappedTileId(Id.Z + 1, childX, childY), _offset, _worldScale);
        }

        public bool AnyCornerTowardsCamera(Camera camera, float comp)
        {
            return Vector3.Dot(Center.normalized, camera.transform.forward) < comp ||
                   Vector3.Dot(TopLeft.normalized, camera.transform.forward) < comp ||
                   Vector3.Dot(TopRight.normalized, camera.transform.forward) < comp ||
                   Vector3.Dot(BottomLeft.normalized, camera.transform.forward) < comp ||
                   Vector3.Dot(BottomRight.normalized, camera.transform.forward) < comp;
        }

        public float GetDistanceToCamera(Camera camera)
        {
            var dist = float.MaxValue;
            var corners = new[] {TopLeft, TopRight, BottomLeft, BottomRight, Center};
            foreach (var corner in corners)
            {
                var cornerDistance = Vector3.SqrMagnitude(corner - camera.transform.position);
                if (cornerDistance < dist)
                {
                    dist = cornerDistance;
                }
            }

            return dist;
        }

        public float ShortestDistanceTo(Vector3 p)
        {
            var dx = Math.Max(0, Math.Max(UnityBounds.min.x - p.x, p.x - UnityBounds.max.x));
            var dz = Math.Max(0, Math.Max(UnityBounds.min.z - p.z, p.z - UnityBounds.max.z));
            return (float) Math.Sqrt(dx*dx + dz*dz);
        }
    }
}