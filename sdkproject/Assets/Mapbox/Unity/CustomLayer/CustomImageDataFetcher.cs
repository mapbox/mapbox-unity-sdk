using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;

namespace CustomImageLayerSample
{
	public class CustomImageDataFetcher : ImageDataFetcher
	{
		private string _urlFormat;

		public CustomImageDataFetcher(string format)
		{
			_urlFormat = format;
		}

		protected override void CreateWebRequest(string tilesetId, CanonicalTileId tileId, bool useRetina, string etag, UnityTile unityTile = null)
		{
			var rasterTile = new CustomImageTile(_urlFormat);

			if (unityTile != null)
			{
				unityTile.AddTile(rasterTile);
			}

			EnqueueForFetching(new FetchInfo()
			{
				TileId = tileId,
				TilesetId = tilesetId,
				RasterTile = rasterTile,
				ETag = etag,
				Callback = () => { FetchingCallback(tileId, rasterTile, unityTile); }
			});
		}
	}
}