//-----------------------------------------------------------------------
// <copyright file="CompressionTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'
#if UNITY_EDITOR
#if UNITY_5_6_OR_NEWER

namespace Mapbox.MapboxSdkCs.UnitTest
{
	using System.Text;
	using Mapbox.Platform;
	using Mapbox.Utils;
	using NUnit.Framework;
#if UNITY_5_6_OR_NEWER
	using UnityEngine.TestTools;
	using System.Collections;
	using UnityEngine;
#endif

	[TestFixture]
	internal class CompressionTest
	{


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


		[Test]
		public void Empty()
		{
			var buffer = new byte[] { };
			Assert.AreEqual(buffer, Compression.Decompress(buffer));
		}

		[Test]
		public void NotCompressed()
		{
			var buffer = Encoding.ASCII.GetBytes("foobar");
			Assert.AreEqual(buffer, Compression.Decompress(buffer));
		}

#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator Corrupt()
		{
#else
		[Test]
		public void Corrupt() {
#endif
			var buffer = new byte[] { };

			// Vector tiles are compressed.
			_fs.Request(
				"https://api.mapbox.com/v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf",
				(Response res) =>
				{
					if (res.HasError)
					{
#if UNITY_5_6_OR_NEWER
						Debug.LogError(res.ExceptionsAsString);
#else
						System.Diagnostics.Debug.WriteLine(res.ExceptionsAsString);
#endif
					}
					buffer = res.Data;
				},
				_timeout
			);


#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif

			Assert.Greater(buffer.Length, 30);

			buffer[10] = 0;
			buffer[20] = 0;
			buffer[30] = 0;

			Assert.AreEqual(buffer, Compression.Decompress(buffer));
		}


#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator Decompress()
		{
#else
		[Test]
		public void Decompress() {
#endif
			var buffer = new byte[] { };

			// Vector tiles are compressed.
			_fs.Request(
				"https://api.mapbox.com/v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf",
				(Response res) =>
				{
					if (res.HasError)
					{
#if UNITY_5_6_OR_NEWER
						Debug.LogError(res.ExceptionsAsString);
#else
						System.Diagnostics.Debug.WriteLine(res.ExceptionsAsString);
#endif
					}
					buffer = res.Data;
				},
				_timeout
			);

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif

			// tiles are automatically decompressed during HttpRequest on full .Net framework
			// not on .NET Core / UWP / Unity
#if NETFX_CORE || UNITY_5_6_OR_NEWER
			Assert.Less(buffer.Length, Compression.Decompress(buffer).Length);
#else
			Assert.AreEqual(buffer.Length, Compression.Decompress(buffer).Length);
#endif
		}
	}
}


#endif
#endif
