using System;
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
        private QuadTreeView _previousView;

        private Camera _camera;

        private Material LineMaterial;

        public override void Start()
        {
            base.Start();
            _camera = Camera.main;
            CameraController.Initialize(WorldScale, _camera, this);
            QuadTreeGenerator.Initialize(WorldScale, _camera, this);

            _mapVisualizer.OnTileFinished -= OnMapVisualizerOnOnTileFinished;
            _mapVisualizer.OnTileDisposing -= OnMapVisualizerOnOnTileDisposing;
            _mapVisualizer.OnTileFinished += OnMapVisualizerOnOnTileFinished;
            _mapVisualizer.OnTileDisposing += OnMapVisualizerOnOnTileDisposing;

            _isDirty = true;
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
            var newView = QuadTreeGenerator.UpdateQuadTree(elevationAtCenter);
            RequestTiles(newView);
            _previousView = newView;
            UpdateTilePositions(_previousView);
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
            foreach (var tile in _mapVisualizer.ActiveTiles.Values)
            {
                var rect = tile.Rect;

                // TODO: this is constant for all tiles--cache.
                var scale = tile.TileScale;
                var scaleFactor = Mathf.Pow(2, (InitialZoom - tile.UnwrappedTileId.Z));

                var position = new Vector3(
                    (float)(rect.Center.x - CenterMercator.x),
                    0,
                    (float)(rect.Center.y - CenterMercator.y)) / WorldScale;
                tile.transform.localPosition = position;
            }
            // foreach (var mapVisualizerActiveTile in _mapVisualizer.ActiveTiles)
            // {
            //     var tile = mapVisualizerActiveTile.Value;
            //     if(view.Tiles.ContainsKey(tile.UnwrappedTileId))
            //     {
            //         var rect = view.Tiles[tile.UnwrappedTileId];
            //         tile.transform.localPosition = rect.Center;
            //
            //         tile.MeshRenderer.bounds.SetMinMax(rect.UnityBounds.min, rect.UnityBounds.max);
            //     }
            // }
            //
            // foreach (var rectD in _quadrantsToAdd)
            // {
            //     if (MapVisualizer.ActiveTiles.ContainsKey(rectD.Id))
            //     {
            //         MapVisualizer.ActiveTiles[rectD.Id].transform.localPosition = rectD.Center;
            //     }
            // }
        }

        protected Dictionary<UnwrappedTileId, HashSet<UnwrappedTileId>> ZoomOutTracker = new Dictionary<UnwrappedTileId, HashSet<UnwrappedTileId>>();
        private Dictionary<UnwrappedTileId, UnwrappedTileId> _childParentRelationships = new Dictionary<UnwrappedTileId, UnwrappedTileId>();
        protected Dictionary<UnwrappedTileId, HashSet<UnwrappedTileId>> _parentsWaitingForChildren = new Dictionary<UnwrappedTileId, HashSet<UnwrappedTileId>>();
        private HashSet<UnwrappedTileId> _zoomOutChildTilesOnHold = new HashSet<UnwrappedTileId>();

        private void OnMapVisualizerOnOnTileDisposing(UnityTile t)
        {
            t.Logs.Add(string.Format("{0} - {1}", Time.frameCount, "dispose"));
            var toDispose = new List<UnwrappedTileId>();

            RemoveZoomInReferences(t.UnwrappedTileId, toDispose);

            if (ZoomOutTracker.ContainsKey(t.UnwrappedTileId))
            {
                foreach (var tileId in ZoomOutTracker[t.UnwrappedTileId])
                {
                    if (tileId != t.UnwrappedTileId)
                    {
                        //_mapVisualizer.DisposeTile(tileId);
                        toDispose.Add(tileId);
                    }
                    RemoveZoomInReferences(tileId, toDispose);
                }
                ZoomOutTracker.Remove(t.UnwrappedTileId);
            }

            foreach (var tileId in toDispose)
            {
                _mapVisualizer.DisposeTile(tileId);
            }
        }

        private void RemoveZoomInReferences(UnwrappedTileId tileId, List<UnwrappedTileId> toDispose)
        {
            if (_childParentRelationships.ContainsKey(tileId))
            {
                var parent = _childParentRelationships[tileId];
                _childParentRelationships.Remove(tileId);
                if (_parentsWaitingForChildren.ContainsKey(parent))
                {
                    _parentsWaitingForChildren[parent].Remove(tileId);
                    if (_parentsWaitingForChildren[parent].Count == 0)
                    {
                        _parentsWaitingForChildren.Remove(parent);
                        if (_zoomOutChildTilesOnHold.Contains(parent))
                        {
                            _zoomOutChildTilesOnHold.Remove(parent);
                        }
                        // if (!_previousView.Tiles.ContainsKey(parent))
                        // {
                        //     toDispose.Add(parent);
                        // }
                    }
                }

            }
        }

        private void OnMapVisualizerOnOnTileFinished(UnityTile t)
        {
            t.Logs.Add(string.Format("{0} - {1}", Time.frameCount, "finished"));
            if (_childParentRelationships.ContainsKey(t.UnwrappedTileId))
            {
                //we are zooming in, first clear out the child/parent relationship
                //then check parent is ready to get removed
                var parent = _childParentRelationships[t.UnwrappedTileId];
                _childParentRelationships.Remove(t.UnwrappedTileId);

                if (_parentsWaitingForChildren.ContainsKey(parent))
                {
                    _parentsWaitingForChildren[parent].Remove(t.UnwrappedTileId);
                    if (_parentsWaitingForChildren[parent].Count == 0)
                    {
                        _parentsWaitingForChildren.Remove(parent);
                        foreach (var tileId in parent.Children)
                        {
                            if (!_childParentRelationships.ContainsKey(tileId) &&
                                !_parentsWaitingForChildren.ContainsKey(tileId) &&
                                _mapVisualizer.ActiveTiles.ContainsKey(tileId) &&
                                _previousView.Tiles.ContainsKey(tileId))
                            {
                                _mapVisualizer.ActiveTiles[tileId].gameObject.SetActive(true);
                            }
                        }

                        if (_mapVisualizer.ActiveTiles.ContainsKey(parent))
                        {
                            _mapVisualizer.ActiveTiles[parent].Logs
                                .Add(string.Format("{0} - {1}", Time.frameCount, "OnMapVisualizerOnOnTileFinished parent"));
                            _mapVisualizer.ActiveTiles[parent].gameObject.SetActive(false);
                            _mapVisualizer.ActiveTiles[parent].Logs
                                .Add(string.Format("{0} - {1}", Time.frameCount, "set active false"));
                            _mapVisualizer.DisposeTile(parent);
                        }
                    }
                }
                else
                {
                    _mapVisualizer.DisposeTile(parent);
                }
            }

            if (ZoomOutTracker.ContainsKey(t.UnwrappedTileId))
            {
                foreach (var tileId in ZoomOutTracker[t.UnwrappedTileId])
                {
                    if (_mapVisualizer.ActiveTiles.ContainsKey(tileId))
                    {
                        _mapVisualizer.ActiveTiles[tileId].gameObject.SetActive(false);
                        _mapVisualizer.ActiveTiles[tileId].Logs
                            .Add(string.Format("{0} - {1}", Time.frameCount, "ZoomOutTracker dispose"));
                    }
                    _zoomOutChildTilesOnHold.Remove(tileId);
                    _mapVisualizer.DisposeTile(tileId);
                }
                ZoomOutTracker.Remove(t.UnwrappedTileId);
                t.Logs
                    .Add(string.Format("{0} - {1}", Time.frameCount, "ZoomOutTracker remove"));

                if (_previousView.Tiles.ContainsKey(t.UnwrappedTileId) &&
                    !_zoomOutChildTilesOnHold.Contains(t.UnwrappedTileId) &&
                    !t.IsStopped)
                {
                    t.gameObject.SetActive(true);
                    t.Logs
                    .Add(string.Format("{0} - {1}", Time.frameCount, "set active true"));
                }
            }
        }

        private void RequestTiles(QuadTreeView newView)
        {
            var toAdd = new List<Tuple<UnwrappedTileId, UnityRectD, bool>>();
            var toRemove = new HashSet<UnwrappedTileId>();
            var toStop = new HashSet<UnwrappedTileId>();

            foreach (var newViewTile in newView.Tiles)
            {
                if (_mapVisualizer.ActiveTiles.ContainsKey(newViewTile.Key))
                {
                    continue;
                }

                var enableTile = true;
                var newViewRect = newViewTile.Value;
                var newId = newViewTile.Key;
                var parentId = newViewTile.Key.Parent;
                if (newId == parentId)
                    continue;
                //zoom out
                for (int i = 0; i < 4; i++)
                {
                    var child = newViewTile.Value.QuadrantTileId(i);
                    if (_previousView != null && _previousView.Tiles.ContainsKey(child))
                    {
                        if (!ZoomOutTracker.ContainsKey(newId))
                        {
                            ZoomOutTracker.Add(newId, new HashSet<UnwrappedTileId>());
                        }

                        enableTile = false;
                        if (_mapVisualizer.ActiveTiles.ContainsKey(child))
                        {
                            if (ZoomOutTracker.ContainsKey(child))
                            {
                                foreach (var grandChild in ZoomOutTracker[child])
                                {
                                    ZoomOutTracker[newId].Add(grandChild);
                                }

                                ZoomOutTracker.Remove(child);
                                _zoomOutChildTilesOnHold.Remove(child);
                                toRemove.Add(child);
                            }
                            else
                            {
                                ZoomOutTracker[newId].Add(child);
                                _zoomOutChildTilesOnHold.Add(child);
                                toStop.Add(child);
                            }
                        }

                        //this part is important for cases where we zoom out before
                        //a zoom in operation using same tiles finishes.
                        //so when you zoom in and before all children is loaded, you zoom out
                        //this should clear leftovers from first zoom-in action

                        if (_childParentRelationships.ContainsKey(child))
                        {
                            var zoomInParent = _childParentRelationships[child];
                            _childParentRelationships.Remove(child);
                            _parentsWaitingForChildren[zoomInParent].Remove(child);
                            if (_parentsWaitingForChildren[zoomInParent].Count == 0)
                            {
                                _parentsWaitingForChildren.Remove(zoomInParent);
                            }
                        }
                    }
                }

                //zoom in
                if (_previousView != null && _previousView.Tiles.ContainsKey(parentId))
                {
                    toStop.Add(parentId);
                    if (_childParentRelationships.ContainsKey(parentId))
                    {
                        var grandParent = _childParentRelationships[parentId];
                        _parentsWaitingForChildren.Remove(parentId);
                        _childParentRelationships.Add(newId, grandParent);
                        if (_parentsWaitingForChildren.ContainsKey(grandParent))
                        {
                            _parentsWaitingForChildren[grandParent].Remove(parentId);
                            _parentsWaitingForChildren[grandParent].Add(newId);
                        }
                        else
                        {
                            Debug.Log("this shouldn't happen, right?");
                        }

                        toRemove.Add(parentId);
                    }
                    else
                    {
                        _childParentRelationships.Add(newId, parentId);
                        if (!_parentsWaitingForChildren.ContainsKey(parentId))
                        {
                            _parentsWaitingForChildren.Add(parentId, new HashSet<UnwrappedTileId>());
                        }
                        _parentsWaitingForChildren[parentId].Add(newId);
                    }
                    enableTile = false;

                    if(ZoomOutTracker.ContainsKey(parentId))
                    {
                        foreach (var tileId in ZoomOutTracker[parentId])
                        {
                            _zoomOutChildTilesOnHold.Remove(tileId);
                        }
                        ZoomOutTracker.Remove(parentId);
                    }

                    if (ZoomOutTracker.ContainsKey(newId))
                    {
                        foreach (var tileId in ZoomOutTracker[newId])
                        {
                            _zoomOutChildTilesOnHold.Remove(tileId);
                        }
                        ZoomOutTracker.Remove(newId);
                    }
                }

                toAdd.Add(new Tuple<UnwrappedTileId, UnityRectD, bool>(newId, newViewRect, enableTile));

            }

            foreach (var tile in _mapVisualizer.ActiveTiles.Values)
            {
                if (newView.Tiles.ContainsKey(tile.UnwrappedTileId))
                {
                    continue;
                }

                if (_childParentRelationships.ContainsKey(tile.UnwrappedTileId))
                {
                    continue;
                }

                if (ZoomOutTracker.ContainsKey(tile.UnwrappedTileId))
                {
                    continue;
                }

                if (_parentsWaitingForChildren.ContainsKey(tile.UnwrappedTileId))
                    continue;

                if (_zoomOutChildTilesOnHold.Contains(tile.UnwrappedTileId))
                    continue;

                toRemove.Add(tile.UnwrappedTileId);
            }

            foreach (var tuple in toAdd)
            {
                var newChildTile = _mapVisualizer.LoadTile(tuple.Item1, tuple.Item3);
                if (newChildTile != null)
                {
                    newChildTile.transform.position = tuple.Item2.Center;
                    newChildTile.transform.SetParent(transform);
                    newChildTile.transform.localScale = Vector3.one * tuple.Item2.UnityBounds.size.x / 10;
                    newChildTile.Logs
                        .Add(string.Format("{0} - {1}", Time.frameCount, "brand new tile"));
                }
            }
            foreach (var unwrappedTileId in toRemove)
            {
                if (_mapVisualizer.ActiveTiles.ContainsKey(unwrappedTileId))
                {
                    _mapVisualizer.ActiveTiles[unwrappedTileId].Logs.Add(string.Format("{0} - {1}", Time.frameCount, "remove untracked"));
                }
                _mapVisualizer.DisposeTile(unwrappedTileId);
            }

            foreach (var unwrappedTileId in toStop)
            {
                _mapVisualizer.StopTile(unwrappedTileId);
            }
        }

        void OnDrawGizmos()
        {
            if (_previousView != null)
            {
                foreach (var tile in _previousView.Tiles.Values)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(tile.Center, tile.UnityBounds.size);

                }
            }


            // if (MapVisualizer != null && MapVisualizer.ActiveTiles != null)
            // {
            //     foreach (var tile in MapVisualizer.ActiveTiles.Values)
            //     {
            //         Gizmos.color = Color.blue;
            //         Gizmos.DrawWireCube(tile.transform.position, tile.MeshRenderer.bounds.size);
            //     }
            // }
        }
    }
}