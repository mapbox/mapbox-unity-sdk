using Mapbox.Json;
using Mapbox.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mapbox.Platform.TilesetTileJSON
{
	public class TileJSON
	{

		private IFileSource _fileSource;
		private int _timeout;


		public IFileSource FileSource { get { return _fileSource; } }


		public TileJSON(IFileSource fileSource, int timeout)
		{
			_fileSource = fileSource;
			_timeout = timeout;
		}


		public IAsyncRequest Get(string tilesetName, Action<TileJSONResponse> callback)
		{
			string url = string.Format(
				"{0}v4/{1}.json?secure"
				, Constants.BaseAPI
				, tilesetName
			);

			return _fileSource.Request(
				url
				, (Response response) =>
				{
					string json = Encoding.UTF8.GetString(response.Data);
					TileJSONResponse tileJSONResponse = JsonConvert.DeserializeObject<TileJSONResponse>(json);
					callback(tileJSONResponse);
				}
				, _timeout
			);
		}




	}
}
