using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace CustomImageLayerSample
{
	public sealed class CustomImageFactoryManager : ImageFactoryManager
	{
		public string _urlFormat;
		private string CustomTextureFieldName;
		private string CustomTextureScaleOffsetFieldName;

		public CustomImageFactoryManager(IFileSource fileSource, string urlFormat, string tilesetId, bool downloadFallbackImagery, string textureFieldName = "_MainTex", string textureScaleOffsetFieldName = "_MainTex_ST") : base(fileSource, tilesetId, downloadFallbackImagery)
		{
			_urlFormat = urlFormat;
			CustomTextureFieldName = textureFieldName;
			CustomTextureScaleOffsetFieldName = textureScaleOffsetFieldName;

			if (DownloadFallbackImagery)
			{
				DownloadAndCacheBaseTiles(_tilesetId, true);
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
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_tilesetId, parent.Canonical);
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