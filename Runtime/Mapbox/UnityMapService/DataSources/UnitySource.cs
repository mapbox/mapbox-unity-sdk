using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.TileJSON;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;

namespace Mapbox.UnityMapService.DataSources
{
    public abstract class UnitySource<T> : Source<T>
    {
        public override bool IsReady()
        {
            return _isTileJsonReady;
        }

        protected string _tilesetId;

        /// <summary>
        /// The tileset this source fetches. For vector sources this can be a composed,
        /// comma-separated list of tileset ids, which the Vector Tiles API returns as one tile
        /// carrying the union of their layers.
        /// </summary>
        public string TilesetId => _tilesetId;

        private bool _isTileJsonReady;
        private TileJSONResponse _tileJsonResponse;
        protected int[] _sourceZoomRange;
        
        private readonly DataFetchingManager _dataFetchingManager;
        private readonly MapboxCacheManager _cacheManager;
        private IAsyncRequest _tileJsonRequest;
        private Dictionary<CanonicalTileId, FetchInfo> _activeRequests;
        private Dictionary<CanonicalTileId, List<TaskWrapper>> _activeTasks;

        protected UnitySource(DataFetchingManager dataFetchingManager, MapboxCacheManager cacheManager, string tilesetId)
        {
            _activeTasks = new Dictionary<CanonicalTileId, List<TaskWrapper>>();
            _tilesetId = tilesetId;
            _dataFetchingManager = dataFetchingManager;
            _cacheManager = cacheManager;
            _activeRequests = new Dictionary<CanonicalTileId, FetchInfo>();
            _sourceZoomRange = new[] {0, 22};
        }
        
        public override IEnumerator Initialize()
        {
            while (!_isTileJsonReady)
            {
                if (_tileJsonRequest == null)
                {
                    _tileJsonRequest = _dataFetchingManager.GetTileJSON(1).Get(_tilesetId, (response) =>
                    {
                        if (response == null || response.MaxZoom == 0) //failed
                        {
                            //TODO fix this part
                            _tileJsonResponse = null;
                            _sourceZoomRange = new[] {0, 22};
                            _isTileJsonReady = true;
                        }
                        else
                        {
                            _tileJsonResponse = response;
                            _sourceZoomRange = new[] {_tileJsonResponse.MinZoom, _tileJsonResponse.MaxZoom};
                            _isTileJsonReady = true;
                        };
                    });
                }
                yield return null;
            }
        }

        protected void WebRequestData(Tile tile, Action<DataFetchingResult> callback) => RequestData(tile, callback, false);
        protected void WebRequestUpdate(Tile tile, Action<DataFetchingResult> callback) => RequestData(tile, callback, true);
        
        /// <summary>
        /// Enqueues a fetch for the tile. The callback is invoked exactly once, always - that is
        /// the contract callers depend on, because several of them park a coroutine on a flag the
        /// callback sets (see ImageSource and PbfSource's "while (working) yield return null").
        ///
        /// A second request for a tile id already in flight joins the existing fetch instead of
        /// starting a new one, so it receives that fetch's real outcome. It used to return here
        /// without invoking the callback at all, which read to the caller as "nothing happened,
        /// ever" and left those coroutines spinning for the rest of the session - and with them
        /// LoadTileCoverToMemory, and so LoadViewCompleted, which never fired.
        ///
        /// Note the join is keyed on tile id alone, so a conditional-GET refresh (WebRequestUpdate)
        /// and a real data fetch (WebRequestData) collide and whichever arrived first wins. The
        /// joiner is handed a result for a tile instance it does not own, so it will see its own
        /// tile as unloaded and report no data - correct, if pessimistic, and it can request again.
        /// </summary>
        private void RequestData(Tile tile, Action<DataFetchingResult> callback, bool isUpdate)
        {
            if (_activeRequests.TryGetValue(tile.Id, out var inFlight))
            {
                inFlight.Callback += callback;
                return;
            }

            var fetchInfo = new FetchInfo(tile, (result) =>
            {
                _activeRequests.Remove(tile.Id);
                callback(result);
            });
            fetchInfo.IsUpdate = isUpdate;
            _activeRequests.Add(tile.Id, fetchInfo);
            _dataFetchingManager.EnqueueForFetching(fetchInfo);
        }
		
        protected void CancelFetching(Tile tile, string tilesetId)
        {
            tile.Cancel();
            _cacheManager.CancelFetching(tile.Id, tilesetId);
            //we removed data fetching cancel here as data fetching now track
            //removal through the tile.Cancel
            //no further calls are necessary
            // _dataFetchingManager.CancelFetching(tile, tilesetId);
            
            if(_activeTasks.TryGetValue(tile.Id, out List<TaskWrapper> taskList))
            {
                foreach (var task in taskList)
                {
                    task.Cancel();
                }
                _activeTasks.Remove(tile.Id);
            }
        }

