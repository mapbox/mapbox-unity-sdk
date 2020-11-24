using System;
using System.IO;
using Mapbox.Map;
using UnityEngine;

namespace Mapbox.Platform.Cache
{
    public class MapboxCacheManager
    {
        private TextureMemoryCache _textureMemoryCache;
        private MemoryCache _vectorMemoryCache;
        private FileCache _textureFileCache;
        private SQLiteCache _sqLiteCache;

        public MapboxCacheManager(TextureMemoryCache textureMemoryCache, MemoryCache memoryCache, FileCache fileCache = null, SQLiteCache sqliteCache = null)
        {
            _textureMemoryCache = textureMemoryCache;
            _vectorMemoryCache = memoryCache;
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

        public CacheItem GetDataItem(string tilesetId, CanonicalTileId tileId)
        {
            var cacheItem = _vectorMemoryCache.Get(tilesetId, tileId);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            if (_sqLiteCache != null)
            {
                cacheItem = _sqLiteCache.Get(tilesetId, tileId);
                if (cacheItem != null)
                {
                    _vectorMemoryCache.Add(tilesetId, tileId, cacheItem, true);
                }
            }

            return cacheItem;
        }

        public void AddDataItem(string tilesetId, CanonicalTileId tileId, CacheItem cachedItem, bool forceInsert)
        {
            _vectorMemoryCache.Add(tilesetId, tileId, cachedItem, forceInsert);
            _sqLiteCache?.Add(tilesetId, tileId, cachedItem, forceInsert);
        }

        public TextureCacheItem GetTextureItemFromMemory(string tilesetId, CanonicalTileId tileId)
        {
            return (TextureCacheItem) _textureMemoryCache.Get(tilesetId, tileId);
        }

        public bool TextureFileExists(string tilesetId, CanonicalTileId tileId)
        {
            if (_textureFileCache != null)
            {
                return _textureFileCache.Exists(tilesetId, tileId);
            }

            return false;
        }

        public void AddTextureItem(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
        {
            _textureMemoryCache.Add(tilesetId, tileId, textureCacheItem, forceInsert);
            _textureFileCache?.Add(tilesetId, tileId, textureCacheItem, forceInsert);
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
                
                _textureMemoryCache.Add(tilesetId, tileId, textureCacheItem, true);
                callback(textureCacheItem);
            });
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

            _textureMemoryCache.Clear(); //clear tracked objects
            _vectorMemoryCache.Clear(); //clear tracked objects
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
            _vectorMemoryCache.Clear();
            _textureMemoryCache?.Clear();
        }
    }
}