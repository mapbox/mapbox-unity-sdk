using System;
using Mapbox.Map;
using Mapbox.Platform.Cache;
using Mapbox.Unity.CustomLayer;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.DataFetching
{
	public class FileDataFetcher : ImageDataFetcher
	{
		public void FetchData(FileImageTile tile, string tilesetId, CanonicalTileId tileId, bool useRetina, Action<TextureCacheItem> callback)
		{
			EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
			{
				Callback = () =>
				{
					FetchingCallback(tileId, tile, null);

					//file cache is using datafetcher queue and items in queue might get cancelled at any time
					//cancelled counts as an error, if a file fetching order is cancelled
					//setting error flag here is important so cache manager won't follow up with
					//sql queries for file details like etag

					var textureCacheItem = new TextureCacheItem
					{
						TileId = tileId,
						TilesetId = tilesetId,
						Texture2D = tile.Texture2D,
						FilePath = tile.FilePath,
						HasError = tile.CurrentTileState == TileState.Canceled
					};

					callback(textureCacheItem);
				}
			});
		}

		protected override void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
		{
			if (unityTile != null && !unityTile.ContainsDataTile(rasterTile))
			{
				return;
			}

			if (rasterTile.HasError)
			{
				FetchingError(unityTile, rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), unityTile, rasterTile.Exceptions));
			}
			else
			{

				rasterTile.ExtractTextureFromRequest();

#if UNITY_EDITOR
				if (rasterTile.Texture2D != null)
				{
					rasterTile.Texture2D.name = string.Format("{0}_{1}", tileId.ToString(), rasterTile.TilesetId);
				}
#endif

				if (rasterTile.StatusCode != 304) //NOT MODIFIED
				{
					TextureReceived(unityTile, rasterTile);
				}
			}
		}
	}
}