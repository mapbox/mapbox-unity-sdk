using System;
using Mapbox.Map;

namespace Mapbox.Platform.Cache
{
    public class MapboxCacheManager
    {
        private TextureMemoryCache _textureMemoryCache;
        private MemoryCache _vectorMemoryCache;
        private FileCache _textureFileCache;
        private SQLiteCache _sqLiteCache;

        public MapboxCacheManager(TextureMemoryCache textureMemoryCache, MemoryCache memoryCache, FileCache fileCache, SQLiteCache sqliteCache)
        {
            _textureMemoryCache = textureMemoryCache;
            _vectorMemoryCache = memoryCache;
            _textureFileCache = fileCache;
            _sqLiteCache = sqliteCache;
        }
		
        public void Clear()
        {
            _textureMemoryCache.Clear();
            _vectorMemoryCache.Clear();
            _textureFileCache.Clear();
            _sqLiteCache.Clear();
        }

        public void ReInit()
        {
            _textureMemoryCache.ReInit();
            _vectorMemoryCache.ReInit();
            _textureFileCache.ReInit();
            _sqLiteCache.ReInit();
        }

        public CacheItem GetDataItem(string tilesetId, CanonicalTileId tileId)
        {
            var cacheItem = _vectorMemoryCache.Get(tilesetId, tileId);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            cacheItem = _sqLiteCache.Get(tilesetId, tileId);
            if (cacheItem != null)
            {
                _vectorMemoryCache.Add(tilesetId, tileId, cacheItem, true);
            }

            return cacheItem;
        }

        public void AddDataItem(string tilesetId, CanonicalTileId tileId, CacheItem cachedItem, bool forceInsert)
        {
            _vectorMemoryCache.Add(tilesetId, tileId, cachedItem, forceInsert);
            _sqLiteCache.Add(tilesetId, tileId, cachedItem, forceInsert);
        }

        public bool TextureExists(string tilesetId, CanonicalTileId tileId)
        {
            return _textureFileCache.Exists(tilesetId, tileId);
        }
		
        public TextureCacheItem GetTextureItem(string tilesetId, CanonicalTileId tileId)
        {
            return (TextureCacheItem) _textureMemoryCache.Get(tilesetId, tileId);
        }
		
        public void GetTextureItem(string tilesetId, CanonicalTileId tileId, Action<TextureCacheItem> callback)
        {
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
                    
                    _sqLiteCache.DeleteTileFile(textureCacheItem.FilePath, tilesetId, tileId);
                }
                
                _textureMemoryCache.Add(tilesetId, tileId, textureCacheItem, true);
                callback(textureCacheItem);
            });
        }

        public void AddTextureItem(string tilesetId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert)
        {
            _textureMemoryCache.Add(tilesetId, tileId, textureCacheItem, forceInsert);
            _textureFileCache.Add(tilesetId, tileId, textureCacheItem, forceInsert);
        }
    }
}