
namespace Mapbox.Experimental.Tests.MapboxSdkCs.Platform.Http
{

	using Mapbox.Experimental.Platform.Http;
	using NUnit.Framework;
	using System.Threading.Tasks;

	[TestFixture]
	internal class MapboxHttp
	{


		private MapboxWebDataFetcher _fetcher;

		[OneTimeSetUp]
		public void Init()
		{
			// this fetcher does no caching
			_fetcher = new MapboxWebDataFetcher(
				Mapbox.Unity.MapboxAccess.Instance.Configuration.DefaultTimeout
				, Mapbox.Unity.MapboxAccess.Instance.Configuration.AccessToken
				, false
			);
		}


		[Test]
		public async Task SimpleHttp()
		{

			MapboxHttpRequest request = _fetcher.GetRequest("http://www.google.com");
			await request.SendAsync("myId", HttpMethod.Get);
		}



	}
}
