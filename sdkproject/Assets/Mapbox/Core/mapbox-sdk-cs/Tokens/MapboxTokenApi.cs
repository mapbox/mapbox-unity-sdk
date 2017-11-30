

namespace Mapbox.Tokens
{

	using System.ComponentModel;


	public enum MapboxTokenStatus
	{
		[Description("")]
		TokenValid,
		TokenMalformed,
		TokenInvalid,
		TokenExpired,
		TokenRevoked
	}


	public class MapboxTokenApi
	{

		public MapboxTokenApi(string access_token)
		{
			_accessToken = access_token;
		}


		private string _accessToken;


		public void Retrieve() { }

	}
}