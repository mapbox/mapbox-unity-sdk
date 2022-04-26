﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Platform.Cache
{
    public class MapboxCacheManager
    {
        private IMemoryCache _memoryCache;
        private IFileCache _textureFileCache;
        private SQLiteCache _sqLiteCache;

        public MapboxCacheManager(MemoryCache memoryCache, FileCache fileCache = null, SQLiteCache sqliteCache = null)
        {
            _memoryCache = memoryCache;
            _textureFileCache = fileCache;
            _sqLiteCache = sqliteCache;

            if (_textureFileCache != null)
            {
                _textureFileCache.FileSaved += TextureFileSaved;
            }

            if (_sqLiteCache != null)
            {
                if (!_sqLiteCache.IsUpToDate())
                {
                    var sqliteDeleteSuccess = _sqLiteCache.ClearDatabase();
                    if (sqliteDeleteSuccess && _textureFileCache != null)
                    {
                        _textureFileCache.ClearAll();
                    }
                    _sqLiteCache.ReadySqliteDatabase();
                }

                CheckSqlAndFileIntegrity();
            }
        }

        /// <summary>
        /// We check for files that exists but not tracked in sqlite file and delete them all
        /// If we don't do that, those files will pile up (assuming systems loses track due to a bug somehow) and fill all the disk
        /// Vice versa (file doesn't exists, sqlite entry does) isn't important as entry will be cycled out soon anyway
        /// </summary>
        private void CheckSqlAndFileIntegrity()
        {
            var sqlTileList = _sqLiteCache.GetAllTiles();
            var fileList = _textureFileCache.GetFileList();

            foreach (var tile in sqlTileList)
            {
                if (fileList.Contains(tile.tile_path))
                {
                    fileList.Remove(tile.tile_path);
                }
            }

            if (fileList.Count > 0)
            {
                foreach (var filePath in fileList)
                {
                    File.Delete(filePath);
                }

                //double checking just in case
                CheckSqlAndFileIntegrity();
            }
        }

        public void AddVectorItemToMemory(string tilesetId, CanonicalTileId tileId, CacheItem vectorCacheItem, bool forceInsert)
        {
            _memoryCache.Add(tileId, tilesetId, vectorCacheItem, forceInsert);
        }

        public void AddVectorDataItem(string tilesetId, CanonicalTileId tileId, CacheItem vectorCacheItem, bool forceInsert)
        {
            _memoryCache.Add(tileId, tilesetId, vectorCacheItem, forceInsert);
            _sqLiteCache?.Add(tilesetId, tileId, vectorCacheItem, forceInsert);
        }

        public CacheItem GetVectorItemFromMemory(string tilesetId, CanonicalTileId tileId)
        {
            return _memoryCache.Get(tileId, tilesetId);
        }

        public void GetVectorItemFromSqlite(
            Map.VectorTile tile,
            string tilesetId,
            CanonicalTileId tileId,
            Action<CacheItem> successCallback,
            Action failureCallback,
            Action cancelledCallback
            )
        {
            if (_sqLiteCache == null)
            {
                failureCallback();
                return;
            }

            var localTilesetId = tilesetId;
            var localTileId = tileId;
            var localCopy = tile;
            CacheItem cacheItem = null;

            var task = new TaskWrapper(tileId.GenerateKey(localTilesetId, "GetVectorItemSqlite"))
            {
                OwnerTileId = localTileId,
                TileId = localTileId,
                TilesetId = tilesetId,
                Action = () =>
                {
                    cacheItem = _sqLiteCache.Get(localTilesetId, localTileId);
                    if (cacheItem.Data != null)
                    {
                        localCopy.SetByteData(cacheItem.Data);
                    }
                },
                ContinueWith = (t) =>
                {
                    if (t.Exception != null)
                    {
                        failureCallback();
                    }
                    else
                    {
#if UNITY_EDITOR
                        localCopy.FromCache = CacheType.SqliteCache;
                        cacheItem.From = localCopy.FromCache;
#endif
                        localCopy.ETag = cacheItem.ETag;
                        cacheItem.Tile = localCopy;
                        successCallback(cacheItem);
                    }
                },
                OnCancelled = () =>
                {
                    cancelledCallback();
                },
#if UNITY_EDITOR
                Info = "MapboxCacheManager.GetVectorItemFromSqlite"
#endif
            };
            MapboxAccess.Instance.TaskManager.AddTask(task);


            // if (cacheItem != null)
            // {
            //     _memoryCache.Add(tileId, tilesetId, cacheItem, true);
            // }

            //kept callback behaviour from texture operations here as we'll most likely need to change this to async
            //either due to sqlite read or vector tile decompressing
            //so it's not async or using the callback thing properly at the moment
        }

        public void AddTextureItem(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
        {
            _memoryCache.Add(tileId, tilesetId, textureCacheItem, forceInsert);
            _textureFileCache?.Add(tileId, tilesetId, textureCacheItem, forceInsert);
        }

        public void AddTextureItemToMemory(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
        {
            _memoryCache.Add(tileId, tilesetId, textureCacheItem, forceInsert);
        }

        public void UpdateExpirationDate(string tilesetId, CanonicalTileId tileId, DateTime expirationDate)
        {
            _memoryCache.UpdateExpiration(tilesetId, tileId, expirationDate);
            _sqLiteCache?.UpdateExpiration(tilesetId, tileId, expirationDate);
        }

        public TextureCacheItem GetTextureItemFromMemory(string tilesetId, CanonicalTileId tileId, bool resetDestructionIndex = false)
        {
            return (TextureCacheItem) _memoryCache.Get(tileId, tilesetId, resetDestructionIndex);
        }

        public void GetTextureItemFromFile(
            string tilesetId,
            CanonicalTileId tileId,
            CanonicalTileId ownerTileId,
            bool isTextureNonreadable,
            Action<TextureCacheItem> textureReadCallback,
            Action<TextureCacheItem> textureInfoReadyCallback,
            Action failureCallback,
            Action cancelledCallback = null,
            Action fileNotFoundCallback = null)
        {
            if (_textureFileCache == null)
            {
                failureCallback();
                return;
            }

            var fileExists = _textureFileCache.GetAsync(
                tileId,
                tilesetId,
                isTextureNonreadable,
                (textureCacheItem) =>
            {
                //this might happen in some corner cases
                //it means file was supposed to be there but couldn't be found in the last step when requested
                //maybe deleted in an earlier frame by cache limit?
                if (textureCacheItem == null || textureCacheItem.HasError)
                {
                    failureCallback();
                    return;
                }

#if UNITY_EDITOR
                //textureCacheItem.Texture2D.name = string.Format("{0}_{1}", tileId.ToString(), tilesetId);
#endif

                textureReadCallback(textureCacheItem);

                CacheItem cacheItem = null;
                MapboxAccess.Instance.TaskManager.AddTask(
                    new TaskWrapper(tileId.GenerateKey(tilesetId, "GetTextureItemInfoSql"))
                    {
                        OwnerTileId = ownerTileId,
                        TileId = tileId,
                        TilesetId = tilesetId,
                        Action = () => { cacheItem = _sqLiteCache.Get(tilesetId, tileId); },
                        ContinueWith = (t) =>
                        {
                            if (cacheItem != null)
                            {
                                textureCacheItem.ETag = cacheItem.ETag;
                                textureCacheItem.ExpirationDate = cacheItem.ExpirationDate;
                            }
                            else
                            {
                                //file exists but sqlite entry does not
                                //entry was probably pruned but file deletion didn't go through (crashed/closed app)
                                //serve the image without metadata for now
                                //delete tile, next tile it'll be updated

                                _textureFileCache.DeleteTileFile(textureCacheItem.FilePath);
                            }

                            textureInfoReadyCallback(textureCacheItem);
                        },
#if UNITY_EDITOR
                        Info = "MapboxCacheManager.GetTextureItemInfoSql"
#endif
                    }, 4);

                //this isn't async. shouldn't it be?
                // var tile = _sqLiteCache.Get(tilesetId, tileId);
                // if (tile != null)
                // {
                //     textureCacheItem.ETag = tile.ETag;
                //     textureCacheItem.ExpirationDate = tile.ExpirationDate;
                // }
                // else
                // {
                //     //file exists but sqlite entry does not
                //     //entry was probably pruned but file deletion didn't go through (crashed/closed app)
                //     //serve the image without metadata for now
                //     //delete tile, next tile it'll be updated
                //
                //     _textureFileCache.DeleteTileFile(textureCacheItem.FilePath);
                // }

                //decided not to do this and leave control to caller
                //they can add it to memory cache using AddTextureItemToMemory
                //_memoryCache.Add(tilesetId, tileId, textureCacheItem, true);

                //callback(textureCacheItem);
            });

            if (!fileExists)
            {
                if (fileNotFoundCallback != null)
                {
                    fileNotFoundCallback();
                }
                else
                {
                    failureCallback();
                }

                return;
            }
        }

        public bool TileExistsInSqlite(string tilesetId, CanonicalTileId tileId)
        {
            if (_sqLiteCache != null)
            {
                return _sqLiteCache.TileExists(tilesetId, tileId);
            }

            return false;
        }

        public bool TextureFileExists(string tilesetId, CanonicalTileId tileId)
        {
            if (_textureFileCache != null)
            {
                return _textureFileCache.Exists(tileId, tilesetId);
            }

            return false;
        }

        private void TextureFileSaved(CanonicalTileId tileId, string tilesetId, TextureCacheItem textureCacheItem)
        {
            _sqLiteCache?.Add(tilesetId, tileId, textureCacheItem, true);
        }

        public void MarkFallback(CanonicalTileId tileId, string tilesetId)
        {
            _memoryCache.MarkFallback(tileId, tilesetId);
        }

        public void ClearAndReinitCacheFiles()
        {
            var sqliteDeleteSuccess = _sqLiteCache.ClearDatabase();
            if (sqliteDeleteSuccess && _textureFileCache != null)
            {
                _textureFileCache.ClearAll();
            }

            Debug.Log("Cached files all cleared");

            _memoryCache.Clear(); //clear tracked objects
            _textureFileCache?.Clear(); //clear tracked objects
            _sqLiteCache?.Reopen(); //close existing, reopen and create if necessary

            Debug.Log("Caches reinitialized");
            if (_sqLiteCache != null)
            {
                _sqLiteCache.ReadySqliteDatabase();
                Debug.Log("SQlite cache tables recreated");
            }
        }

        public void ClearMemoryCache()
        {
            _memoryCache?.Clear();
        }

#if UNITY_EDITOR
        public EditorMemoryCache GetMemoryCache()
        {
            return _memoryCache as EditorMemoryCache;
        }

        public EditorFileCache GetFileCache()
        {
            return _textureFileCache as EditorFileCache;
        }
#endif

        public void TileDisposed(UnityTile tile, string tilesetId)
        {
            _memoryCache?.TileDisposed(tile, tilesetId);
            _textureFileCache?.TileDisposed(tile, tilesetId);
        }

        public void TileDisposed(RasterTile tileTerrainData, string tilesetId)
        {
            _memoryCache?.TileDisposed(tileTerrainData, tilesetId);
            _textureFileCache?.TileDisposed(tileTerrainData, tilesetId);
        }
    }
}