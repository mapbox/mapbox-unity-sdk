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
        protected Dictionary<UnwrappedTileId, ParentTileChildren> _parentsWaitingForChildren = new Dictionary<UnwrappedTileId, ParentTileChildren>();
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
                    _parentsWaitingForChildren[parent].RemoveTile(tileId);
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
                    _parentsWaitingForChildren[parent].TileCompleted(t.UnwrappedTileId);
                    if (_mapVisualizer.ActiveTiles.ContainsKey(t.UnwrappedTileId) &&
                        _parentsWaitingForChildren.ContainsKey(parent))
                    {
                        _mapVisualizer.ActiveTiles[t.UnwrappedTileId].Logs.Add("removed from " + parent + " of " + _parentsWaitingForChildren[parent].Count + " " + _parentsWaitingForChildren[parent].LoadingChildren);
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

                if (
                    //_previousView.Tiles.ContainsKey(t.UnwrappedTileId) &&
                    //!_zoomOutChildTilesOnHold.Contains(t.UnwrappedTileId) &&
                    !t.IsStopped)
                {
                    t.gameObject.SetActive(true);
                    t.Logs
                    .Add(string.Format("{0} - {1}", Time.frameCount, "set active true"));
                }
            }
        }

        private void FinalizeZoomInTile(ParentTileChildren rel)
        {
            if (!_parentsWaitingForChildren.ContainsKey(rel.ParentId))
                return;

            foreach (var tileId in rel.Children)
            {
                if (
                    //!_childParentRelationships.ContainsKey(tileId) &&
                    //!_parentsWaitingForChildren.ContainsKey(tileId) &&
                    _mapVisualizer.ActiveTiles.ContainsKey(tileId))
                {
                    _mapVisualizer.ActiveTiles[tileId].gameObject.SetActive(true);
                    _mapVisualizer.ActiveTiles[tileId].Logs.Add("FinalizeZoomInTile set active");
                }
            }
            rel.Children.Clear();
            _parentsWaitingForChildren.Remove(rel.ParentId);

            if (_mapVisualizer.ActiveTiles.ContainsKey(rel.ParentId))
            {
                _mapVisualizer.ActiveTiles[rel.ParentId].Logs
                    .Add(string.Format("{0} - {1}", Time.frameCount, "OnMapVisualizerOnOnTileFinished parent"));
                _mapVisualizer.ActiveTiles[rel.ParentId].gameObject.SetActive(false);
                _mapVisualizer.ActiveTiles[rel.ParentId].Logs
                    .Add(string.Format("{0} - {1}", Time.frameCount, "set active false"));
                _mapVisualizer.DisposeTile(rel.ParentId);
            }

            if (_zoomOutChildTilesOnHold.Contains(rel.ParentId))
            {
                _zoomOutChildTilesOnHold.Remove(rel.ParentId);
            }
        }

        private void RequestTiles(QuadTreeView newView)
        {
            var toAdd = new List<Tuple<UnwrappedTileId, UnityRectD, bool, List<string>>>();
            var toRemove = new HashSet<UnwrappedTileId>();
            var toStop = new HashSet<UnwrappedTileId>();
            foreach (var newViewTile in newView.Tiles)
            {
                var tileLogs = new List<string>();
                tileLogs.Add(Time.frameCount.ToString());
                if (_mapVisualizer.ActiveTiles.ContainsKey(newViewTile.Key))
                {
                    continue;
                }

                var enableTile = true;
                var newId = newViewTile.Key;
                var parentId = newViewTile.Key.Parent;
                if (newId == parentId)
                    continue;

                if (_previousView != null)
                {
                    //zoom out
                    var childrenInView = new List<UnwrappedTileId>();
                    if (ChildrenInPreviousView(ref newId, ref childrenInView))
                    {
                        foreach (var child in childrenInView)
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
                                    tileLogs.Add("nested zoom out");
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
                                    tileLogs.Add("child added to zoom out tracker & hold " + child);
                                    ZoomOutTracker[newId].Add(child);
                                    _zoomOutChildTilesOnHold.Add(child);
                                    toStop.Add(child);
                                }
                            }

                            if (_childParentRelationships.ContainsKey(child))
                            {
                                tileLogs.Add("child was in zoom in tracker " + child);
                                var zoomInParent = _childParentRelationships[child];
                                _childParentRelationships.Remove(child);
                                if (_parentsWaitingForChildren.ContainsKey(zoomInParent))
                                {
                                    _parentsWaitingForChildren[zoomInParent].RemoveTile(child);
                                }
                                else
                                {
                                    Debug.Log("this shouldn't happen");
                                }
                            }
                        }

                    }

                    //zoom in
                    if (ParentInPreviousView(ref newId, ref parentId))
                    {
                        toStop.Add(parentId);
                        enableTile = false;

                        tileLogs.Add("zoom in");
                        if (_childParentRelationships.ContainsKey(parentId))
                        {
                            var grandParent = _childParentRelationships[parentId];
                            _parentsWaitingForChildren.Remove(parentId);
                            _childParentRelationships.Add(newId, grandParent);
                            if (_parentsWaitingForChildren.ContainsKey(grandParent))
                            {
                                _parentsWaitingForChildren[grandParent].AddChildTile(newId);
                                _parentsWaitingForChildren[grandParent].RemoveTile(parentId);
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
                                _parentsWaitingForChildren.Add(parentId, new ParentTileChildren(parentId, FinalizeZoomInTile));
                            }

                            _parentsWaitingForChildren[parentId].AddChildTile(newId);
                            tileLogs.Add("added to parents waiting list " + newId);
                        }

                        if (ZoomOutTracker.ContainsKey(parentId))
                        {
                            tileLogs.Add("hmm ZoomOutTracker.ContainsKey(parentId)");
                            foreach (var tileId in ZoomOutTracker[parentId])
                            {
                                _zoomOutChildTilesOnHold.Remove(tileId);
                            }

                            ZoomOutTracker.Remove(parentId);
                        }

                        if (ZoomOutTracker.ContainsKey(newId))
                        {
                            tileLogs.Add("hmm ZoomOutTracker.ContainsKey(newId)");
                            foreach (var tileId in ZoomOutTracker[newId])
                            {
                                _zoomOutChildTilesOnHold.Remove(tileId);
                            }

                            ZoomOutTracker.Remove(newId);
                        }
                    }

                }
                toAdd.Add(new Tuple<UnwrappedTileId, UnityRectD, bool, List<string>>(newId, newViewTile.Value, enableTile, tileLogs));
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

                foreach (var log in tuple.Item4)
                {
                    newChildTile.Logs.Add(log);
                }

                if (ZoomOutTracker.ContainsKey(tuple.Item1))
                {
                    newChildTile.Logs.Add("waiting for children: " + string.Join(",", ZoomOutTracker[tuple.Item1]));
                }

                if (_childParentRelationships.ContainsKey(tuple.Item1))
                {
                    newChildTile.Logs.Add("waiting for parent: " + string.Join(",", _childParentRelationships[tuple.Item1]));
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

        private bool ChildrenInPreviousView(ref UnwrappedTileId newId, ref List<UnwrappedTileId> childrenInView)
        {
            foreach (var pair in _previousView.Tiles)
            {
                var id = pair.Key;
                if (id.Z > newId.Z)
                {
                    if (newId == id.ParentAt(newId.Z))
                    {
                        childrenInView.Add(id);
                    }
                }
            }

            return childrenInView.Count > 0;
        }

        private bool ParentInPreviousView(ref UnwrappedTileId newId, ref UnwrappedTileId parentId)
        {
            parentId = newId.Parent;
            while (parentId.Z > 1)
            {
                if(_previousView.Tiles.ContainsKey(parentId))
                {
                    return true;
                }

                parentId = parentId.Parent;
            }

            return false;
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


        protected class ParentTileChildren
        {
            public UnwrappedTileId ParentId;
            public HashSet<UnwrappedTileId> Children;
            public int LoadingChildren;
            public Action<ParentTileChildren> Finalize;

            public ParentTileChildren(UnwrappedTileId parentId, Action<ParentTileChildren> finalize)
            {
                ParentId = parentId;
                Children = new HashSet<UnwrappedTileId>();
                LoadingChildren = 0;
                Finalize = finalize;
            }

            public int Count => Children.Count;

            public void AddChildTile(UnwrappedTileId newChild)
            {
                if (!Children.Contains(newChild))
                {
                    Children.Add(newChild);
                    LoadingChildren++;
                }
            }

            public void TileCompleted(UnwrappedTileId parentId)
            {
                if (Children.Contains(parentId))
                {
                    LoadingChildren--;

                    if (LoadingChildren <= 0)
                    {
                        if (Finalize != null)
                            Finalize(this);
                    }
                }
            }

            public void RemoveTile(UnwrappedTileId parentId)
            {
                if (Children.Contains(parentId))
                {
                    Children.Remove(parentId);
                    LoadingChildren--;

                    if (LoadingChildren <= 0)
                    {
                        if (Finalize != null)
                            Finalize(this);
                    }
                }
            }
        }
    }
}