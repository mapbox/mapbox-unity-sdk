using System;
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
            }
        }

        public void ReInit()
        {
            _textureMemoryCache.ReInit();
            _vectorMemoryCache.ReInit();
            _textureFileCache?.ReInit();
            _sqLiteCache?.ReInit();
        }

        public void Clear()
        {
            _textureMemoryCache.Clear();
            _vectorMemoryCache.Clear();
            _textureFileCache?.Clear();
            _sqLiteCache?.Clear();
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
            ReInit();
            Debug.Log("Caches reinitialized");
        }
    }
}