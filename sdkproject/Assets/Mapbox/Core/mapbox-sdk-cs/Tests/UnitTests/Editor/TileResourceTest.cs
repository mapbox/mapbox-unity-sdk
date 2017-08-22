//-----------------------------------------------------------------------
// <copyright file="TileResourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'
#if UNITY_EDITOR
#if UNITY_5_6_OR_NEWER

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using Mapbox.Map;
	using Mapbox.Utils;
	using NUnit.Framework;

	[TestFixture]
	public class TileResourceTest
	{
		//needed when tests can be enabled again
		//private string _api;
		//private CanonicalTileId _tileId;

		[SetUp]
		public void SetUp()
		{
			//_api = Constants.BaseAPI;
			//_tileId = new CanonicalTileId(0, 0, 0);
		}

		[Test]
		[Ignore("not working in Unity as 'TileResource' is 'internal sealed' and mapbox-sdk-cs and tests end up in 2 different aseemblies")]
		public void GetUrlRaster()
		{
			// TODO
			/*
			var res1 = TileResource.MakeRaster(_tileId, null);
			Assert.AreEqual(_api + "styles/v1/mapbox/satellite-v9/tiles/0/0/0", res1.GetUrl());

			var res2 = TileResource.MakeRaster(_tileId, "mapbox://styles/mapbox/basic-v9");
			Assert.AreEqual(_api + "styles/v1/mapbox/basic-v9/tiles/0/0/0", res2.GetUrl());

			var res3 = TileResource.MakeRaster(_tileId, "https://api.mapbox.com/styles/v1/penny/penny-map/tiles");
			Assert.AreEqual(_api + "styles/v1/penny/penny-map/tiles/0/0/0", res3.GetUrl());
			*/
		}

		[Test]
		[Ignore("not working in Unity as 'TileResource' is 'internal sealed' and mapbox-sdk-cs and tests end up in 2 different aseemblies")]
		public void GetUrlClassicRaster()
		{
			// TODO
			/*
			var res1 = TileResource.MakeClassicRaster(_tileId, null);
			Assert.AreEqual(_api + "v4/mapbox.satellite/0/0/0.png", res1.GetUrl());

			var res2 = TileResource.MakeClassicRaster(_tileId, "foobar");
			Assert.AreEqual(_api + "v4/foobar/0/0/0.png", res2.GetUrl());

			var res3 = TileResource.MakeClassicRaster(_tileId, "test");
			Assert.AreEqual(_api + "v4/test/0/0/0.png", res3.GetUrl());
			*/
		}

		[Test]
		[Ignore("not working in Unity as 'TileResource' is 'internal sealed' and mapbox-sdk-cs and tests end up in 2 different aseemblies")]
		public void GetUrlVector()
		{
			// TODO
			/*
			var res1 = TileResource.MakeVector(id, null);
			Assert.AreEqual(api + "v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf", res1.GetUrl());

			var res2 = TileResource.MakeVector(id, "foobar");
			Assert.AreEqual(api + "v4/foobar/0/0/0.vector.pbf", res2.GetUrl());

			var res3 = TileResource.MakeVector(id, "test");
			Assert.AreEqual(api + "v4/test/0/0/0.vector.pbf", res3.GetUrl());
			*/
		}
	}
}

#endif
#endif
