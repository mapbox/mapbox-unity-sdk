
namespace Mapbox.Experimental.Tests.MapboxSdkCs.Platform.Http
{

	using Mapbox.Experimental.Platform.Http;
	using Mapbox.Map;
	using Mapbox.Utils;
	using NUnit.Framework;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using UnityEngine;

	[TestFixture]
	internal class MapboxHttp
	{


		private MapboxWebDataFetcher _fetcher;
		private long _responseEventsCount = 0;

		[OneTimeSetUp]
		public void Init()
		{
			// this fetcher doesn't cache
			_fetcher = new MapboxWebDataFetcher(
				Mapbox.Unity.MapboxAccess.Instance.Configuration.DefaultTimeout
				, Mapbox.Unity.MapboxAccess.Instance.Configuration.AccessToken
				, false
			);
			_fetcher.ResponseReveived += fetcher_ResponseReveived;
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_fetcher.ResponseReveived -= fetcher_ResponseReveived;
			_fetcher.Dispose();
			_fetcher = null;
		}



		private void fetcher_ResponseReveived(object sender, MapboxHttpResponseReceivedEventArgs e)
		{
			_responseEventsCount++;
			MapboxHttpRequest request = sender as MapboxHttpRequest;
			Debug.Log($"response received for request {request.Url}");
		}


		[Test]
		public async void SimpleHttpRequest()
		{
			MapboxHttpRequest request = await _fetcher.GetRequest("http://www.mapbox.com");
			MapboxHttpResponse response = await request.Get("myId");
			commonResponseTests(response);
		}


		[Test]
		public async void PbfRequest()
		{
			MapboxHttpRequest request = await _fetcher.GetRequest("https://a.tiles.mapbox.com/v4/mapbox.mapbox-terrain-v2,mapbox.mapbox-streets-v7/10/545/361.vector.pbf");
			MapboxHttpResponse response = await request.Get("pbf");
			commonResponseTests(response);

			// hmmm: no content-type???????
			foreach (var hdr in response.Headers)
			{
				Debug.Log($"{hdr.Key} : {hdr.Value}");
			}

		}


		[Test]
		public async void Cancel()
		{
			MapboxHttpRequest request = await _fetcher.GetRequest("https://a.tiles.mapbox.com/v4/mapbox.mapbox-terrain-v2,mapbox.mapbox-streets-v7/10/545/361.vector.pbf");
			Task<MapboxHttpResponse> response = request.Get("pbf");
			request.Cancel();
			await response;

			MapboxHttpResponse resp = response.Result;
			Assert.IsTrue(resp.HasError);
			Assert.IsTrue(resp.Exceptions.Any(e => e is TaskCanceledException), "response doesn't contain 'TaskCanceledException'");
		}


		[Test]
		public async void Parallel()
		{
			_responseEventsCount = 0;

			Vector2d sw = new Vector2d(48.21659, 16.39010);
			Vector2d ne = new Vector2d(48.21970, 16.39376);
			Vector2dBounds bounds = new Vector2dBounds(sw, ne);
			HashSet<CanonicalTileId> tileIds = TileCover.Get(bounds, 18);
			Task<MapboxHttpResponse>[] downloads = new Task<MapboxHttpResponse>[tileIds.Count];
			int idCnt = 0;
			foreach (var tileId in tileIds)
			{
				string url = TileResource.MakeRaster(tileId, null).GetUrl();
				Debug.Log($"url:{url}");
				MapboxHttpRequest request = await _fetcher.GetRequest(url);
				downloads[idCnt] = request.Get(tileId);
				idCnt++;
			}

			//Task.WaitAll(downloads);
			MapboxHttpResponse[] responses = await Task.WhenAll(downloads);

			Assert.AreEqual(tileIds.Count, responses.Length);

			// hmmmm, why doesn't this work???
			// maybe automatic disconnect of eventhandlers???
			await Task.Delay(3000);
			Assert.AreEqual(tileIds.Count, _responseEventsCount);
		}



		private void commonResponseTests(MapboxHttpResponse response)
		{
			Debug.Log($"status:{response.StatusCode} data.length:{(response.Data == null ? "NULL" : response.Data.Length.ToString())}");
			Assert.AreEqual(200, response.StatusCode.Value, "request status code indicates failure");
			Assert.NotNull(response.Data, "no data received");
			Assert.AreNotEqual(0, response.Data.Length, "empty data received");
			Assert.AreEqual(string.Empty, response.ExceptionsAsString, "response has exceptions");
		}




	}
}
