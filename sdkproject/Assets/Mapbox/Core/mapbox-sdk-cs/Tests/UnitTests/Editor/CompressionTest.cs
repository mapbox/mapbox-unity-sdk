//-----------------------------------------------------------------------
// <copyright file="CompressionTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{
	using System.Text;
	using Mapbox.Platform;
	using Mapbox.Utils;
	using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
	using UnityEngine.TestTools;
	using System.Collections;
	using UnityEngine;
#endif

	[TestFixture]
	internal class CompressionTest
	{


		private FileSource _fs;


		[SetUp]
		public void SetUp()
		{
#if UNITY_5_3_OR_NEWER
			_fs = new FileSource(Unity.MapboxAccess.Instance.Configuration.AccessToken);
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

#if UNITY_5_3_OR_NEWER
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
					buffer = res.Data;
				});


#if UNITY_5_3_OR_NEWER
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


#if UNITY_5_3_OR_NEWER
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
					buffer = res.Data;
				});

#if UNITY_5_3_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif

			// tiles are automatically decompressed during HttpRequest on full .Net framework
			// not on .NET Core / UWP
#if NETFX_CORE
			Assert.Less(buffer.Length, Compression.Decompress(buffer).Length);
#else
			Assert.AreEqual(buffer.Length, Compression.Decompress(buffer).Length);
#endif
		}
	}
}
