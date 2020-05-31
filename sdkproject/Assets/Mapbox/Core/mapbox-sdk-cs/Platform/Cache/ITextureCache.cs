using System;
using Mapbox.Map;
using UnityEngine;

namespace Mapbox.Platform.Cache
{
	public interface ITextureCache : ICache
	{
		void Add(string mapId, CanonicalTileId tileId, TextureCacheItem textureCacheItem, bool forceInsert);
		void GetAsync(string mapId, CanonicalTileId tileId, Action<TextureCacheItem> callback);
		bool Exists(string tilesetId, CanonicalTileId tileId);
	}
}