using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
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

        //private Dictionary<UnityTile, UnityRectD> _tiles = new Dictionary<UnityTile, UnityRectD>();
        private QuadTreeView _currentView;

        private Camera _camera;

        public override void Start()
        {
            base.Start();
            _camera = Camera.main;
            QuadTreeCameraController.Initialize(WorldScale, _camera, this);
            QuadCameraSettings.Initialize(WorldScale, _camera, this);

            // _mapVisualizer.OnTileFinished -= OnMapVisualizerOnOnTileFinished;
            // _mapVisualizer.OnTileDisposing -= OnMapVisualizerOnOnTileDisposing;
            // _mapVisualizer.OnTileFinished += OnMapVisualizerOnOnTileFinished;
            // _mapVisualizer.OnTileDisposing += OnMapVisualizerOnOnTileDisposing;

            RedrawMap();
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

            var viewChanged = QuadTreeCameraController.UpdateCamera();
            if (viewChanged || _isDirty)
            {
                RedrawMap();
            }

            if (_currentView != null)
            {
                foreach (var viewTile in _currentView.Tiles)
                {
                    foreach (var edge in viewTile.Value.Edges())
                    {
                        Debug.DrawLine(edge.Item1, edge.Item2);
                    }
                }

                if (_currentView.CenterRect != null)
                {
                    foreach (var edge in _currentView.CenterRect.Edges())
                    {
                        Debug.DrawLine(edge.Item1, edge.Item2, Color.red);
                    }
                }
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
            _currentView = QuadCameraSettings.UpdateQuadTree();
            RequestTiles(_currentView);
            UpdateTilePositions(_currentView);
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
                        tile.MeshRenderer.sharedMaterial.SetFloat("_ObjectScale", rect.Value.UnityBounds.size.x / 100);
                        //tile.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}