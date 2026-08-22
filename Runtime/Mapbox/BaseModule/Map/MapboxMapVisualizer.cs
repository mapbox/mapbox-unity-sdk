using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.BaseModule.Map
{
    /// <summary>
    /// The primary object responsible for preparing the data and generating the visuals of the map.
    /// </summary>
    [Serializable]
    public class MapboxMapVisualizer : IMapVisualizer, ITileLifecycleSource
    {
        // Upper bound of the Mercator tile pyramid. Used by DelveInto and any other
        // recursion that needs an absolute hard cap independent of user-configurable
        // MaximumZoomLevel settings (which represent LOD intent, not pyramid depth).
        public const int MaxMercatorZoom = 22;

        public List<ILayerModule> LayerModules;
        public Dictionary<UnwrappedTileId, UnityMapTile> ActiveTiles { get; private set; }
        public List<UnityMapTile> TempTiles { get; private set; }
        public IMapInformation MapInformation => _mapInformation;

        // Explicit interface impl narrows ActiveTiles to IReadOnlyDictionary for the
        // ITileLifecycleSource contract while internal code keeps the mutable Dictionary
        // it needs for Add/Remove.
        IReadOnlyDictionary<UnwrappedTileId, UnityMapTile> ITileLifecycleSource.ActiveTiles => ActiveTiles;

        protected UnityContext _unityContext;
        protected IMapInformation _mapInformation;
        protected ITileCreator _tileCreator;

        private HashSet<UnwrappedTileId> _toRemove;
        private HashSet<CanonicalTileId> _retainedTiles;
        private Coroutine _internalUpdateCoroutine;
        private bool _destroyed;

        // Reusable scratch array to avoid per-frame allocation in InternalUpdateCoroutine
        private readonly UnwrappedTileId[] _quadrants = new UnwrappedTileId[4];

        // Explicit stack for the iterative DelveInto (was recursive with per-call new bool[4]
        // and new UnwrappedTileId[4] allocations). Bounded by recursion depth × 4, so the
        // List reaches its peak size after one large call and never re-allocates after.
        private struct DelveFrame
        {
            public UnwrappedTileId TileId;
            public int Depth;
            public int Found;       // bitmask: bit i set if quadrant i is satisfied (here or via descendants)
            public int NextChild;   // 0..4
            public int ParentFrame; // index in _delveStack, or -1 for root
            public int ParentSlot;  // which slot of the parent we are filling (0..3)
        }
        private readonly List<DelveFrame> _delveStack = new List<DelveFrame>();

        public MapboxMapVisualizer(IMapInformation mapInformation, UnityContext unityContext, ITileCreator tileCreator)
        {
            _unityContext = unityContext;
            _mapInformation = mapInformation;

            ActiveTiles = new Dictionary<UnwrappedTileId, UnityMapTile>(100);
            TempTiles = new List<UnityMapTile>();
            LayerModules = new List<ILayerModule>();

            _tileCreator = tileCreator;
            _tileCreator.OnTileBroken += (tt) =>
            {
                if (ActiveTiles.TryGetValue(tt, out var mapTile))
                {
                    PoolTile(mapTile);
                }
            };

            _mapInformation.WorldScaleChanged += RepositionAllTiles;
            _mapInformation.LatitudeLongitudeChanged += RepositionAllTiles;

            _toRemove = new HashSet<UnwrappedTileId>();
            _retainedTiles = new HashSet<CanonicalTileId>();

            _internalUpdateCoroutine = Runnable.Instance.StartCoroutine(InternalUpdate());
        }

        public virtual IEnumerator Initialize()
        {
            var existsTerrainModule = LayerModules.Any(x => x is ITerrainLayerModule);

            if (existsTerrainModule)
            {
                yield return _tileCreator.Initialize();
            }
            else
            {
                var terrainStrategy = new FlatTerrainStrategy();
                yield return _tileCreator.Initialize(terrainStrategy);
            }

            // Modules that keep bookkeeping in step with tiles (the terrain bounds tracker, for
            // one) subscribe from inside Initialize, using the source passed here. Handing it over
            // at this point is what makes a second attachment pass unnecessary: a module reaches
            // its subscription before its first yield, so it is watching before any tile loads.
            yield return LayerModules.Select(x => x.Initialize(this)).WaitForAll();
        }

        /// <summary>
        /// Prepare data and visuals for given tile cover. It loads the data to memory, generates vector feature visuals
        /// and prepare it all to ensure following tile requests will be finished in single frame.
        /// So this method doesn't create the tile, it prepares everything inside a tile.
        /// </summary>
        /// <param name="tileCover"></param>
        /// <returns></returns>
        public virtual IEnumerator LoadTileCoverToMemory(TileCover tileCover)
        {
            var hashsetTiles = new HashSet<CanonicalTileId>(tileCover.Tiles.Select(x => x.Canonical));
            var coroutines = LayerModules.SelectMany(x => x.GetTileCoverCoroutines(hashsetTiles).Where(x => x != null));
            yield return coroutines.WaitForAll();
        }

        public virtual void Load(TileCover tileCover)
        {
            RemoveUnnecessaryTiles(tileCover);

            // Protect filler children of temp tiles that are still loading.
            // Without this, fillers get pooled one frame after creation (DelveInto only
            // rescues them on the frame the temp tile is first added).
            for (var i = TempTiles.Count - 1; i >= 0; i--)
            {
                var tempTile = TempTiles[i];
                if (tempTile.Children != null && ActiveTiles.ContainsKey(tempTile.UnwrappedTileId))
                {
                    foreach (var child in tempTile.Children)
                    {
                        _toRemove.Remove(child.UnwrappedTileId);
                    }
                }
            }

            foreach (var tileId in tileCover.Tiles)
            {
                if (ActiveTiles.ContainsKey(tileId))
                {
                    continue;
                }

                UnityMapTile unityMapTile = null;
                {
                    if (CreateTileInstant(tileId, out unityMapTile))
                    {
                        ShowTile(unityMapTile);
                        continue;
                    }
                    else
                    {
                        CreateTempTile(tileId, out unityMapTile);
                        // Reuse the tile's own Children list across pool/reuse cycles instead of
                        // allocating one per cache miss. PoolTile clears it on return.
                        if (unityMapTile.Children == null)
                            unityMapTile.Children = new List<UnityMapTile>();
                        else
                            unityMapTile.Children.Clear();
                        var coveredByQuadrants = DelveInto(tileId, unityMapTile.Children, recursiveDepth: 1);
                        ActiveTiles.Add(tileId, unityMapTile);
                        TempTiles.Add(unityMapTile);
                        if (!coveredByQuadrants)
                        {
                            ShowTile(unityMapTile);
                        }
                    }
                }
            }

            foreach (var tileId in _toRemove)
            {
                if (ActiveTiles.TryGetValue(tileId, out var tile))
                {
                    if (tile.LoadingState == LoadingState.Temporary)
                    {
                        TempTiles.Remove(tile);
                    }

                    PoolTile(tile);
                }
            }

            _retainedTiles.Clear();
            foreach (var tile in tileCover.Tiles)
            {
                _retainedTiles.Add(tile.Canonical);
            }

            foreach (var visualization in LayerModules)
            {
                visualization.RetainTiles(_retainedTiles);
            }
        }

        /// <summary>
        /// Create the map in given tileCover area. Makes decision to load or unload tiles and handle temporary filler
        /// tiles until actual tiles are loaded.
        /// </summary>
        public virtual void InternalUpdateCoroutine()
        {
            //finish temp tiles from tempTiles list
            _toRemove.Clear();
            for (var index = TempTiles.Count - 1; index >= 0; index--)
            {
                var tilePair = TempTiles[index];
                if (!ActiveTiles.ContainsKey(tilePair.UnwrappedTileId))
                {
                    TempTiles.RemoveAt(index);
                    continue;
                }

                if (tilePair.LoadingState == LoadingState.Temporary && CreateTile(tilePair))
                {
                    ShowTile(tilePair);

                    if (tilePair.Children != null && tilePair.Children.Count > 0)
                    {
                        foreach (var child in tilePair.Children)
                        {
                            PoolTile(child);
                        }
                        tilePair.Children.Clear();
                    }

                    TempTiles.RemoveAt(index);

                    _quadrants[0] = tilePair.UnwrappedTileId.Quadrant(0);
                    _quadrants[1] = tilePair.UnwrappedTileId.Quadrant(1);
                    _quadrants[2] = tilePair.UnwrappedTileId.Quadrant(2);
                    _quadrants[3] = tilePair.UnwrappedTileId.Quadrant(3);
                    for (int i = 0; i < 4; i++)
                    {
                        if (ActiveTiles.TryGetValue(_quadrants[i], out _))
                        {
                            _toRemove.Add(_quadrants[i]);
                        }
                    }
                }
            }

            foreach (var tileId in _toRemove)
            {
                if (ActiveTiles.TryGetValue(tileId, out var tile))
                {
                    if (tile.LoadingState == LoadingState.Temporary)
                    {
                        TempTiles.Remove(tile);
                    }

                    PoolTile(tile);
                }
            }
        }


        /// <summary>
        /// Minimal function that'll try to load view with whatever data is available.
        /// It will not unload any tiles, it will not trigger any data fetching.
        /// It'll only organize and use data already in memory.
        /// If resources required for the requested tile aren't ready, it'll use whatever available
        /// and create a "temporary tile".
        /// </summary>
        /// <param name="tileCover"></param>
        public void LoadSnapshot(TileCover tileCover)
        {
            foreach (var tileId in tileCover.Tiles)
            {
                if (ActiveTiles.ContainsKey(tileId))
                    continue;

                UnityMapTile unityMapTile = null;
                if (!CreateTileInstant(tileId, out unityMapTile)) //if we can't fully load the tile
                {
                    CreateTempTile(tileId, out unityMapTile); //we load it whatever data we can find
                    ActiveTiles.Add(tileId, unityMapTile);
                    TempTiles.Add(unityMapTile);
                }

                ShowTile(unityMapTile);
            }
        }

        public void OnDestroy()
        {
            _destroyed = true;
            if (_internalUpdateCoroutine != null && Runnable.Instance != null)
            {
                Runnable.Instance.StopCoroutine(_internalUpdateCoroutine);
                _internalUpdateCoroutine = null;
            }

            foreach (var layerModule in LayerModules)
            {
                layerModule.OnDestroy();
            }
        }

        /// <summary>
        /// Find a LayerModule by given type. LayerModules are kept as ILayerModule and this method queries by concrete
        /// so it might cause unexpected issues if there are multiple layer modules of same type. This method will simply
        /// return the first one found.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="module"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetLayerModule<T>(out T module) where T : ILayerModule
        {
            module = (T)LayerModules.FirstOrDefault(x => x is T);
            if (module == null)
            {
                var composite = LayerModules.FirstOrDefault(x => x is CompositeLayerModule) as CompositeLayerModule;
                if (composite != null)
                {
                    module = (T)composite.LayerModules.FirstOrDefault(x => x is T);
                }
            }
            return module != null;
        }


        private IEnumerator InternalUpdate()
        {
            while (!_destroyed)
            {
                InternalUpdateCoroutine();
                yield return null;
            }
        }

        private void RemoveUnnecessaryTiles(TileCover tileCover)
        {
            // Fillers used to be exempt from removal here, but that caused orphaned filler
            // z-fighting when their logical parent transitioned out of TempTile state. They
            // are now eligible for pooling; visual continuity is preserved by the temp-tile
            // filler protection pass in Load() above.
            _toRemove.Clear();
            foreach (var tilePair in ActiveTiles)
            {
                if (!tileCover.Tiles.Contains(tilePair.Key))
                {
                    _toRemove.Add(tilePair.Key);
                }
            }
        }


        protected bool DelveInto(UnwrappedTileId tileId, List<UnityMapTile> activeChildren, int recursiveDepth = 3)
        {
            // Iterative replacement for the prior recursive implementation. Same semantics:
            // at each level we look up the 4 quadrants in ActiveTiles; missing ones recurse
            // deeper up to `recursiveDepth`; if any quadrant at a level is satisfied (here
            // or via a descendant), the remaining slots at THAT level get temp filler tiles.
            _delveStack.Clear();
            _delveStack.Add(new DelveFrame
            {
                TileId = tileId,
                Depth = recursiveDepth,
                Found = 0,
                NextChild = 0,
                ParentFrame = -1,
                ParentSlot = -1,
            });

            bool rootResult = false;

            while (_delveStack.Count > 0)
            {
                int frameIdx = _delveStack.Count - 1;
                var frame = _delveStack[frameIdx];

                if (frame.NextChild < 4)
                {
                    int slot = frame.NextChild;
                    var quadrant = frame.TileId.Quadrant(slot);
                    frame.NextChild++;

                    if (ActiveTiles.TryGetValue(quadrant, out var unityMapTile))
                    {
                        _toRemove.Remove(quadrant);

                        // A tile with a non-empty Children list is itself still hidden and
                        // covered by those (more detailed) children — it was created
                        // Temporary, its own DelveInto exploration found something, and it
                        // was never shown (Load()'s "if (!coveredByQuadrants) ShowTile(...)"
                        // skipped it). Passing those children through, instead of collapsing
                        // to this tile's own not-yet-loaded imagery, is what keeps the most
                        // detailed data actually available on screen during a fast zoom
                        // rather than replacing it with a coarser fallback the moment an
                        // ancestor happens to explore this far down. unityMapTile's own state
                        // and TempTiles membership are left untouched: it can still promote
                        // normally later, and InternalUpdateCoroutine's promotion path already
                        // shows it and pools these same child objects together, atomically, so
                        // there is never a frame where both are visible. The existing "protect
                        // filler children of temp tiles" pass at the top of Load() keeps
                        // reprotecting these children each frame for as long as unityMapTile
                        // stays in TempTiles, exactly as it already did before this tile was
                        // ever found by an ancestor's search.
                        if (unityMapTile.LoadingState == LoadingState.Temporary
                            && unityMapTile.Children != null && unityMapTile.Children.Count > 0)
                        {
                            activeChildren.Add(unityMapTile);
                        }
                        else
                        {
                            // Nothing more detailed hiding behind this one — it is already
                            // showing its own data (Finished, or a filler adopted earlier by
                            // a different ancestor search). Show it, and pool any children it
                            // might still be holding (shouldn't happen given the invariant
                            // above, kept defensively) instead of leaving them dangling.
                            TempTiles.Remove(unityMapTile);
                            unityMapTile.LoadingState = LoadingState.Filler;
                            if (unityMapTile.Children != null && unityMapTile.Children.Count > 0)
                            {
                                foreach (var subchild in unityMapTile.Children)
                                {
                                    PoolTile(subchild);
                                }
                                unityMapTile.Children.Clear();
                            }
                            ShowTile(unityMapTile);
                            activeChildren.Add(unityMapTile);
                        }

                        frame.Found |= 1 << slot;
                        _delveStack[frameIdx] = frame;
                    }
                    else if (frame.Depth > 0 && frame.TileId.Z < MaxMercatorZoom)
                    {
                        // Save the advanced NextChild before pushing the child frame —
                        // we'll resume this slot's evaluation after the child finishes.
                        _delveStack[frameIdx] = frame;
                        _delveStack.Add(new DelveFrame
                        {
                            TileId = quadrant,
                            Depth = frame.Depth - 1,
                            Found = 0,
                            NextChild = 0,
                            ParentFrame = frameIdx,
                            ParentSlot = slot,
                        });
                    }
                    else
                    {
                        // Leaf miss: nothing found here, no deeper recursion allowed.
                        _delveStack[frameIdx] = frame;
                    }
                }
                else
                {
                    bool anyFound = frame.Found != 0;
                    if (anyFound)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if ((frame.Found & (1 << i)) == 0)
                            {
                                var q = frame.TileId.Quadrant(i);
                                CreateTempTile(q, out var unityMapTile);
                                unityMapTile.LoadingState = LoadingState.Filler;
                                ActiveTiles[q] = unityMapTile;
                                ShowTile(unityMapTile);
                                activeChildren.Add(unityMapTile);
                            }
                        }
                    }

                    if (frame.ParentFrame >= 0)
                    {
                        var parent = _delveStack[frame.ParentFrame];
                        if (anyFound)
                        {
                            parent.Found |= 1 << frame.ParentSlot;
                        }
                        _delveStack[frame.ParentFrame] = parent;
                    }
                    else
                    {
                        rootResult = anyFound;
                    }
                    _delveStack.RemoveAt(frameIdx);
                }
            }

            return rootResult;
        }

        protected void ShowTile(UnityMapTile unityTile)
        {
            unityTile.gameObject.SetActive(true);
            _mapInformation.PositionObjectFor(unityTile.gameObject, unityTile.CanonicalTileId);
        }

        protected void PoolTile(UnityMapTile tile)
        {
            if (tile.LoadingState == LoadingState.None)
                return;

            TileUnloading(tile);
            ActiveTiles.Remove(tile.UnwrappedTileId);
            tile.Recycle();
            tile.LoadingState = LoadingState.None;
            _tileCreator.PutTile(tile);

            if (tile.Children != null)
            {
                foreach (var tileChild in tile.Children)
                {
                    PoolTile(tileChild);
                }

                tile.Children.Clear();
            }
        }

        protected void CreateTempTile(UnwrappedTileId tileId, out UnityMapTile tile)
        {
            //we need to do positioning and scaling before mesh gen for now
            GetMapTile(tileId, out tile);

            foreach (var module in LayerModules)
            {
                module.LoadTempTile(tile);
            }

            tile.LoadingState = LoadingState.Temporary;
            // ActiveTiles.Add(tileId, tile);
            // TempTiles.Add(tile);
        }

        protected bool CreateTileInstant(UnwrappedTileId tileId, out UnityMapTile tile)
        {
            GetMapTile(tileId, out tile);

            var result = CreateTile(tile);

            if (!result)
            {
                tile.Recycle();
                tile.LoadingState = LoadingState.None;
                _tileCreator.PutTile(tile);
            }

            return result;
        }

        protected void GetMapTile(UnwrappedTileId tileId, out UnityMapTile tile)
        {
            var rectd = Conversions.TileBoundsInUnitySpace(tileId, _mapInformation.CenterMercator,
                _mapInformation.Scale);
            tile = null;
            tile = _tileCreator.GetTile();
            // Local-space so the whole map composes with any parent transform on the
            // BaseTileRoot / MapRoot hierarchy (translation + rotation). AR/MR setups
            // typically parent MapRoot under an ARAnchor; setting world-space position
            // here would silently reset that on every tile cover update.
            // Note: non-unit MapRoot.localScale still breaks the quadtree LOD math
            // (camera position is read in world space). Recommended pattern: drive
            // map scale through MapInformation.Scale, not via MapRoot.transform.
            tile.transform.localPosition = new Vector3((float)rectd.Center.x, 0, (float)rectd.Center.y);
            tile.transform.localScale = Vector3.one * (float)rectd.Size.x;
            tile.Initialize(tileId, (float)rectd.Size.x * _mapInformation.Scale);
        }

        protected bool CreateTile(UnityMapTile unityMapTile)
        {
            var tileFinished = true;
            foreach (var module in LayerModules)
            {
                var moduleFinished = module.LoadInstant(unityMapTile);
                tileFinished &= moduleFinished;
                if (!moduleFinished) break;
            }

            if (tileFinished)
            {
                unityMapTile.LoadingState = LoadingState.Finished;
                if (!ActiveTiles.ContainsKey(unityMapTile.UnwrappedTileId))
                {
                    ActiveTiles.Add(unityMapTile.UnwrappedTileId, unityMapTile);
                }

                TileLoaded(unityMapTile);
            }

            return tileFinished;
        }

        /// <summary>
        /// Triggers the repositioning for all tiles per module. This is necessary for vector module to move feature visuals
        /// if (and only if) map settings are such that camera is static and map&tiles are moving (slippy map).
        /// </summary>
        /// <param name="mapInformation"></param>
        protected void RepositionAllTiles(IMapInformation mapInformation)
        {
            foreach (var tilePair in ActiveTiles)
            {
                ShowTile(tilePair.Value);
            }

            foreach (var module in LayerModules)
            {
                module.UpdatePositioning(mapInformation);
            }
        }

        /// <summary>
        /// Map tile finished loading with targeted detail level data. This tile isn't temporary anymore, it'll be in
        /// ActiveTiles list.
        /// </summary>
        public event Action<UnityMapTile> TileLoaded = (tile) => { };

        /// <summary>
        /// Map tile unloading event fires for tiles that are still in active tiles list but not in the latest tileCover.
        /// UnityMapTile object attached to event will be pooled after the event call.
        /// </summary>
        public event Action<UnityMapTile> TileUnloading = (tile) => { };
    }
}