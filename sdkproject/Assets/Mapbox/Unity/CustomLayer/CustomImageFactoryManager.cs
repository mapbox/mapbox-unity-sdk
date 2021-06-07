using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	public sealed class CustomImageFactoryManager : ImageFactoryManager
	{
		public string _urlFormat;
		private string CustomTextureFieldName;
		private string CustomTextureScaleOffsetFieldName;

		public CustomImageFactoryManager(string urlFormat, ImageryLayerProperties settings, bool downloadFallbackImagery, string textureFieldName = "_MainTex", string textureScaleOffsetFieldName = "_MainTex_ST") : base(settings.sourceOptions, downloadFallbackImagery)
		{
			_urlFormat = urlFormat;
			CustomTextureFieldName = textureFieldName;
			CustomTextureScaleOffsetFieldName = textureScaleOffsetFieldName;

			if (DownloadFallbackImagery)
			{
				DownloadAndCacheBaseTiles(_sourceSettings.Id, true);
			}
		}

		protected override RasterTile CreateTile(CanonicalTileId tileId, string tilesetId)
		{
			return new CustomImageTile(tileId, tilesetId, _urlFormat);
		}

		protected override void SetTexture(UnityTile unityTile, RasterTile dataTile)
		{
			unityTile.MeshRenderer.sharedMaterial.SetTexture(CustomTextureFieldName, dataTile.Texture2D);
			unityTile.MeshRenderer.sharedMaterial.SetVector(CustomTextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
		}

		protected override void ApplyParentTexture(UnityTile tile)
		{
			var parent = tile.UnwrappedTileId.Parent;
			tile.SetParentTexture(parent, null, CustomTextureFieldName, CustomTextureScaleOffsetFieldName);
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_sourceSettings.Id, parent.Canonical);
				if (cacheItem != null && cacheItem.Texture2D != null)
				{
					tile.SetParentTexture(parent, cacheItem.Texture2D, CustomTextureFieldName, CustomTextureScaleOffsetFieldName);
					break;
				}

				parent = parent.Parent;
			}
		}
	}
}