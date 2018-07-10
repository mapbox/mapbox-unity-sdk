

namespace Mapbox.Tokens
{
	using Mapbox.Platform;
	using System;
	using System.ComponentModel;
#if MAPBOX_EXPERIMENTAL
	using System.Threading.Tasks;
	using Mapbox.Experimental.Platform.Http;
	using Mapbox.Unity;
#endif


	public enum MapboxTokenStatus
	{
		/// <summary>The token is valid and active </summary>
		[Description("The token is valid and active")]
		TokenValid,
		/// <summary>the token can not be parsed </summary>
		[Description("the token can not be parsed")]
		TokenMalformed,
		/// <summary>the signature for the token does not validate </summary>
		[Description("the signature for the token does not validate")]
		TokenInvalid,
		/// <summary> the token was temporary and expired</summary>
		[Description("the token was temporary and expired")]
		TokenExpired,
		/// <summary>the token's authorization has been revoked </summary>
		[Description("the token's authorization has been revoked")]
		TokenRevoked,
		/// <summary>inital value </summary>
		StatusNotYetSet
	}


	/// <summary>
	/// Wrapper class to retrieve details about a token
	/// </summary>
	public class MapboxTokenApi
	{

#if MAPBOX_EXPERIMENTAL
		private MapboxAccess _mapboxAccess;
#endif

		// use internal FileSource without(!) passing access token from config into constructor
		// otherwise access token would be appended to url twice
		// https://www.mapbox.com/api-documentation/#retrieve-a-token
		// if we should ever implement other API methods: creating, deleting, updating ... tokens
		// we will need another FileSource with the token from the config
		private FileSource _fs = new FileSource();


#if MAPBOX_EXPERIMENTAL
		public MapboxTokenApi(MapboxAccess mapboxAccess)
		{
			_mapboxAccess = mapboxAccess;
		}
#endif

		public MapboxTokenApi() { }



#if MAPBOX_EXPERIMENTAL
		public async Task<MapboxToken> Retrieve(string accessToken)
		{
			// we can use the request provided by `MapboxAccess.Request()' as
			// the underlying 'MapboxHttpRequest' takes care of **not** appending
			// the token from the settings for 'MapboxWebDataRequestType.Token' requests
			// nevertheless: for creating, updating, ... tokens another implementation
			// will be necessary
			string url = $"{Utils.Constants.BaseAPI}tokens/v2?access_token={accessToken}";
			MapboxHttpRequest request = await _mapboxAccess.Request(
				MapboxWebDataRequestType.Token
				, null
				, MapboxHttpMethod.Get
				, url
			);
			MapboxHttpResponse response = await request.GetResponseAsync();
			if (response.HasError)
			{
				return new MapboxToken
				{
					HasError = true,
					ErrorMessage = response.ExceptionsAsString
				};
			}

			return MapboxToken.FromResponseData(response.Data);
		}
#endif

		public void Retrieve(string accessToken, Action<MapboxToken> callback)
		{
			_fs.Request(
				Utils.Constants.BaseAPI + "tokens/v2?access_token=" + accessToken,
				(Response response) =>
				{
					if (response.HasError)
					{
						callback(new MapboxToken()
						{
							HasError = true,
							ErrorMessage = response.ExceptionsAsString
						});
						return;

					}
					callback(MapboxToken.FromResponseData(response.Data));
				}
			);
		}



	}
}
