using System;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.CustomLayer
{
	public sealed class MapboxImageFactoryManager : ImageFactoryManager
	{
		private ImageryLayerProperties _imageSettings;

		public MapboxImageFactoryManager(ImageryLayerProperties imageSettings, bool downloadFallbackImagery) : base(imageSettings.sourceOptions, downloadFallbackImagery)
		{
			_imageSettings = imageSettings;

			if (DownloadFallbackImagery)
			{
				DownloadAndCacheBaseTiles(_sourceSettings.Id, true);
			}
		}

		protected override RasterTile CreateTile(CanonicalTileId tileId, string tilesetId)
		{
			RasterTile rasterTile;
			//`starts with` is weak and string operations are slow
			//but caching type and using Activator.CreateInstance (or caching func and calling it)  is even slower
			if (tilesetId.StartsWith("mapbox://", StringComparison.Ordinal))
			{
				rasterTile = _imageSettings.rasterOptions.useRetina ? new RetinaRasterTile(tileId, tilesetId) : new RasterTile(tileId, tilesetId);
			}
			else
			{
				rasterTile = _imageSettings.rasterOptions.useRetina ? new ClassicRetinaRasterTile(tileId, tilesetId) : new ClassicRasterTile(tileId, tilesetId);
			}

#if UNITY_EDITOR
			rasterTile.IsMapboxTile = true;
#endif

			return rasterTile;
		}

		protected override void SetTexture(UnityTile unityTile, RasterTile dataTile)
		{
			unityTile.SetRasterData(dataTile, _imageSettings.rasterOptions.useMipMap, _imageSettings.rasterOptions.useCompression);
		}
	}
}