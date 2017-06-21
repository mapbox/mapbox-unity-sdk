//-----------------------------------------------------------------------
// <copyright file="FileSourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Mapbox.MapboxSdkCs.UnitTest
{


	using System;
	using Mapbox.Platform;
	using NUnit.Framework;
	using System.Net;
	using System.Diagnostics;


	[TestFixture]
	internal class FileSourceTest
	{
		private const string _url = "https://api.mapbox.com/geocoding/v5/mapbox.places/helsinki.json";
		private FileSource _fs;
		private int _timeout = 10;


		[SetUp]
		public void SetUp()
		{
#if UNITY_5_3_OR_NEWER
			_fs = new FileSource(Unity.MapboxAccess.Instance.Configuration.AccessToken);
			_timeout = Unity.MapboxAccess.Instance.Configuration.DefaultTimeout;
#else
			// when run outside of Unity FileSource gets the access token from environment variable 'MAPBOX_ACCESS_TOKEN'
			_fs = new FileSource();
#endif
		}


#if !UNITY_5_3_OR_NEWER
		[Test]
		public void AccessTokenSet()
		{
			Assert.IsNotNull(
				Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN"),
				"MAPBOX_ACCESS_TOKEN not set in the environment.");
		}
#endif


		[Test]
		[Ignore("not working within Unity")]
		public void Request()
		{
			_fs.Request(
				_url,
				(Response res) =>
				{
					Assert.IsNotNull(res.Data, "No data received from the servers.");
				}
				, _timeout
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		[Ignore("not working within Unity")]
		public void MultipleRequests()
		{
			int count = 0;

			_fs.Request(_url, (Response res) => ++count, _timeout);
			_fs.Request(_url, (Response res) => ++count, _timeout);
			_fs.Request(_url, (Response res) => ++count, _timeout);

			_fs.WaitForAllRequests();

			Assert.AreEqual(count, 3, "Should have received 3 replies.");
		}


		[Test]
		[Ignore("not working within Unity")]
		public void RequestCancel()
		{
			var request = _fs.Request(
				_url,
				(Response res) =>
				{
					Assert.IsTrue(res.HasError);
					WebException wex = res.Exceptions[0] as WebException;
					Assert.IsNotNull(wex);
					Assert.AreEqual(wex.Status, WebExceptionStatus.RequestCanceled);
				},
				_timeout
			);

			request.Cancel();

			_fs.WaitForAllRequests();
		}


		[Test]
		[Ignore("not working within Unity")]
		public void RequestDnsError()
		{
			_fs.Request(
				"https://dnserror.shouldnotwork",
				(Response res) =>
				{
					Assert.IsTrue(res.HasError);
				},
				_timeout
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		[Ignore("not working within Unity")]
		public void RequestForbidden()
		{
			// Mapbox servers will return a forbidden when attempting
			// to access a page outside the API space with a token
			// on the query. Let's hope the behaviour stay like this.
			_fs.Request(
				"https://mapbox.com/forbidden",
				(Response res) =>
				{
					Assert.IsTrue(res.HasError);
				},
				_timeout
			);

			_fs.WaitForAllRequests();
		}


		[Test]
		[Ignore("not working within Unity")]
		public void WaitWithNoRequests()
		{
			// This should simply not block.
			_fs.WaitForAllRequests();
		}


	}
}