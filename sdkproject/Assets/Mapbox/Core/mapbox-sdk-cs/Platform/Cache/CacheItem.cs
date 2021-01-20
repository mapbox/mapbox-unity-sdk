
using Mapbox.Map;

namespace Mapbox.Platform.Cache
{

	using System;


	public class CacheItem
	{
		public CanonicalTileId TileId;
		public string TilesetId;
		/// <summary> Raw response data- </summary>
		public byte[] Data;
		/// <summary> UTC ticks when item was added to the cache. </summary>
		public long AddedToCacheTicksUtc;
		/// <summary> ETag value of API response. https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag </summary>
		public string ETag;
		/// <summary> Expiration date of the cached data </summary>
		public DateTime? ExpirationDate;
		/// <summary> Has an error occured reading from cache or operation cancelled while system is reading from cache async </summary>
		public bool HasError = false;

#if UNITY_EDITOR
		public CacheType From;
#endif
	}
}
