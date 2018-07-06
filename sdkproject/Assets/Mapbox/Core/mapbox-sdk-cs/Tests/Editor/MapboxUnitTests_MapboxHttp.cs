
namespace Mapbox.Experimental.Tests.MapboxSdkCs.Platform.Http
{

	using Mapbox.Experimental.Platform.Http;
	using Mapbox.Map;
	using Mapbox.Utils;
	using NUnit.Framework;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using UnityEngine;
	using UnityEngine.TestTools;
	using static System.FormattableString;

	[TestFixture]
	internal class MapboxHttp
	{

		private MapboxWebDataFetcher _fetcher;
		private static readonly object _lock = new object();
		private long _responseEventsCount = 0;
		private string _className;
		private string _singlePbfUrl = "https://a.tiles.mapbox.com/v4/mapbox.mapbox-terrain-v2,mapbox.mapbox-streets-v7/10/545/361.vector.pbf";

		[OneTimeSetUp]
		public void Init()
		{
			_className = this.GetType().Name;
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("**************************************************");
			sb.AppendLine("**************************************************");
			sb.AppendLine("**************************************************");
			sb.AppendLine("**************************************************");
			sb.AppendLine("              ATTENTION!!!!!");
			sb.AppendLine($"{_className}: seems Unity does not yet support 'async Task' test methods.");
			sb.AppendLine("These tests use a workaround via: 'Action asyncWorkaround = async () => { }'.");
			sb.AppendLine("This might make Unity stuck if an unexpected exception occurs during the tests.");
			sb.AppendLine("**************************************************");
			sb.AppendLine("**************************************************");
			sb.AppendLine("**************************************************");
			sb.AppendLine("**************************************************");

			Debug.LogWarning(sb.ToString());

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



		private void fetcher_ResponseReveived(object sender, MapboxWebDataFetcherResponseReceivedEventArgs e)
		{
			// take care of responses arriving in quick succession
			lock (_lock) { _responseEventsCount++; }
			MapboxHttpRequest request = sender as MapboxHttpRequest;
			Debug.Log($"response event received for request {request.Url}, requests in queue/executing:{e.RequestsInQueue}/{e.RequestsExecuting}");
		}


		[UnityTest]
		public IEnumerator SimpleHttpRequest()
		{
			bool running = true;

			Action asyncWorkaround = async () =>
			{
				try
				{
					MapboxHttpRequest request = await _fetcher.GetRequestAsync(MapboxWebDataRequestType.Tile, "myId", MapboxHttpMethod.Get, "http://www.mapbox.com");
					MapboxHttpResponse response = await request.GetResponseAsync();
					commonResponseTests(response);
				}
				finally { running = false; }
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}



		[UnityTest]
		public IEnumerator PbfRequest()
		{
			bool running = true;

			Action asyncWorkaround = async () =>
			{
				try
				{
					MapboxHttpRequest request = await _fetcher.GetRequestAsync(MapboxWebDataRequestType.Tile, "pbf", MapboxHttpMethod.Get, _singlePbfUrl);
					MapboxHttpResponse response = null;
					while (null == (response = request.Response))
					{
						await Task.Delay(100);
					}
					commonResponseTests(response);

					// hmmm: no content-type???????
					foreach (var hdr in response.Headers)
					{
						Debug.Log($"{hdr.Key} : {hdr.Value}");
					}
				}
				finally { running = false; }
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}


		[UnityTest]
		public IEnumerator PbfRequestWithEvent()
		{
			bool running = true;
			string requestId = "my-unique-pbf-request-id";

			EventHandler<MapboxWebDataFetcherResponseReceivedEventArgs> handler = (sender, evtArgs) =>
			{
				MapboxHttpResponse response = evtArgs.ResponseEventArgs.Response;
				Assert.AreEqual(requestId, evtArgs.ResponseEventArgs.Id);
				commonResponseTests(response);
				running = false;
			};
			_fetcher.ResponseReveived += handler;

			Action asyncWorkaround = async () =>
			{
				MapboxHttpRequest request = await _fetcher.GetRequestAsync(MapboxWebDataRequestType.Tile, requestId, MapboxHttpMethod.Get, _singlePbfUrl);
			};
			asyncWorkaround();

			while (running) { yield return null; }

			_fetcher.ResponseReveived -= handler;
		}


		[UnityTest]
		public IEnumerator Cancel()
		{
			bool running = true;

			Action asyncWorkaround = async () =>
			{
				try
				{
					MapboxHttpRequest request = await _fetcher.GetRequestAsync(MapboxWebDataRequestType.Tile, "pbf", MapboxHttpMethod.Get, "https://a.tiles.mapbox.com/v4/mapbox.mapbox-terrain-v2,mapbox.mapbox-streets-v7/10/545/361.vector.pbf");
					Task<MapboxHttpResponse> response = request.GetResponseAsync();
					request.Cancel();
					await response;

					Assert.IsTrue(response.Result.HasError);
					Assert.IsTrue(response.Result.Exceptions.Any(e => e is TaskCanceledException), "response doesn't contain 'TaskCanceledException'");
				}
				finally { running = false; }
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}


		[UnityTest]
		public IEnumerator Parallel()
		{
			bool running = true;

			Action asyncWorkaround = async () =>
			{
				try
				{
					_responseEventsCount = 0;

					Vector2d sw = new Vector2d(48.21659, 16.39010);
					Vector2d ne = new Vector2d(48.21970, 16.39376);
					Vector2dBounds bounds = new Vector2dBounds(sw, ne);
					HashSet<CanonicalTileId> tileIds = TileCover.Get(bounds, 18);
					Task<MapboxHttpResponse>[] downloads = new Task<MapboxHttpResponse>[tileIds.Count];
					int idCnt = 0;

					// try to force no caching. not all participating parties might adhere: OS, ISP, ...
					Dictionary<string, string> headers = new Dictionary<string, string>() { { "Cache-Control", "max-age=0, no-cache, no-store" } };
					foreach (var tileId in tileIds)
					{
						string url = TileResource.MakeRaster(tileId, null).GetUrl();
#if MAPBOX_DEBUG_HTTP
						Debug.Log($"creating request for url:{url}");
#endif
						MapboxHttpRequest request = await _fetcher.GetRequestAsync(MapboxWebDataRequestType.Tile, tileId, MapboxHttpMethod.Get, url, headers: headers);
						downloads[idCnt] = request.GetResponseAsync();
						idCnt++;
					}

					MapboxHttpResponse[] responses = await Task.WhenAll(downloads);

					Assert.AreEqual(tileIds.Count, responses.Length);
					Assert.AreEqual(tileIds.Count, _responseEventsCount);

					double sumDurations = 0.0d;
					DateTime start = DateTime.MaxValue;
					DateTime end = DateTime.MinValue;
					foreach (var response in responses)
					{
						commonResponseTests(response);
						Debug.Log($"{response.RequestUrl}: {response.StartedUtc:HH:mm:ss.fff} -> {response.EndedUtc:HH:mm:ss.fff}: {response.Duration}");
						start = response.StartedUtc.Value < start ? response.StartedUtc.Value : start;
						end = response.EndedUtc.Value > end ? response.EndedUtc.Value : end;
						sumDurations += response.Duration.Value.TotalMilliseconds;
					}

					double duration = (end - start).TotalMilliseconds;
					Debug.Log(Invariant($"first request started:{start:HH:mm:ss.fff} last request ended:{end:HH:mm:ss.fff}"));
					Debug.Log(Invariant($"duration first request started to last request ended[ms]:{duration:0.000}"));
					Debug.Log(Invariant($"duration∑ of requests[ms]:{sumDurations:0.000}"));
					// assumption: if requests are parallel the duration between start of first request
					// and end of last request should be less than the sum of durations of all requests executing.
					// however, this test might fail if it is run several times in short succession
					// and tiles get most likely cached somewhere in the middle (OS, proxy, ISP, ...)
					// despite the 'Cache-Control' header we set above.
					Assert.Less(duration, sumDurations, "requests did not run in parallel");
				}
				finally { running = false; }
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}



		[UnityTest]
		public IEnumerator CancelAll()
		{
			bool running = true;

			Action asyncWorkaround = async () =>
			{
				try
				{
					_responseEventsCount = 0;

					Vector2d sw = new Vector2d(48.21659, 16.39010);
					Vector2d ne = new Vector2d(48.21970, 16.39376);
					Vector2dBounds bounds = new Vector2dBounds(sw, ne);
					HashSet<CanonicalTileId> tileIds = TileCover.Get(bounds, 18);
					Task<MapboxHttpResponse>[] downloads = new Task<MapboxHttpResponse>[tileIds.Count];
					int idCnt = 0;

					// try to force no caching. not all participating parties might adhere: OS, ISP, ...
					Dictionary<string, string> headers = new Dictionary<string, string>() { { "Cache-Control", "max-age=0, no-cache, no-store" } };
					foreach (var tileId in tileIds)
					{
						string url = TileResource.MakeRaster(tileId, null).GetUrl();
#if MAPBOX_DEBUG_HTTP
						Debug.Log($"creating request for url:{url}");
#endif
						MapboxHttpRequest request = await _fetcher.GetRequestAsync(MapboxWebDataRequestType.Tile, tileId, MapboxHttpMethod.Get, url, headers: headers);
						downloads[idCnt] = request.GetResponseAsync();
						request.Cancel();
						idCnt++;
					}

					MapboxHttpResponse[] responses = await Task.WhenAll(downloads);

					Assert.AreEqual(tileIds.Count, responses.Length);
					Assert.AreEqual(tileIds.Count, _responseEventsCount);

					int failedCnt = 0;
					int succeededCnt = 0;
					foreach (var response in responses)
					{
#if MAPBOX_DEBUG_HTTP
						Debug.Log($"{response.RequestUrl}: hasError->{response.HasError} statusCode:{response.StatusCode}");
#endif
						if (response.HasError)
						{
							failedCnt++;
						}
						else
						{
							succeededCnt++;
						}
					}

					// check if at least more requests failed than succeeded: Mapbox API is faassst ;-)
					Assert.GreaterOrEqual(failedCnt, succeededCnt, "unexpected result: more requests succeeded than failed");
				}
				finally { running = false; }
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}



		private void commonResponseTests(MapboxHttpResponse response)
		{
			//Debug.Log($"status:{response.StatusCode} data.length:{(response.Data == null ? "NULL" : response.Data.Length.ToString())}");
			Assert.IsTrue(response.StatusCode.HasValue, "reponse StatusCode does not have a value set");
			Assert.AreEqual(200, response.StatusCode.Value, "response StatusCode indicates failure");
			Assert.NotNull(response.Data, "no data received");
			Assert.AreNotEqual(0, response.Data.Length, "empty data received");
			Assert.IsNull(response.Exceptions, "response has unexpected exceptions");
		}




	}
}
