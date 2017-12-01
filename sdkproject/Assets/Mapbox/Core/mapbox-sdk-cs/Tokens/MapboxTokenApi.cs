

namespace Mapbox.Tokens
{
	using Mapbox.Platform;
	using System.ComponentModel;


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
		TokenRevoked
	}


	public class MapboxTokenApi
	{

		public MapboxTokenApi(FileSource fs)
		{
			_fs = fs;
		}


		private FileSource _fs;


		public MapboxToken Retrieve(string accessToken)
		{

			byte[] data = null;
			_fs.Request(
				Utils.Constants.BaseAPI + "tokens/v2?access_token=" + accessToken,
				(Response response) =>
				{
					if (null != response.Data && !response.HasError)
					{
						data = response.Data;
					}
				}
			);

			_fs.WaitForAllRequests();


			return MapboxToken.FromResponseData(data);
		}

	}
}