using System;
using System.IO;
using Mapbox.Map;
using UnityEngine;

namespace Mapbox.Platform.Cache
{
    public class MapboxCacheManager
    {
        private MemoryCache _memoryCache;
        private FileCache _textureFileCache;
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

        public void AddVectorDataItem(string tilesetId, CanonicalTileId tileId, CacheItem cachedItem, bool forceInsert)
        {
            _memoryCache.Add(tilesetId, tileId, cachedItem, forceInsert);
            _sqLiteCache?.Add(tilesetId, tileId, cachedItem, forceInsert);
        }

        public VectorCacheItem GetVectorItemFromMemory(string tilesetId, CanonicalTileId tileId)
        {
            return (VectorCacheItem) _memoryCache.Get(tilesetId, tileId);
        }

        public void GetVectorItemFromSqlite(string tilesetId, CanonicalTileId tileId, Action<VectorCacheItem> callback)
        {
            if (_sqLiteCache == null)
            {
                callback(null);
                return;
            }

            var cacheItem = (VectorCacheItem) _sqLiteCache.Get(tilesetId, tileId);
            if (cacheItem != null)
            {
                _memoryCache.Add(tilesetId, tileId, cacheItem, true);
            }

            //kept callback behaviour from texture operations here as we'll most likely need to change this to async
            //either due to sqlite read or vector tile decompressing
            //so it's not async or using the callback thing properly at the moment

            callback(cacheItem);
        }

        public void AddTextureItem(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
        {
            _memoryCache.Add(tilesetId, tileId, textureCacheItem, forceInsert);
            _textureFileCache?.Add(tilesetId, tileId, textureCacheItem, forceInsert);
        }

        public TextureCacheItem GetTextureItemFromMemory(string tilesetId, CanonicalTileId tileId)
        {
            return (TextureCacheItem) _memoryCache.Get(tilesetId, tileId);
        }

        public void GetTextureItemFromFile(string tilesetId, CanonicalTileId tileId, Action<TextureCacheItem> callback)
        {
            if (_textureFileCache == null)
            {
                callback(null);
                return;
            }

            _textureFileCache.GetAsync(tilesetId, tileId, (textureCacheItem) =>
            {

#if UNITY_EDITOR
                //helpful for debugging memory
                textureCacheItem.Texture2D.name = tileId.ToString() + "_" + tilesetId;
#endif
                var tile = _sqLiteCache.Get(tilesetId, tileId);
                if (tile != null)
                {
                    textureCacheItem.ETag = tile.ETag;
                    textureCacheItem.ExpirationDate = tile.ExpirationDate;
                }
                else
                {
                    //file exists but sqlite entry does not
                    //entry was probably pruned but file deletion didn't go through (crashed/closed app)
                    //serve the image without metadata for now
                    //delete tile, next tile it'll be updated
                    
                    _textureFileCache.DeleteTileFile(textureCacheItem.FilePath);
                }
                
                _memoryCache.Add(tilesetId, tileId, textureCacheItem, true);
                callback(textureCacheItem);
            });
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
                return _textureFileCache.Exists(tilesetId, tileId);
            }

            return false;
        }

        private void TextureFileSaved(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem)
        {
            _sqLiteCache?.Add(tilesetId, tileId, textureCacheItem, true);
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
    }
}