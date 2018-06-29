namespace Mapbox.Experimental.Platform.Http
{

	using System;


	public static class MapboxHttp
	{

		private static readonly object _configLock = new object();

		private static Lazy<MapboxHttpClientFactory> _factory = new Lazy<MapboxHttpClientFactory>(() => new MapboxHttpClientFactory());


		public static MapboxHttpClientFactory HttpClientFactory => _factory.Value;


		/// <summary>Global HttpSettings</summary>
		public static void ConfigureGlobal() { throw new NotImplementedException(); }


		/// <summary>
		/// Settings for *all* calls to the host of the url
		/// </summary>
		/// <param name="url"></param>
		public static void ConfigureClient(string url) { throw new NotImplementedException(); }
	}

}
