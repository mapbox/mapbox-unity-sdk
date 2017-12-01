
namespace Mapbox.Tokens
{

	using Mapbox.Json;
	using System;
	using System.Text;


	/// <summary>
	/// Mapbox Token: https://www.mapbox.com/api-documentation/#retrieve-a-token
	/// </summary>
	public class MapboxToken
	{

		/// <summary>String representation of the token' status </summary>
		[JsonProperty("code")]
		public string Code;


		/// <summary>Token metadata</summary>
		[JsonProperty("token")]
		public TokenMetadata TokenMetadata;


		/// <summary>Parsed token status from 'code'</summary>
		[JsonIgnore]
		public MapboxTokenStatus Status = MapboxTokenStatus.StatusNotYetSet;


		/// <summary>True if there was an error during requesting or parsing the token</summary>
		[JsonIgnore]
		public bool HasError;


		/// <summary>Error message if the token could not be requested or parsed</summary>
		[JsonIgnore]
		public string ErrorMessage;


		public static MapboxToken FromResponseData(byte[] data)
		{

			if (null == data || data.Length < 1)
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

				MapboxTokenStatus status = (MapboxTokenStatus)Enum.Parse(typeof(MapboxTokenStatus), token.Code);
				if (!Enum.IsDefined(typeof(MapboxTokenStatus), status))
				{
					throw new Exception(string.Format("could not convert token.code '{0}' to MapboxTokenStatus", token.Code));
				}

				token.Status = status;
			}
			catch (Exception ex)
			{
				token.HasError = true;
				token.ErrorMessage = ex.Message;
			}

			return token;
		}
	}





	/// <summary>
	/// Every token has a metadata object that contains information about the capabilities of the token.
	/// https://www.mapbox.com/api-documentation/#the-token-metadata-object
	/// </summary>
	public class TokenMetadata
	{

		/// <summary>the identifier for the token</summary>
		[JsonProperty("id")]
		public string ID;


		/// <summary>the type of token</summary>
		[JsonProperty("usage")]
		public string Usage;


		/// <summary>if the token is a default token</summary>
		[JsonProperty("default")]
		public bool Default;


		/// <summary></summary>
		[JsonProperty("user")]
		public string User;


		/// <summary></summary>
		[JsonProperty("authorization")]
		public string Authorization;


		/// <summary>date and time the token was created</summary>
		[JsonProperty("created")]
		public string Created;


		/// <summary>date and time the token was last modified</summary>
		[JsonProperty("modified")]
		public string Modified;


		/// <summary>array of scopes granted to the token</summary>
		[JsonProperty("scopes")]
		public string[] Scopes;


		/// <summary>the client for the token, always 'api'</summary>
		[JsonProperty("client")]
		public string Client;


		/// <summary>the token itself</summary>
		[JsonProperty("token")]
		public string Token;
	}



}