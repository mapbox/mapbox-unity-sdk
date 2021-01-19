using System;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;

namespace CustomImageLayerSample
{
	public sealed class MapboxImageFactoryManager : ImageFactoryManager
	{
		public bool UseRetina = true;
		public bool UseMipMap = false;
		public bool UseCompression = false;

		public MapboxImageFactoryManager(string tilesetId, bool downloadFallbackImagery, bool useRetina = true, bool useMipMap = false, bool useCompression = false) : base(tilesetId, downloadFallbackImagery)
		{
			UseRetina = useRetina;
			UseMipMap = useMipMap;
			UseCompression = useCompression;

			if (DownloadFallbackImagery)
			{
				DownloadAndCacheBaseTiles(_tilesetId, true);
			}
		}

		protected override RasterTile CreateTile(CanonicalTileId tileId, string tilesetId)
		{
			RasterTile rasterTile;
			//`starts with` is weak and string operations are slow
			//but caching type and using Activator.CreateInstance (or caching func and calling it)  is even slower
			if (tilesetId.StartsWith("mapbox://", StringComparison.Ordinal))
			{
				rasterTile = UseRetina ? new RetinaRasterTile(tileId, tilesetId) : new RasterTile(tileId, tilesetId);
			}
			else
			{
				rasterTile = UseRetina ? new ClassicRetinaRasterTile(tileId, tilesetId) : new ClassicRasterTile(tileId, tilesetId);
			}

#if UNITY_EDITOR
			rasterTile.IsMapboxTile = true;
#endif

			return rasterTile;
		}

		protected override void SetTexture(UnityTile unityTile, RasterTile dataTile)
		{
			if (dataTile.Texture2D != null)
			{
				unityTile.SetRasterData(dataTile);
			}
			else
			{
				unityTile.SetRasterData(dataTile, UseMipMap, UseCompression);
			}
		}

		protected override void ApplyParentTexture(UnityTile tile)
		{
			var parent = tile.UnwrappedTileId.Parent;
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_tilesetId, parent.Canonical);
				if (cacheItem != null && cacheItem.Texture2D != null)
				{
					tile.SetParentTexture(parent, cacheItem.Texture2D);
					break;
				}

				parent = parent.Parent;
			}
		}
	}
}