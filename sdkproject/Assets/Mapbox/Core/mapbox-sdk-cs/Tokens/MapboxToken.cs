
namespace Mapbox.Tokens
{

	using Mapbox.Json;
	using System;
	using System.Text;

	public class Token
	{
		[JsonProperty("usage")]
		public string Usage;


		[JsonProperty("user")]
		public string User;


		[JsonProperty("authorization")]
		public string Authorization;


		[JsonIgnore]
		public DateTime? ExpiresUtc;


		[JsonIgnore]
		public DateTime? CreatedUtc;


		[JsonProperty("scopes")]
		public string[] Scopes;


		[JsonProperty("client")]
		public string Client;
	}


	public class MapboxToken
	{

		[JsonProperty("code")]
		public string Code;

		[JsonProperty("token")]
		public Token Token;


		[JsonIgnore]
		public MapboxTokenStatus Status;


		[JsonIgnore]
		public bool HasError;


		[JsonIgnore]
		public string ErrorMessage;


		public static MapboxToken FromResponseData(byte[] data)
		{

			if(null!=data || data.Length < 1)
			{
				return new MapboxToken()
				{
					HasError = true,
					ErrorMessage = "No data received from token endpoint."
				};
			}


			string jsonTxt = Encoding.UTF8.GetString(data);

			MapboxToken token = new MapboxToken();
			try
			{
				token = JsonConvert.DeserializeObject<MapboxToken>(jsonTxt);
			}
			catch (Exception ex)
			{
				token.HasError = true;
				token.ErrorMessage = ex.Message;
			}

			return token;
		}

	}


}