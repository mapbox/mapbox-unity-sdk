//-----------------------------------------------------------------------
// <copyright file="BboxToVector2dBoundsConverterTest.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Mapbox.UnitTest {
	using HttpMock;
	using Mapbox.Json;
	using Mapbox.Utils;
	using Mapbox.Utils.JsonConverters;
	using NUnit.Framework;
	using Platform;
	using System;
	using System.Net;


	[TestFixture]
	internal class FileSourceMockApiTest {


		private static readonly string _mockBaseUrl = "http://localhost:2345";
		private FileSource _fs = new FileSource();
		IHttpServer _mockApi;

		private struct _testUrl {
			public static string simpleJson = "/testmock1";
			public static string customStatusCode = "/testmock2";
			public static string rateLimitHit = "/ratelimithit";
			public static string xrateheader = "/xrateheader";
			public static string cancel = "/cancel";
		}

		[OneTimeTearDown]
		public void Finished() {
			if (null == _mockApi) { return; }
			_mockApi.Dispose();
			_mockApi = null;
		}


		[OneTimeSetUp]
		public void SetupMockHttp() {

			var serverFactory = new HttpServerFactory();
			_mockApi = serverFactory.Get(new Uri(_mockBaseUrl)).WithNewContext();

			_mockApi.Start();

			_mockApi.Stub(r => r.Get(_testUrl.simpleJson))
				.Return(@"{""name"":""first test""}")
				.AsContentType("application/json")
				.OK();

			// test status code ('Unavailable For Legal Reasons') not available in .NET HttpStatusCode enum
			_mockApi.Stub(r => r.Get(_testUrl.customStatusCode)).WithStatus((HttpStatusCode)451);

			_mockApi.Stub(r => r.Get(_testUrl.rateLimitHit))
				.Return(string.Empty)
				.WithStatus((HttpStatusCode)429);

			Func<string> wait = delegate () { System.Threading.Thread.Sleep(1000); return string.Empty; };
			_mockApi.Stub(r => r.Get(_testUrl.cancel))
			   .Return(wait)
			   .OK();

			double unixTimestamp = UnixTimestampUtils.To(new DateTime(1981, 12, 2));
			_mockApi.Stub(r => r.Get(_testUrl.xrateheader))
				.AddHeader("X-Rate-Limit-Interval", "60")
				.AddHeader("X-Rate-Limit-Limit", "100000")
				.AddHeader("X-Rate-Limit-Reset", unixTimestamp.ToString())
				.OK();

		}


		[Test]
		public void SimpleJson() {

			_fs.Request(
				_mockBaseUrl + _testUrl.simpleJson
				, (Response r) => {
					Assert.IsTrue(r.StatusCode.HasValue, "mock api did not set status code");
					Assert.AreEqual(200, r.StatusCode, "mock api returned wrong status code");
					Assert.AreEqual("application/json", r.ContentType, "mock api didn't set correct content type");
					Assert.AreEqual(@"{""name"":""first test""}", System.Text.Encoding.UTF8.GetString(r.Data), "mock api returned wrong response");
				}
			);

			_fs.WaitForAllRequests();


			_fs.Request(
				_mockBaseUrl + _testUrl.customStatusCode
				, (Response r) => {
					Assert.IsTrue(r.StatusCode.HasValue, "mock api did not set status code");
					Assert.AreEqual(451, r.StatusCode, "mock api returned wrong status code");
				}
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		public void RateLimitHit() {

			_fs.Request(
				_mockBaseUrl + _testUrl.rateLimitHit
				, (Response r) => {
					Assert.IsTrue(r.StatusCode.HasValue, "request did not set status code");
					Assert.AreEqual(429, r.StatusCode, "request did not set rate limit status code correctly");
					Assert.IsTrue(r.HasError, "request did not set 'HasError'");
					Assert.NotNull(r.Exceptions, "request did not set any exceptions");
					Assert.GreaterOrEqual(r.Exceptions.Count, 1, "request did not set enough exceptions");
				}
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		public void XRateHeaders() {

			_fs.Request(
				_mockBaseUrl + _testUrl.xrateheader
				, (Response r) => {
					Assert.IsTrue(r.XRateLimitInterval.HasValue, "request did not set XRateLimitInterval");
					Assert.IsTrue(r.XRateLimitLimit.HasValue, "request did not set XRateLimitLimit");
					Assert.IsTrue(r.XRateLimitReset.HasValue, "request did not set XRateLimitReset");

					Assert.AreEqual(60, r.XRateLimitInterval.Value, "request did not set XRateLimitInterval value correctly");
					Assert.AreEqual(100000, r.XRateLimitLimit.Value, "request did not set XRateLimitLimit value correctly");
					DateTime dt = new DateTime(1981, 12, 2);
					Assert.AreEqual(dt, r.XRateLimitReset.Value, "request did not set XRateLimitReset value correctly");
				}
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		public void DoesNotExist404() {

			_fs.Request(
				_mockBaseUrl + "/doesnotexist/mvt.pbf"
				, (Response r) => {
					Assert.IsTrue(r.StatusCode.HasValue, "request did not set status code");
					Assert.AreEqual(404, r.StatusCode, "request did not set 404 status code correctly");
					Assert.IsTrue(r.HasError, "request did not set 'HasError'");
					Assert.NotNull(r.Exceptions, "request did not set any exceptions");
					Assert.GreaterOrEqual(r.Exceptions.Count, 1, "request did not set enough exceptions");
				}
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		public void NonThreaded() {

			_fs.Request(
				_mockBaseUrl + _testUrl.simpleJson
				, (Response r) => {
					Assert.AreEqual(200, r.StatusCode);
				}
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		public void Cancel() {

			IAsyncRequest request = _fs.Request(
				_mockBaseUrl + _testUrl.cancel
				, (Response r) => {
					Assert.IsTrue(r.HasError);
				}
			);

			request.Cancel();

			_fs.WaitForAllRequests();
		}


	}
}