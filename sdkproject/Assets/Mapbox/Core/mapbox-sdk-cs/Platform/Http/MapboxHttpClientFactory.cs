

namespace Mapbox.Experimental.Platform.Http
{

	using System;
	using System.Collections.Concurrent;
	using System.Net;
	using System.Net.Http;
	using System.Threading;

	public interface IMapboxHttpClientFactory : IDisposable
	{
		IMapboxHttpClient Get(string url);
	}


	public class MapboxHttpClientFactory : IMapboxHttpClientFactory
	{

		private readonly ConcurrentDictionary<string, IMapboxHttpClient> _clients = new ConcurrentDictionary<string, IMapboxHttpClient>();


		public IMapboxHttpClient Get(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentNullException("url");
			}

			return
				_clients.AddOrUpdate(
					getCacheKey(url)
					, u => create(u)
					, (u, client) => client.IsDisposed ? create(u) : client
				);
		}


		private string getCacheKey(string url) => new Uri(url).Host;

		private IMapboxHttpClient create(string url) => new MapboxHttpClient();


		public void Dispose()
		{
			foreach (var kv in _clients)
			{
				if (!kv.Value.IsDisposed)
				{
					kv.Value.Dispose();
				}
			}
			_clients.Clear();
		}

	}
}
