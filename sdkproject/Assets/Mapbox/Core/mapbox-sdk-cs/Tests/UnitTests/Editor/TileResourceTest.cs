//-----------------------------------------------------------------------
// <copyright file="TileResourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest
{
    using Mapbox.Map;
    using Mapbox.Utils;
    using NUnit.Framework;

    [TestFixture]
	internal class TileResourceTest
	{
		private string api;
		private CanonicalTileId id;

		[SetUp]
		public void SetUp()
		{
			this.api = Constants.BaseAPI;
			this.id = new CanonicalTileId(0, 0, 0);
		}

		[Test]
		public void GetUrlRaster()
		{
			var res1 = TileResource.MakeRaster(this.id, null);
			Assert.AreEqual(this.api + "styles/v1/mapbox/satellite-v9/tiles/0/0/0", res1.GetUrl());

			var res2 = TileResource.MakeRaster(this.id, "mapbox://styles/mapbox/basic-v9");
			Assert.AreEqual(this.api + "styles/v1/mapbox/basic-v9/tiles/0/0/0", res2.GetUrl());

			var res3 = TileResource.MakeRaster(this.id, "https://api.mapbox.com/styles/v1/penny/penny-map/tiles");
			Assert.AreEqual(this.api + "styles/v1/penny/penny-map/tiles/0/0/0", res3.GetUrl());
		}

		[Test]
		public void GetUrlClassicRaster()
		{
			var res1 = TileResource.MakeClassicRaster(this.id, null);
			Assert.AreEqual(this.api + "v4/mapbox.satellite/0/0/0.png", res1.GetUrl());

			var res2 = TileResource.MakeClassicRaster(this.id, "foobar");
			Assert.AreEqual(this.api + "v4/foobar/0/0/0.png", res2.GetUrl());

			var res3 = TileResource.MakeClassicRaster(this.id, "test");
			Assert.AreEqual(this.api + "v4/test/0/0/0.png", res3.GetUrl());
		}

		[Test]
		public void GetUrlVector()
		{
			var res1 = TileResource.MakeVector(this.id, null);
			Assert.AreEqual(this.api + "v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf", res1.GetUrl());

			var res2 = TileResource.MakeVector(this.id, "foobar");
			Assert.AreEqual(this.api + "v4/foobar/0/0/0.vector.pbf", res2.GetUrl());

			var res3 = TileResource.MakeVector(this.id, "test");
			Assert.AreEqual(this.api + "v4/test/0/0/0.vector.pbf", res3.GetUrl());
		}
	}
}