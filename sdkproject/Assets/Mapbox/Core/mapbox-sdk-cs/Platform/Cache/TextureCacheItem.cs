using UnityEngine;

namespace Mapbox.Platform.Cache
{
	public class TextureCacheItem : CacheItem
	{
		public Texture2D Texture2D;
		public string FilePath;
	}

	public class VectorCacheItem : CacheItem
	{
		public VectorTile.VectorTile VectorTile;
	}
}