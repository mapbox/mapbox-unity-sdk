

namespace Mapbox.Experimental.Platform.Http
{
	using System;
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;

	public interface IMapboxHttpRequest
	{
		IMapboxHttpClient Client { get; set; }
		string Url { get; set; }
		HttpMethod Verb { get; }
		Task<MapboxHttpResponse> SendAsync(
			object id
			, HttpMethod verb
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


		public string Url { get; set; }


		public HttpMethod Verb { get; private set; }


		public void Cancel()
		{
			if (null == _cancellationTokenSource) { return; }
			_cancellationTokenSource.Cancel();
		}

		public async Task<MapboxHttpResponse> SendAsync(
			object id
			, HttpMethod verb
			, HttpContent content = null
			, Dictionary<string, string> headers = null
			/*, CancellationToken? cancellationToken = null*/
			, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead
			)
		{

			Verb = verb;

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
			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = verb,
				Content = content,
				RequestUri = uriBuilder.Uri
			};

			if (null != headers)
			{
				foreach (var hdr in headers)
				{
					request.Headers.Add(hdr.Key, hdr.Value);
				}
			}


			var userToken = _cancellationTokenSource.Token; // cancellationToken ?? CancellationToken.None;

			var cts = CancellationTokenSource.CreateLinkedTokenSource(userToken);
			cts.CancelAfter(_timeOutSeconds * 1000);
			var token = cts.Token;
			MapboxHttpResponse response = null;
			try
			{
				using (HttpResponseMessage resp = await Client.HttpClient.SendAsync(request, completionOption, token).ConfigureAwait(false))
				{
					response = await MapboxHttpResponse.FromWebResponse(this, resp, null);
					return response;
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError(ex);
				if (ex is OperationCanceledException && !token.IsCancellationRequested)
				{
					response = await MapboxHttpResponse.FromWebResponse(this, null, new TimeoutException());
				}
				else
				{
					response = await MapboxHttpResponse.FromWebResponse(this, null, ex);
				}
				return response;
			}
			finally
			{
				request.Dispose();
				request = null;

				MapboxHttpResponseReceivedEventArgs args = new MapboxHttpResponseReceivedEventArgs(id, response);
				ResponseReveived?.Invoke(this, args);
			}
		}







	}
}
