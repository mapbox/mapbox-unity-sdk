using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Mapbox.Unity.QuadTree
{
    public class QuadTreeMap : MapCore
    {
        public QuadTreeCameraController CameraController;
        public QuadTreeGenerator QuadTreeGenerator;
        public AbstractMapVisualizer MapVisualizer => _mapVisualizer;
        public float WorldScale = 1000f;

        //private Dictionary<UnityTile, UnityRectD> _tiles = new Dictionary<UnityTile, UnityRectD>();
        private QuadTreeView _currentView;

        private Camera _camera;

        public override void Start()
        {
            base.Start();
            _camera = Camera.main;
            CameraController.Initialize(WorldScale, _camera, this);
            QuadTreeGenerator.Initialize(WorldScale, _camera, this);

            // _mapVisualizer.OnTileFinished -= OnMapVisualizerOnOnTileFinished;
            // _mapVisualizer.OnTileDisposing -= OnMapVisualizerOnOnTileDisposing;
            // _mapVisualizer.OnTileFinished += OnMapVisualizerOnOnTileFinished;
            // _mapVisualizer.OnTileDisposing += OnMapVisualizerOnOnTileDisposing;

            _isDirty = true;
        }

        private void OnMapVisualizerOnOnTileDisposing(UnityTile t)
        {
            // OnTileDisposing(t);
            // if (ZoomInRelations.ContainsKey(t.UnwrappedTileId))
            // {
            //     var parent = ZoomInRelations[t.UnwrappedTileId];
            //     if (ZoomInTracker.ContainsKey(parent))
            //     {
            //         ZoomInTracker[parent].Remove(t.UnwrappedTileId);
            //         if (ZoomInTracker[parent].Count == 0)
            //         {
            //             TileProvider_OnTileRemoved(parent);
            //             ZoomInTracker.Remove(parent);
            //         }
            //     }
            //
            //     ZoomInRelations.Remove(t.UnwrappedTileId);
            // }
        }

        private void OnMapVisualizerOnOnTileFinished(UnityTile t)
        {
            for (int i = 0; i < 4; i++)
            {
                var parent = _currentView.Tiles[t.UnwrappedTileId];
                var quad = parent.QuadrantTileId(i);
                if (_mapVisualizer.ActiveTiles.ContainsKey(quad))
                {
                    _mapVisualizer.DisposeTile(quad);
                }
            }

            t.gameObject.SetActive(true);
            if (ZoomOutTracker.ContainsKey(t.UnwrappedTileId))
            {
                foreach (var tileId in ZoomOutTracker[t.UnwrappedTileId])
                {
                    if (_mapVisualizer.ActiveTiles.ContainsKey(tileId))
                    {
                        _mapVisualizer.DisposeTile(tileId);
                        //TileProvider_OnTileRemoved(tileId);
                        //_destructionList.Remove(tileId);
                    }
                }

                ZoomOutTracker.Remove(t.UnwrappedTileId);
            }

            // if (ZoomInRelations.ContainsKey(t.UnwrappedTileId))
            // {
            //     var parent = ZoomInRelations[t.UnwrappedTileId];
            //     if (ZoomInTracker.ContainsKey(parent))
            //     {
            //         ZoomInTracker[parent].Remove(t.UnwrappedTileId);
            //         if (ZoomInTracker[parent].Count == 0)
            //         {
            //             TileProvider_OnTileRemoved(parent);
            //             ZoomInTracker.Remove(parent);
            //         }
            //     }
            //
            //     ZoomInRelations.Remove(t.UnwrappedTileId);
            // }
            //
            // OnTileFinished(t);
        }


        private void Update()
        {
            if (!Application.isPlaying)
                return;

            var viewChanged = CameraController.UpdateCamera();
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

        public void RedrawMap()
        {
            var elevationAtCenter = GetElevationAtCenter(WorldScale) * MapVisualizer.TerrainLayer.ExaggerationFactor;
            Debug.DrawLine(new Vector3(0, elevationAtCenter, 0), new Vector3(0, 100, 0), Color.red);
            _currentView = QuadTreeGenerator.UpdateQuadTree(elevationAtCenter);
            RequestTiles(_currentView);
            UpdateTilePositions(_currentView);
        }

        private float GetElevationAtCenter(float worldScale)
        {
            var meters = Conversions.LatLonToMeters(_centerLatitudeLongitude.x, _centerLatitudeLongitude.y);
            UnityTile tile = null;
            bool foundTile = MapVisualizer.ActiveTiles.TryGetValue(Conversions.LatitudeLongitudeToTileId(_centerLatitudeLongitude.x, _centerLatitudeLongitude.y, (int)Zoom), out tile);
            if (foundTile)
            {
                var rect = tile.Rect;
                return tile.QueryHeightData((float) ((meters - rect.TopLeft).x / rect.Size.x), (float) ((meters.y - rect.BottomRight.y) / rect.Size.y)) * tile.transform.localScale.x;
            }
            else
            {
                return 0f;
            }
        }

        private void UpdateTilePositions(QuadTreeView view)
        {
            foreach (var mapVisualizerActiveTile in _mapVisualizer.ActiveTiles)
            {
                var tile = mapVisualizerActiveTile.Value;
                if(view.Tiles.ContainsKey(tile.UnwrappedTileId))
                {
                    var rect = view.Tiles[tile.UnwrappedTileId];
                    tile.transform.localPosition = rect.Center;

                    tile.MeshRenderer.bounds.SetMinMax(rect.UnityBounds.min, rect.UnityBounds.max);
                }
            }

            foreach (var rectD in _quadrantsToAdd)
            {
                if (MapVisualizer.ActiveTiles.ContainsKey(rectD.Id))
                {
                    MapVisualizer.ActiveTiles[rectD.Id].transform.localPosition = rectD.Center;
                }
            }
        }

        protected Dictionary<UnwrappedTileId, HashSet<UnwrappedTileId>> ZoomOutTracker = new Dictionary<UnwrappedTileId, HashSet<UnwrappedTileId>>();
        private List<UnityRectD> _quadrantsToAdd = new List<UnityRectD>();

        private void RequestTiles(QuadTreeView view)
        {
            // var activeTiles = _mapVisualizer.ActiveTiles;
            //
            // var onHoldList = new HashSet<UnwrappedTileId>();
            // _quadrantsToAdd.Clear();


            //zoom out
            // foreach (var parentTile in view.Tiles.Values)
            // {
            //     for (int i = 0; i < 4; i++)
            //     {
            //         var quadrantId = parentTile.QuadrantTileId(i);
            //         if (activeTiles.ContainsKey(quadrantId))
            //         {
            //             var child = activeTiles[quadrantId];
            //             _mapVisualizer.StopTile(child);
            //
            //             if (!ZoomOutTracker.ContainsKey(parentTile.Id))
            //             {
            //                 ZoomOutTracker.Add(parentTile.Id, new HashSet<UnwrappedTileId>());
            //             }
            //
            //             if (ZoomOutTracker.ContainsKey(child.UnwrappedTileId))
            //             {
            //                 foreach (var subchild in ZoomOutTracker[child.UnwrappedTileId])
            //                 {
            //                     ZoomOutTracker[parentTile.Id].Add(subchild);
            //                 }
            //
            //                 ZoomOutTracker.Remove(child.UnwrappedTileId);
            //                 MapVisualizer.DisposeTile(child.UnwrappedTileId);
            //                 //REMOVE TILE TileProvider_OnTileRemoved(child);
            //             }
            //             else
            //             {
            //                 if (!ZoomOutTracker.ContainsKey(child.UnwrappedTileId))
            //                 {
            //                     onHoldList.Add(child.UnwrappedTileId);
            //                     ZoomOutTracker[parentTile.Id].Add(child.UnwrappedTileId);
            //
            //                     var quadrantTile = parentTile.Quadrant(i);
            //                     _quadrantsToAdd.Add(quadrantTile);
            //
            //                 }
            //             }
            //         }
            //     }
            // }
            //
            //
            // var disposeList = new List<UnwrappedTileId>();
            // foreach (var activeTile in activeTiles)
            // {
            //     if (!view.Tiles.ContainsKey(activeTile.Key) && !onHoldList.Contains(activeTile.Value.UnwrappedTileId))
            //     {
            //         disposeList.Add(activeTile.Value.UnwrappedTileId);
            //     }
            // }
            // foreach (var id in disposeList)
            // {
            //     _mapVisualizer.DisposeTile(id);
            // }


            var disposeList = new List<UnwrappedTileId>();
            foreach (var tilePair in _mapVisualizer.ActiveTiles)
            {
                if (!view.Tiles.ContainsKey(tilePair.Key))
                {
                    // var planes = GeometryUtility.CalculateFrustumPlanes(_camera);
                    // if ((!GeometryUtility.TestPlanesAABB(planes, _tiles[tilePair.Value].UnityBounds)))
                    // {
                        disposeList.Add(tilePair.Key);
                        //_tiles.Remove(tilePair.Value);
                    //}
                }
            }

            foreach (var id in disposeList)
            {
                _mapVisualizer.DisposeTile(id);
            }

            foreach (var rect in view.Tiles)
            {
                if (!_mapVisualizer.ActiveTiles.ContainsKey(rect.Key))
                {
                    var overlapping = ZoomOutTracker.ContainsKey(rect.Key) && ZoomOutTracker[rect.Key].Count == 4;
                    var tile = _mapVisualizer.LoadTile(rect.Key, !overlapping);

                    //_tiles.Add(tile, rect.Value);
                    if (tile != null)
                    {
                        tile.transform.position = rect.Value.Center;
                        tile.transform.SetParent(transform);
                        tile.transform.localScale = Vector3.one * rect.Value.UnityBounds.size.x / 100;
                        //tile.MeshRenderer.sharedMaterial.SetFloat("_ObjectScale", rect.Value.UnityBounds.size.x / 100);
                        //tile.gameObject.SetActive(true);
                    }
                }
            }
        }

        void OnDrawGizmos()
        {
            if (_currentView != null)
            {
                foreach (var tile in _currentView.Tiles.Values)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(tile.Center, tile.UnityBounds.size);

                }
            }

            if (MapVisualizer != null && MapVisualizer.ActiveTiles != null)
            {
                foreach (var tile in MapVisualizer.ActiveTiles.Values)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(tile.transform.position, tile.MeshRenderer.bounds.size);
                }
            }
        }
    }
}