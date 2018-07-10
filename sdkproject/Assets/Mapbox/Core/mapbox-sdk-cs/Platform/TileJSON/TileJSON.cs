
namespace Mapbox.Platform.TilesetTileJSON
{


	using Mapbox.Json;
	using Mapbox.Utils;
	using System;
	using System.Collections.Generic;
	using System.Text;
#if MAPBOX_EXPERIMENTAL
	using Mapbox.Unity;
	using System.Threading.Tasks;
	using Mapbox.Experimental.Platform.Http;
#endif


	public class TileJSON
	{

		private IFileSource _fileSource;
		private int _timeout;
#if MAPBOX_EXPERIMENTAL
		private MapboxAccess _mapboxAccess;
#endif


		public IFileSource FileSource { get { return _fileSource; } }

#if MAPBOX_EXPERIMENTAL
		public TileJSON(MapboxAccess mapboxAccess)
		{
			_mapboxAccess = mapboxAccess;
		}
#endif

		public TileJSON(IFileSource fileSource, int timeout)
		{
			_fileSource = fileSource;
			_timeout = timeout;
		}


#if MAPBOX_EXPERIMENTAL
		public async Task<TileJSONResponse> Get(string tilesetName)
		{
			string url = string.Format(
				"{0}v4/{1}.json?secure"
				, Mapbox.Utils.Constants.BaseAPI
				, tilesetName
			);

			MapboxHttpRequest request = await _mapboxAccess.Request(
				MapboxWebDataRequestType.TileJson
				, null
				, MapboxHttpMethod.Get
				, url
			);
			MapboxHttpResponse response = await request.GetResponseAsync();
			string jsonTxt = Encoding.UTF8.GetString(response.Data);
			TileJSONResponse tileJSONResponse = JsonConvert.DeserializeObject<TileJSONResponse>(jsonTxt);
			if (null != tileJSONResponse) { tileJSONResponse.Source = tilesetName; }

			return tileJSONResponse;
		}
#endif

		public IAsyncRequest Get(string tilesetName, Action<TileJSONResponse> callback)
		{
			string url = string.Format(
				"{0}v4/{1}.json?secure"
				, Mapbox.Utils.Constants.BaseAPI
				, tilesetName
			);

			return _fileSource.Request(
				url
				, (Response response) =>
				{
					string json = Encoding.UTF8.GetString(response.Data);
					TileJSONResponse tileJSONResponse = JsonConvert.DeserializeObject<TileJSONResponse>(json);
					if (tileJSONResponse != null)
					{
						tileJSONResponse.Source = tilesetName;
					}
					callback(tileJSONResponse);
				}
				, _timeout
			);
		}




	}
}
