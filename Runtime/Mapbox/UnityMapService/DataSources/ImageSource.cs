using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.UnityMapService.DataSources
{
    public abstract class ImageSource<T> : UnitySource<T> where T : RasterData, new()
    {
        protected Dictionary<CanonicalTileId, RasterTile> _waitingList;
        protected TypeMemoryCache<T> _memoryCache;
        private HashSet<CanonicalTileId> _activeRequestsToCancel;
        private ImageSourceSettings _settings;

        protected ImageSource(DataFetchingManager dataFetchingManager, MapboxCacheManager cacheManager, ImageSourceSettings settings) : base(dataFetchingManager, cacheManager, settings.TilesetId)
        {
            _settings = settings;
            _waitingList = new Dictionary<CanonicalTileId, RasterTile>();
            _activeRequestsToCancel = new HashSet<CanonicalTileId>();

            _memoryCache = RegisterTypeToMemoryCache<T>(this.GetHashCode(), _settings.CacheSize);
            _memoryCache.CacheItemDisposed += (t) =>
            {
                CacheItemDisposed(t);
            };
        }
        
        public override void LoadTile(CanonicalTileId requestedDataTileId)
        {
            LoadTileCore(requestedDataTileId);
        }
        
        public override bool CheckInstantData(CanonicalTileId tileId)
        {
            return _memoryCache.Exists(tileId);
        }
        
        public override bool GetInstantData(CanonicalTileId tileId, out T data)
        {
            var result = _memoryCache.Get(tileId, out data);
            if (data != null)
            {
                data.CacheType = CacheType.MemoryCache;
            }
            return result;
        }

        public override bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
        {
            foreach (var id in retainedTiles)
            {
                if (!CheckInstantData(id))
                {
                    LoadTile(id);
                }
            }
            
            _activeRequestsToCancel.Clear();
            foreach (var activeTile in _waitingList)
            {
                if (!retainedTiles.Contains(activeTile.Key) && (activeTile.Value != null && !activeTile.Value.IsBackgroundData))
                {
                    _activeRequestsToCancel.Add(activeTile.Key);
                }
            }
            
            foreach (var id in _activeRequestsToCancel)
            {
                CancelActiveRequests(id);
            }

            _memoryCache.RetainTiles(retainedTiles);

            return true;
        }
        
        public override void CancelActiveRequests(CanonicalTileId unityTileId)
        {
            if (_waitingList.ContainsKey(unityTileId))
            {
                var tile = _waitingList[unityTileId];
                if (tile != null)
                {
                    CancelFetching(tile, _tilesetId);
                }

                _waitingList.Remove(unityTileId);
            }
        }
        
        public override void DownloadAndCacheBaseTiles()
        {
            var backgroundTiles = new HashSet<CanonicalTileId>();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    backgroundTiles.Add(new CanonicalTileId(2, i, j));
                }
            }

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    backgroundTiles.Add(new CanonicalTileId(1, i, j));
                }
            }

            backgroundTiles.Add(new CanonicalTileId(0, 0, 0));

            foreach (var tileId in backgroundTiles)
            {
                BackgroundLoad(tileId, _tilesetId);
            }
        }

        public virtual void ClearMemoryCache()
        {
            _memoryCache.OnDestroy();
        }

        public IEnumerator ReloadTiles()
        {
            // IMPORTANT: Materialize keys BEFORE starting any coroutines to avoid
            // InvalidOperationException: Collection was modified during enumeration.
            // RefreshData modifies the cache (Remove/Add) while WaitForAll() would be
            // lazily enumerating the live dictionaries if we didn't snapshot them first.
            var activeKeys = _memoryCache.GetActiveData.Keys.ToList();
            var fallbackKeys = _memoryCache.GetFallbackData.Keys.ToList();

            var coroutines = activeKeys.Select(key => RefreshData(key));
            coroutines = coroutines.Concat(fallbackKeys.Select(key => RefreshData(key, data =>
            {
                _memoryCache.MarkFallback(key);
            })));

            yield return coroutines.WaitForAll();
            _memoryCache.ClearInactive();
        }
        
        public override IEnumerator ChangeTilesetId(string tilesetId)
        {
            _settings.TilesetId = tilesetId;
            _tilesetId = _settings.TilesetId;
            yield return Initialize();
            yield return ReloadTiles();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var tile in _waitingList)
            {
                tile.Value?.Cancel();
            }
            _memoryCache.OnDestroy();
        }
        
        


        //COROUTINE METHODS only used in initialization so far
        #region coroutines

        /// <summary>
        /// Shared fetch logic for tile data following the cache-check → file-cache → web-request fallback chain.
        /// </summary>
        /// <param name="requestedDataTileId">Tile ID to fetch</param>
        /// <param name="checkMemoryCacheFirst">If true, checks memory cache before starting fetch chain</param>
        /// <param name="clearExistingCache">If true, removes existing cache entry before adding new data</param>
        /// <param name="callback">Callback invoked with result (can be null)</param>
        private IEnumerator FetchTileDataCoroutine(
            CanonicalTileId requestedDataTileId,
            bool checkMemoryCacheFirst,
            bool clearExistingCache,
            Action<T> callback = null)
        {
            T resultData = null;

            // STEP 1: Check memory cache if requested (LoadTileCoroutine does this, RefreshData doesn't)
            if (checkMemoryCacheFirst && GetInstantData(requestedDataTileId, out resultData))
            {
                callback?.Invoke(resultData);
                yield break;
            }

            // STEP 2: If already being fetched, wait for completion
            if (_waitingList.ContainsKey(requestedDataTileId))
            {
                while(_waitingList.ContainsKey(requestedDataTileId))
                {
                    yield return null;
                }
                GetInstantData(requestedDataTileId, out resultData);
                callback?.Invoke(resultData);
                yield break;
            }

            // STEP 3: Try file cache
            _waitingList[requestedDataTileId] = null;
            yield return GetImageCoroutine<T>(requestedDataTileId, _tilesetId, _settings.UseNonReadableTextures,
                (data) =>
                {
                    resultData = data;
                    _waitingList.Remove(requestedDataTileId);

                    if (resultData != null)
                    {
                        data.CacheType = CacheType.FileCache;

                        // Clear existing cache if requested (RefreshData does this)
                        if (clearExistingCache && _memoryCache.Exists(requestedDataTileId))
                            _memoryCache.Remove(requestedDataTileId);

                        _memoryCache.Add(data);
                        CheckExpiration(data);
                    }
                });

            // STEP 4: If not in file cache, fetch from web
            if (resultData == null)
            {
                var dataTile = CreateTile(requestedDataTileId, _tilesetId);
                _waitingList[requestedDataTileId] = dataTile;
                var working = true;

                WebRequestData(dataTile, (fetchingResult) =>
                {
                    _waitingList.Remove(requestedDataTileId);

                    if (dataTile.CurrentTileState == TileState.Loaded)
                    {
                        // Clear existing cache entry if requested (for RefreshData)
                        if (clearExistingCache && _memoryCache.Exists(requestedDataTileId))
                            _memoryCache.Remove(requestedDataTileId);

                        // Process web response using shared method
                        resultData = TextureFromWebForCoroutine(dataTile);
                    }

                    working = false;
                });

                while (working)
                {
                    yield return null;
                }
            }

            callback?.Invoke(resultData);
        }

        public override IEnumerator LoadTileCoroutine(CanonicalTileId requestedDataTileId, Action<T> callback = null)
        {
            yield return FetchTileDataCoroutine(
                requestedDataTileId,
                checkMemoryCacheFirst: true,
                clearExistingCache: false,
                callback
            );
        }

        private IEnumerator RefreshData(CanonicalTileId requestedDataTileId, Action<T> callback = null)
        {
            yield return FetchTileDataCoroutine(
                requestedDataTileId,
                checkMemoryCacheFirst: false,
                clearExistingCache: true,
                callback
            );
        }
        
        public override IEnumerator LoadTilesCoroutine(IEnumerable<CanonicalTileId> retainedTiles, Action<List<T>> callback = null)
        {
            if(callback != null)
            {
                var results = new List<T>();
                var coroutines = retainedTiles.Select(x => LoadTileCoroutine(x, (data) => results.Add(data)));
                yield return coroutines.WaitForAll();
                callback?.Invoke(results);
            }
            else
            {
                var coroutines = retainedTiles.Select(x => LoadTileCoroutine(x));
                yield return coroutines.WaitForAll();
            }
        }
        #endregion
        
        
        
        
        protected abstract RasterTile CreateTile(CanonicalTileId tileId, string tilesetId);
        protected abstract T CreateRasterDataWrapper(RasterTile tile);
        
        private void LoadTileCore(CanonicalTileId requestedDataTileId, Action<T> callback = null)
        {
            if (IsInProgress(requestedDataTileId))
            {
                callback?.Invoke(null);
                return;
            }
            _waitingList[requestedDataTileId] = null;

            GetImageAsync<T>(requestedDataTileId, _tilesetId, _settings.UseNonReadableTextures, (cacheItem) =>
            {
                if (cacheItem != null)
                {
                    TextureReceivedFromFile(cacheItem);
                    CheckExpiration(cacheItem);
                    if (_waitingList.ContainsKey(requestedDataTileId))
                        _waitingList.Remove(requestedDataTileId);
                    callback?.Invoke(cacheItem);
                }
                else
                {
                    _waitingList.Remove(requestedDataTileId);
                    
                    var dataTile = CreateTile(requestedDataTileId, _tilesetId);
                    _waitingList[requestedDataTileId] = dataTile;
                    WebRequestData(dataTile, (fetchingResult) =>
                    {
                        T resultDataItem = null;
                        if (dataTile.CurrentTileState == TileState.Loaded)
                        {
                            resultDataItem = TextureReceivedFromWeb(dataTile);
                        }
                        else
                        {
                            //?
                        }
                        if (_waitingList.ContainsKey(requestedDataTileId))
                            _waitingList.Remove(requestedDataTileId);
                        callback?.Invoke(resultDataItem);
                    });
                }
            });
        }
        
        protected virtual void TextureReceivedFromFile(T textureCacheItem)
        {
            //var tile = (RasterTile) textureCacheItem.Tile;
            //textureCacheItem.Tile = tile;
            //tile.SetTextureFromCache(textureCacheItem.Texture2D);
            //tile.FromCache = CacheType.FileCache;
            textureCacheItem.CacheType = CacheType.FileCache;

            //IMPORTANT file is read from file cache and it's not automatically
            //moved to memory cache. we have to do it here.
            _memoryCache.Add(textureCacheItem);
        }

        protected virtual T TextureReceivedFromWeb(RasterTile tile)
        {
            tile.AddLog(string.Format("{0} - {1}", Time.unscaledTime, " TextureReceivedHandler"));
            if (tile.Texture2D != null)
            {
                tile.AddLog("updated and old texture is destroyed");
                GameObject.Destroy(tile.Texture2D);
            }

            if (tile.CurrentTileState == TileState.Loaded && tile.Data != null)
            {
                //IMPORTANT This is where we create a Texture2D
                tile.AddLog("extracting texture ", tile.Id);
                tile.ExtractTextureFromRequest();

                var newTextureCacheItem = CreateRasterDataWrapper(tile);

                _memoryCache.Add(newTextureCacheItem);
                SaveImage(newTextureCacheItem, true);

                return newTextureCacheItem;
            }

            return null;
        }

        //this is a clone of method above for terrain coroutine process
        //terrain source overrides method above and calls extract elevation data there
        //this one doesn't call extract data anywhere and it's handles separately during
        //coroutine stuff
        //this should be the one to stay in the future
        private T TextureFromWebForCoroutine(RasterTile tile)
        {
            tile.AddLog(string.Format("{0} - {1}", Time.unscaledTime, " TextureReceivedHandler"));
            if (tile.Texture2D != null)
            {
                tile.AddLog("updated and old texture is destroyed");
                GameObject.Destroy(tile.Texture2D);
            }

            if (tile.CurrentTileState == TileState.Loaded && tile.Data != null)
            {
                //IMPORTANT This is where we create a Texture2D
                tile.AddLog("extracting texture ", tile.Id);
                tile.ExtractTextureFromRequest();

                var newTextureCacheItem = CreateRasterDataWrapper(tile);

                _memoryCache.Add(newTextureCacheItem);
                SaveImage(newTextureCacheItem, true);

                return newTextureCacheItem;
            }

            return null;
        }
        
        
        protected void BackgroundLoad(CanonicalTileId tileId, string tilesetId)
        {
            GetImageAsync<T>(tileId, tilesetId, SystemInfo.supportsAsyncGPUReadback, (cacheItem) =>
            {
                if (cacheItem != null)
                {
                    TextureReceivedFromFile(cacheItem);
                    _memoryCache.MarkFallback(cacheItem.TileId);
                    CheckExpiration(cacheItem);
                }
                else
                {
                    var dataTile = CreateTile(tileId, tilesetId);
                    dataTile.IsBackgroundData = true;
                    WebRequestData(dataTile, (fetchingResult) =>
                    {
                        if (dataTile.CurrentTileState != TileState.Canceled)
                        {
                            TextureReceivedFromWeb(dataTile);
                            _memoryCache.MarkFallback(dataTile.Id);
                        }
                    });
                }
            });
        }

        protected virtual void CheckExpiration(T cacheItem)
        {
            var dataTask = ReadEtagExpiration(cacheItem, 4);
            if (dataTask != null) //can be null if sqlite cache isn't available
            {
                if (dataTask.IsCompleted) //supporting instant calls
                    OnDataTaskDataContinueWith(dataTask.Outcome, dataTask.DataResult);
                else
                    dataTask.DataCompleted += (task, data) => OnDataTaskDataContinueWith(dataTask.Outcome, data);
            }
        }

        private void OnDataTaskDataContinueWith(DataTaskOutcome outcome, T data)
        {
            // Nothing usable came back, so there is no expiration to check. All three of the
            // conditions below used to arrive as one undifferentiated null and were dereferenced
            // on the next line:
            //  - Cancelled: the read never ran, so this tile's expiry is simply unknown.
            //  - Faulted: sqlite threw. TaskManager has already logged the exception.
            //  - Completed with a null result: there is genuinely no row for this tile.
            // Skipping the check is the right response to all three, but they are distinguishable
            // now, so a caller that needs to treat a fault differently from a miss can.
            if (outcome != DataTaskOutcome.Completed || data == null) return;

            if (data.ExpirationDate == null || DateTime.Compare((DateTime)data.ExpirationDate, DateTime.Now) < 0)
            {
                TileExpired(data.TilesetId, data.TileId);
                var dataTile = CreateTile(data.TileId, data.TilesetId);
                dataTile.ETag = data.ETag;
                WebRequestUpdate(dataTile, (result) =>
                {
                    if (result.State == WebResponseResult.Failed)
                    {
                        Debug.LogError(result.ExceptionsAsString);
                        return;
                    }

                    if (result.State == WebResponseResult.Cancelled) return;
                    
                    var tile = result.Tile as RasterTile;
                    if (tile == null)
                        return;
                    
                    if (tile.StatusCode == 200)
                    {
                        //Debug.Log("expired and returned 200");
                        TextureReceivedFromWeb(tile);
                    }
                    else if (tile.StatusCode == 304)
                    {
                        //not changed, just update meta?
                        //Debug.Log("expired but not changed, just update meta?");
                        UpdateExpiration(tile.Id, tile.TilesetId, tile.ExpirationDate);
                    }

                    TileUpdated(tile.TilesetId, tile.Id);
                });
                //Debug.Log("tile needs an update");
            }
            else
            {
                //Debug.Log("doesnt needs an update");
            }
        }
        
        protected bool IsInProgress(CanonicalTileId requestedDataTileId)
        {
            return _waitingList.ContainsKey(requestedDataTileId) || IsActiveRequest(requestedDataTileId);
        }
    }
    
}