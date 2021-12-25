using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	public sealed class CustomImageFactoryManager : ImageFactoryManager
	{
		private string _urlFormat;
		private string CustomTextureFieldName;
		private string CustomTextureScaleOffsetFieldName;
		private int CustomTextureFieldNameID;
		private int CustomTextureScaleOffsetFieldNameID;

		public CustomImageFactoryManager(string urlFormat, ImageryLayerProperties settings, bool downloadFallbackImagery, string textureFieldName = "_MainTex", string textureScaleOffsetFieldName = "_MainTex_ST") : base(settings.sourceOptions, downloadFallbackImagery)
		{
			_urlFormat = urlFormat;
			CustomTextureFieldName = textureFieldName;
			CustomTextureFieldNameID = Shader.PropertyToID(CustomTextureFieldName);

			CustomTextureScaleOffsetFieldName = textureScaleOffsetFieldName;
			CustomTextureScaleOffsetFieldNameID = Shader.PropertyToID(CustomTextureScaleOffsetFieldName);

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

		protected override void ApplyParentTexture(UnityTile unityTile)
		{
			var parent = unityTile.UnwrappedTileId.Parent;
			unityTile.SetParentTexture(parent, null, CustomTextureFieldNameID, CustomTextureScaleOffsetFieldNameID);
			for (int i = unityTile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_sourceSettings.Id, parent.Canonical, true);
				if (cacheItem != null && cacheItem.Texture2D != null)
				{
					unityTile.SetParentTexture(parent, (RasterTile) cacheItem.Tile, CustomTextureFieldNameID, CustomTextureScaleOffsetFieldNameID);
					break;
				}

				parent = parent.Parent;
			}
		}

		public void SetUrlFormat(string urlFormat)
		{
			_urlFormat = urlFormat;
		}

		public void SetMaterialFieldNames(string textureFieldName, string textureScaleOffsetFieldName)
		{
			CustomTextureFieldName = textureFieldName;
			CustomTextureScaleOffsetFieldName = textureScaleOffsetFieldName;
		}
	}
}