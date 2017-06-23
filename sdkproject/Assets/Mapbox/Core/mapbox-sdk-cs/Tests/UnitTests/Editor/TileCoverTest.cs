//-----------------------------------------------------------------------
// <copyright file="TileCoverTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{

	using System;
	using System.Collections.Generic;
	using Mapbox.Map;
	using Mapbox.Utils;
	using NUnit.Framework;


	[TestFixture]
	internal class TileCoverTest
	{


		[Test]
		public void World()
		{
			// Zoom > 8 will generate so many tiles that we
			// might run out of memory.
			for (int zoom = 0; zoom < 8; ++zoom)
			{
				var tiles = TileCover.Get(Vector2dBounds.World(), zoom);
				Assert.AreEqual(Math.Pow(4, zoom), tiles.Count);
			}
		}


		[Test]
		public void Helsinki()
		{
			// Assertion results verified on Mapbox GL Native.
			var sw = new Vector2d(60.163200, 24.937700);
			var ne = new Vector2d(60.163300, 24.937800);

			var set1 = TileCover.Get(new Vector2dBounds(sw, ne), 13);
			Assert.AreEqual(1, set1.Count);

			var list1 = new List<CanonicalTileId>(set1);
			Assert.AreEqual("13/4663/2371", list1[0].ToString());

			var set2 = TileCover.Get(new Vector2dBounds(sw, ne), 6);
			Assert.AreEqual(1, set2.Count);

			var list2 = new List<CanonicalTileId>(set2);
			Assert.AreEqual("6/36/18", list2[0].ToString());

			var set3 = TileCover.Get(new Vector2dBounds(sw, ne), 0);
			Assert.AreEqual(1, set3.Count);

			var list3 = new List<CanonicalTileId>(set3);
			Assert.AreEqual("0/0/0", list3[0].ToString());
		}


	}
}
