//-----------------------------------------------------------------------
// <copyright file="CompressionTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest {
	using System.Text;
	using Mapbox.Platform;
	using Mapbox.Utils;
	using NUnit.Framework;

	[TestFixture]
	internal class CompressionTest {
		[Test]
		public void Empty() {
			var buffer = new byte[] { };
			Assert.AreEqual(buffer, Compression.Decompress(buffer));
		}

		[Test]
		public void NotCompressed() {
			var buffer = Encoding.ASCII.GetBytes("foobar");
			Assert.AreEqual(buffer, Compression.Decompress(buffer));
		}

		[Test]
		public void Corrupt() {
			var fs = new FileSource();
			var buffer = new byte[] { };

			// Vector tiles are compressed.
			fs.Request(
				"https://api.mapbox.com/v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf",
				(Response res) => {
					buffer = res.Data;
				});

			fs.WaitForAllRequests();

			Assert.Greater(buffer.Length, 30);

			buffer[10] = 0;
			buffer[20] = 0;
			buffer[30] = 0;

			Assert.AreEqual(buffer, Compression.Decompress(buffer));
		}

		[Test]
		public void Decompress() {
			var fs = new FileSource();
			var buffer = new byte[] { };

			// Vector tiles are compressed.
			fs.Request(
				"https://api.mapbox.com/v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf",
				(Response res) => {
					buffer = res.Data;
				});

			fs.WaitForAllRequests();

			//tiles are automatically decompressed during HttpRequest on full .Net framework
#if NETFX_CORE
			Assert.Less(buffer.Length, Compression.Decompress(buffer).Length);
#else
			Assert.AreEqual(buffer.Length, Compression.Decompress(buffer).Length);
#endif
		}
	}
}
