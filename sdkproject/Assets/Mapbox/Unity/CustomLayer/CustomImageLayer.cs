using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace CustomImageLayerSample
{
	public class CustomImageLayer : MonoBehaviour
	{
		[SerializeField] private string _customTilesetId = "AerisHeatMap";
		[SerializeField] private string UrlFormat = "https://maps.aerisapi.com/anh3TB1Xu9Wr6cPndbPwF_EuOSGuqkH433UmnajaOP0MD9rpIh5dZ38g2SUwvu/flat,ftemperatures-max-text,admin/{0}/{1}/{2}/current.png";
		private AbstractMap _map;
		private CustomImageDataFetcher _fetcher;
		private string CustomTextureFieldName = "_CustomOne";
		private string CustomTextureScaleOffsetFieldName = "_CustomOne_ST";

		public void Start()
		{
			_map = FindObjectOfType<AbstractMap>();
			_fetcher = new CustomImageDataFetcher(UrlFormat);

			_fetcher.TextureReceived += TextureReceived;
			_fetcher.FetchingError += (tile, rasterTile, TileErrorEventArgs) => { Debug.Log(TileErrorEventArgs.Exceptions); };
			_map.OnTileFinished += LoadTile;
			_map.OnTileDisposing += UnregisterTile;

			DownloadAndCacheBaseTiles(_customTilesetId, true);
		}

		public void DownloadAndCacheBaseTiles(string imageryLayerSourceId, bool rasterOptionsUseRetina)
		{
			var baseImageDataFetcher = new CustomBaseImageDataFetcher(UrlFormat);
			CanonicalTileId tileId;
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					tileId = new CanonicalTileId(2, i, j);
					baseImageDataFetcher.FetchData(imageryLayerSourceId, tileId, true);
				}
			}

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					tileId = new CanonicalTileId(1, i, j);
					baseImageDataFetcher.FetchData(imageryLayerSourceId, tileId, true);
				}
			}

			tileId = new CanonicalTileId(0, 0, 0);
			baseImageDataFetcher.FetchData(imageryLayerSourceId, tileId, true);
		}

		private void TextureReceived(UnityTile tile, RasterTile rasterTile)
		{
			if (tile.CanonicalTileId != rasterTile.Id)
			{
				Debug.Log("wtf");
			}

			if (tile != null)
			{
				tile.CustomDataName = rasterTile.Id.ToString();
				tile.MeshRenderer.sharedMaterial.SetTexture(CustomTextureFieldName, rasterTile.Texture2D);
				tile.MeshRenderer.sharedMaterial.SetVector(CustomTextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
			}
		}

		private void LoadTile(UnityTile tile)
		{
			var tileLocal = tile;
			var tileId = tileLocal.CanonicalTileId;

			ApplyParentTexture(tile);
			_fetcher.FetchData(_customTilesetId, tileId, true, tile);
		}

		protected void UnregisterTile(UnityTile tile)
		{
			tile.MeshRenderer.sharedMaterial.SetTexture(CustomTextureFieldName, null);
			tile.MeshRenderer.sharedMaterial.SetVector(CustomTextureScaleOffsetFieldName, new Vector4(1, 1, 0, 0));
			_fetcher.CancelFetching(tile.UnwrappedTileId, _customTilesetId);
		}

		private void ApplyParentTexture(UnityTile tile)
		{
			var parent = tile.UnwrappedTileId.Parent;
			for (int i = tile.CanonicalTileId.Z - 1; i > 0; i--)
			{
				var cacheItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(_customTilesetId, parent.Canonical);
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