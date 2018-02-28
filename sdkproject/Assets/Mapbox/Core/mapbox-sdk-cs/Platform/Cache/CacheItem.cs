
namespace Mapbox.Platform.Cache
{

	using System;


	public class CacheItem
	{
		/// <summary> Raw response data- </summary>
		public byte[] Data;
		/// <summary> UTC ticks when item was added to the cache. </summary>
		public long AddedToCacheTicksUtc;
		/// <summary> ETag value of API response. https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag </summary>
		public string ETag;
		/// <summary> Can be 'null' as not all APIs populated this value. Last-Modified value of API response in GMT: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Last-Modified </summary>
		public DateTime? LastModified;
	}
}