        public void SaveBlob(MapboxTileData vectorCacheItem, bool forceInsert)
        {
            _cacheManager.SaveBlob(vectorCacheItem, forceInsert);
        }

        public void SaveImage(RasterData textureCacheItem, bool forceInsert)
        {
            _cacheManager.SaveImage(textureCacheItem, forceInsert);
        }
        
        public void RemoveData(string tilesetId, int zoom, int x, int y)
        {
            _cacheManager.RemoveData(tilesetId, zoom, x, y);
        }
        
        public void GetImageAsync<T1>(CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T1> callback) where T1 : RasterData, new()
        {
            _cacheManager.GetImageAsync(tileId, tilesetId, isTextureNonreadable, callback);
        }
        
        public IEnumerator GetImageCoroutine<T1>(CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T1> callback) where T1 : RasterData, new()
        {
            yield return _cacheManager.GetImageCoroutine(tileId, tilesetId, isTextureNonreadable, callback);
        }
        
        public DataTaskWrapper<T1> GetTileInfoAsync<T1>(CanonicalTileId tileId, string tilesetid, int priority = 1) where T1 : MapboxTileData, new()
            => GetTileData<T1>(tileId, tilesetid, null, priority);
        
        public DataTaskWrapper<T1> ReadEtagExpiration<T1>(T1 data, int priority = 1) where T1 : MapboxTileData, new()
            => GetTileData<T1>(data.TileId, data.TilesetId, data, priority);

        private DataTaskWrapper<T1> GetTileData<T1>(CanonicalTileId tileId, string tilesetid, T1 data = null, int priority = 1) where T1 : MapboxTileData, new()
        {
            var taskWrapper = _cacheManager.GetTileInfoTask<T1>(tileId, tilesetid);
            if (taskWrapper == null) return null;
            if (taskWrapper.IsCompleted)
            {
                CompleteTask(taskWrapper);
            }
            else
            {
                TrackTask(taskWrapper);
                taskWrapper.DataCompleted += ((resultTask, result) => CompleteTask(taskWrapper));
            }
            return taskWrapper;
        }
        
        public IEnumerator GetTileData<T1>(CanonicalTileId tileId, string tilesetid, T1 data = null, int priority = 1, Action<T1> callback = null) where T1 : MapboxTileData, new()
        {
            // Tracked like the etag read above, so CancelFetching can reach it. This is the path that
            // reads the actual tile bytes and it was the one path whose tasks nothing could cancel.
            //
            // Untracking is keyed on the task completing, not on this coroutine completing, and that
            // distinction matters: IsActiveRequest is _activeTasks.ContainsKey, which PbfSource and
            // ImageSource use as their "already in flight" guard. An entry left behind by a coroutine
            // that was abandoned - the map destroyed mid-read, the owner stopped - would make that
            // tile look permanently in flight and it would never be requested again. DataCompleted
            // fires on cancellation as well as on success, so the entry always comes back out.
            yield return _cacheManager.GetBlobCoroutine<T1>(tileId, tilesetid, priority, data, callback,
                task =>
                {
                    TrackTask(task);
                    if (task is DataTaskWrapper<T1> typed)
                    {
                        typed.DataCompleted += (resultTask, result) => CompleteTask(task);
                    }
                });
        }
        
        public void UpdateExpiration(CanonicalTileId tileId, string tilesetId, DateTime date)
        {
            _cacheManager.UpdateExpiration(tileId, tilesetId, date);
        }

        protected TypeMemoryCache<T1> RegisterTypeToMemoryCache<T1>(int owner, int cacheSize = 100) where T1 : MapboxTileData
        {
            return _cacheManager.RegisterMemoryCache<T1>(owner, cacheSize);
        }

        // public override bool IsZinSupportedRange(int z)
        // {
        //     return z >= _sourceZoomRange[0] && z <= _sourceZoomRange[1];
        // }
        
        private void TrackTask(TaskWrapper task)
        {
            if (!_activeTasks.ContainsKey(task.TileId))
            {
                _activeTasks.Add(task.TileId, new List<TaskWrapper>());
            }
            _activeTasks[task.TileId].Add(task);
        }
        
        private void CompleteTask(TaskWrapper task)
        {
            if (_activeTasks.TryGetValue(task.TileId, out List<TaskWrapper> tasks))
            {
                tasks.Remove(task);
                if (tasks.Count == 0)
                    _activeTasks.Remove(task.TileId);
            }
        }
        
        protected bool IsActiveRequest(CanonicalTileId tileId) => _activeTasks.ContainsKey(tileId);
        
        public Action<string, CanonicalTileId> TileExpired = (tilesetid, tileId) => { };
        public Action<string, CanonicalTileId> TileUpdated = (tilesetid, tileId) => { };
    }
}