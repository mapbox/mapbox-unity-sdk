

namespace Mapbox.Experimental.Platform.Http
{


	using System;
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;


	/// <summary>
	/// <para>ATTENTION: this is a workaround for Unity tests not being able to access System.Net.Http.HttpMethod.</para>
	/// <para>Hopefully this can be removed some time in the future when Unit has consistent NetStandard2.0 support everywhere.</para>
	public enum MapboxHttpMethod
	{
		Get,
		Post,
		Put,
		Head
	}
	public interface IMapboxHttpRequest
	{
		IMapboxHttpClient Client { get; set; }
		string Url { get; }
		MapboxHttpResponse Response { get; }
		MapboxHttpMethod Verb { get; }

		/// <summary>
		/// Type of request: Tile, Geocoding, TileJson, ...
		/// </summary>
		MapboxWebDataRequestType WebDataRequestType { get; }

		Task<MapboxHttpResponse> SendAsync(
			MapboxWebDataRequestType webDataRequestType
			, object id
			, MapboxHttpMethod verb
			, HttpContent content = null
			, Dictionary<string, string> headers = null
			//, CancellationToken? cancellationToken = null
			, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead
		);
	}

	public class MapboxHttpRequest : IMapboxHttpRequest
	{

		public event EventHandler<MapboxHttpResponseReceivedEventArgs> ResponseReveived;


		private static IMapboxHttpClient _client = null;
		private CancellationTokenSource _cancellationTokenSource;
		private string _accessToken;
		private int _timeOutSeconds;

		public MapboxHttpRequest(string url, int timeoutSeconds, string accessToken)
		{
			Url = url;
			_timeOutSeconds = timeoutSeconds;
			_accessToken = accessToken;
			_cancellationTokenSource = new CancellationTokenSource();
		}


		//_client.DefaultRequestHeaders.Add("Accept", "text/html, application/json");
		//_client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36");
		//_client.DefaultRequestHeaders.Add("Accept", "text/html, application/json");
		//_client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");


		public IMapboxHttpClient Client
		{
			get
			{
				return (null != _client) ? Client :
			   (!string.IsNullOrWhiteSpace(Url)) ? MapboxHttp.HttpClientFactory.Get(Url) :
			   null;
			}
			set { _client = value; }
		}


		public string Url { get; private set; }

		public MapboxWebDataRequestType WebDataRequestType { get; private set; }


		public MapboxHttpResponse Response { get; private set; }

		public MapboxHttpMethod Verb { get; private set; }


		public void Cancel()
		{
			if (null == _cancellationTokenSource) { return; }
			_cancellationTokenSource.Cancel();
		}


		public async Task<MapboxHttpResponse> SendAsync(
			MapboxWebDataRequestType webDataRequestType
			, object id
			, MapboxHttpMethod mbxVerb
			, HttpContent content = null
			, Dictionary<string, string> headers = null
			/*, CancellationToken? cancellationToken = null*/
			, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead
			)
		{

			Response = null;
			Verb = mbxVerb;
			WebDataRequestType = webDataRequestType;

			HttpMethod httpVerb;
			//TODO: remove once System.Net.Http.HttpMethod is accessible in Unity tests
			switch (mbxVerb)
			{
				case MapboxHttpMethod.Get:
					httpVerb = HttpMethod.Get;
					break;
				case MapboxHttpMethod.Post:
					httpVerb = HttpMethod.Post;
					break;
				case MapboxHttpMethod.Put:
					httpVerb = HttpMethod.Put;
					break;
				case MapboxHttpMethod.Head:
					httpVerb = HttpMethod.Head;
					break;
				default:
					httpVerb = HttpMethod.Get;
					break;
			}


			string accessTokenQuery = $"&access_token={_accessToken}";
			UriBuilder uriBuilder = new UriBuilder(Url);
			if (!string.IsNullOrWhiteSpace(uriBuilder.Query) && uriBuilder.Query.Length > 1)
			{
				uriBuilder.Query = $"{uriBuilder.Query.Substring(1)}&{accessTokenQuery}";
			}
			else
			{
				uriBuilder.Query = accessTokenQuery;
			}
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage
			{
				Method = httpVerb,
				Content = content,
				RequestUri = uriBuilder.Uri
			};

			if (null != headers)
			{
				foreach (var hdr in headers)
				{
					httpRequestMessage.Headers.Add(hdr.Key, hdr.Value);
				}
			}


			var userToken = _cancellationTokenSource.Token; // cancellationToken ?? CancellationToken.None;

			var cts = CancellationTokenSource.CreateLinkedTokenSource(userToken);
			cts.CancelAfter(_timeOutSeconds * 1000);
			var token = cts.Token;
			MapboxHttpResponse mapboxHttpResponse = null;
			HttpResponseMessage httpResponseMessage = null;
			try
			{
				DateTime started = DateTime.UtcNow;
				httpResponseMessage = await Client.HttpClient.SendAsync(httpRequestMessage, completionOption, token).ConfigureAwait(false);
				mapboxHttpResponse = await MapboxHttpResponse.FromWebResponse(this, httpResponseMessage, null);
				mapboxHttpResponse.StartedUtc = started;
				return mapboxHttpResponse;
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogWarning($"caught exception: {ex}");
				if (ex is OperationCanceledException && !token.IsCancellationRequested)
				{
					mapboxHttpResponse = await MapboxHttpResponse.FromWebResponse(this, httpResponseMessage, new TimeoutException());
				}
				else
				{
					mapboxHttpResponse = await MapboxHttpResponse.FromWebResponse(this, httpResponseMessage, ex);
				}
				return mapboxHttpResponse;
			}
			finally
			{
				if (null != mapboxHttpResponse) { mapboxHttpResponse.EndedUtc = DateTime.UtcNow; }
				if (null != httpResponseMessage)
				{
					httpResponseMessage.Dispose();
					httpResponseMessage = null;
				}

				httpRequestMessage.Dispose();
				httpRequestMessage = null;

				Response = mapboxHttpResponse;
				MapboxHttpResponseReceivedEventArgs args = new MapboxHttpResponseReceivedEventArgs(id, mapboxHttpResponse);
				ResponseReveived?.Invoke(this, args);
			}
		}


		public async Task<MapboxHttpResponse> GetResponseAsync()
		{
			while (null == Response) { await Task.Delay(1); }
			return Response;
		}




	}
}
