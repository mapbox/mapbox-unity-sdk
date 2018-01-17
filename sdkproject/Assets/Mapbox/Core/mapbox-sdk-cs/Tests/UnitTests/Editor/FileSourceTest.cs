//-----------------------------------------------------------------------
// <copyright file="FileSourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'
#if UNITY_EDITOR
#if UNITY_5_6_OR_NEWER

namespace Mapbox.MapboxSdkCs.UnitTest
{


	using Mapbox.Platform;
	using NUnit.Framework;
#if UNITY_5_6_OR_NEWER
	using UnityEngine.TestTools;
	using System.Collections;
#endif



	[TestFixture]
	internal class FileSourceTest
	{

		private const string _url = "https://api.mapbox.com/geocoding/v5/mapbox.places/helsinki.json";
		private FileSource _fs;
		private int _timeout = 10;


		[SetUp]
		public void SetUp()
		{
#if UNITY_5_6_OR_NEWER
			_fs = new FileSource(Unity.MapboxAccess.Instance.Configuration.AccessToken);
			_timeout = Unity.MapboxAccess.Instance.Configuration.DefaultTimeout;
#else
			// when run outside of Unity FileSource gets the access token from environment variable 'MAPBOX_ACCESS_TOKEN'
			_fs = new FileSource();
#endif
		}



#if !UNITY_5_6_OR_NEWER
		[Test]
		public void AccessTokenSet()
		{
			Assert.IsNotNull(
				Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN"),
				"MAPBOX_ACCESS_TOKEN not set in the environment."
			);
		}
#endif



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator Request()
#else
		[Test]
		public void Request()
#endif
		{
			byte[] data = null;
			_fs.Request(
				_url,
				(Response res) =>
				{
					data = res.Data;
				}
				, _timeout
			);

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif
			Assert.IsNotNull(data, "No data received from the servers.");
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator MultipleRequests()
#else
		[Test]
		public void Request() 
#endif
		{
			int count = 0;

			_fs.Request(_url, (Response res) => ++count, _timeout);
			_fs.Request(_url, (Response res) => ++count, _timeout);
			_fs.Request(_url, (Response res) => ++count, _timeout);

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif

			Assert.AreEqual(count, 3, "Should have received 3 replies.");
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator RequestCancel()
#else
		[Test]
		public void RequestCancel() 
#endif
		{
			var request = _fs.Request(
				_url,
				(Response res) =>
				{
					Assert.IsTrue(res.HasError);
#if UNITY_5_6_OR_NEWER
					Assert.IsNotNull(res.Exceptions[0]);
					Assert.AreEqual("Request aborted", res.Exceptions[0].Message);
#else
					WebException wex = res.Exceptions[0] as WebException;
					Assert.IsNotNull(wex);
					Assert.AreEqual(wex.Status, WebExceptionStatus.RequestCanceled);
#endif
				},
				_timeout
			);

			request.Cancel();

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator RequestDnsError()
#else
		[Test]
		public void RequestDnsError() 
#endif
		{
			_fs.Request(
				"https://dnserror.shouldnotwork",
				(Response res) =>
				{
					Assert.IsTrue(res.HasError);
					// Attention: when using Fiddler to throttle requests message is "Failed to receive data"
					Assert.IsTrue(
						res.Exceptions[0].Message.Contains("destination host")
						, string.Format("Exception message [{0}] does not contain 'destination host'", res.Exceptions[0].Message)
					);
				},
				_timeout
			);

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator RequestForbidden()
#else
		[Test]
		public void RequestForbidden() 
#endif
		{
			// Mapbox servers will return a forbidden when attempting
			// to access a page outside the API space with a token
			// on the query. Let's hope the behaviour stay like this.
			_fs.Request(
				"https://mapbox.com/forbidden",
				(Response res) =>
				{
					Assert.IsTrue(res.HasError);
					Assert.AreEqual(403, res.StatusCode);
				},
				_timeout
			);

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator WaitWithNoRequests()
#else
		[Test]
		public void WaitWithNoRequests() 
#endif
		{
			// This should simply not block.
#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif
		}


	}
}

#endif
#endif