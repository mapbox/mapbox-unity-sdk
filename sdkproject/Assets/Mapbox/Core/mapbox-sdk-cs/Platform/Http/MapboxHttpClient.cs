

namespace Mapbox.Experimental.Platform.Http
{

	using System;
	using System.Net;
	using System.Net.Http;
	using System.Threading;


	public interface IMapboxHttpClient : IDisposable
	{
		HttpClient HttpClient { get; }
		string BaseUrl { get; set; }
		MapboxHttpRequest Request(string url, int timeoutSeconds, string accessToken);

		bool IsDisposed { get; }
	}


	public class MapboxHttpClient : IMapboxHttpClient, IDisposable
	{

		private readonly Lazy<HttpClient> _client;

		public MapboxHttpClient(string baseUrl = null)
		{

			UnityEngine.Debug.LogWarning("MAPBOX_EXPERIMENTAL TODO: add proxy - settings??");

			BaseUrl = baseUrl;

			// DIRTY HACK! to get around missing Mono certificates:
			// System.Net.WebException: Error: SecureChannelFailure (The authentication or decryption has failed.) ---> System.IO.IOException: The authentication or decryption has failed. ---> System.IO.IOException: The authentication or decryption has failed. ---> Mono.Security.Protocol.Tls.TlsException: The authentication or decryption has failed.
			// with newer Mono versions (+3.12.0) that *shouldn't* be necessary anymore:
			// http://www.mono-project.com/docs/about-mono/releases/3.12.0/#cert-sync
			ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;

			//ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
			CookieContainer cookies = new CookieContainer();

			_client = new Lazy<HttpClient>(() =>
			new HttpClient(new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				//AutomaticRedirection = true, // not available
				CookieContainer = cookies,

				//not implemented??
				//MaxConnectionsPerServer = 2,

				//UseProxy = true,
				//Proxy = new WebProxy("192.168.1.125", 8888)
				Proxy = WebRequest.DefaultWebProxy
			}
			)
			{
				// we set timeout per request using a CancellationToken
				Timeout = Timeout.InfiniteTimeSpan
			});

			//_client.DefaultRequestHeaders.Add("Accept", "text/html, application/json");
			//_client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36");

			// DOES NOT WORK "/0" is the problem
			//_client.Value.DefaultRequestHeaders.Add("User-Agent", "unity.Mapbox.MBX SDK/1.4.3/0 MapboxEventsUnityEditor/1.4.3");
			// works! replaced with " 0"
			//_client.Value.DefaultRequestHeaders.Add("User-Agent", "unity.Mapbox.MBX SDK/1.4.3 0 MapboxEventsUnityEditor/1.4.3");

			//_client.DefaultRequestHeaders.Add("Accept", "text/html, application/json");
			//_client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

		}

		public string BaseUrl { get; set; }


		public HttpClient HttpClient => _client.Value;

		public MapboxHttpRequest Request(string url, int timeoutSeconds, string accessToken)
		{
			return new MapboxHttpRequest(url, timeoutSeconds, accessToken);
		}


		public bool IsDisposed { get; private set; }


		public virtual void Dispose()
		{
			if (IsDisposed) { return; }

			//todo for httpmessagehandler
			if (_client.IsValueCreated) { _client.Value.Dispose(); }

			IsDisposed = true;
		}

	}
}
