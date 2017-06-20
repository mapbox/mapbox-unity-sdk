//-----------------------------------------------------------------------
// <copyright file="MapTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.UnitTest {
	using System.Drawing;
	using Mapbox.Map;
	using Mapbox.Platform;
	using Mapbox.Utils;
	using NUnit.Framework;

	[TestFixture]
	internal class MapTest {
		private FileSource fs;

		[SetUp]
		public void SetUp() {
			this.fs = new FileSource();
		}

		[Test]
		public void World() {
			var map = new Map<VectorTile>(this.fs);

			map.Vector2dBounds = Vector2dBounds.World();
			map.Zoom = 3;

			var mapObserver = new Utils.VectorMapObserver();
			map.Subscribe(mapObserver);
			map.Update();

			this.fs.WaitForAllRequests();

			Assert.AreEqual(64, mapObserver.Tiles.Count);

			map.Unsubscribe(mapObserver);
		}

		[Test]
		public void RasterHelsinki() {
			var map = new Map<RasterTile>(this.fs);

			map.Center = new Vector2d(60.163200, 24.937700);
			map.Zoom = 13;

			var mapObserver = new Utils.RasterMapObserver();
			map.Subscribe(mapObserver);
			map.Update();

			this.fs.WaitForAllRequests();

			// TODO: Assert.True(mapObserver.Complete);
			// TODO: Assert.IsNull(mapObserver.Error);
			Assert.AreEqual(1, mapObserver.Tiles.Count);
			Assert.AreEqual(new Size(512, 512), mapObserver.Tiles[0].Size);

			map.Unsubscribe(mapObserver);
		}

		[Test]
		public void ChangeMapId() {
			var map = new Map<ClassicRasterTile>(this.fs);

			var mapObserver = new Utils.ClassicRasterMapObserver();
			map.Subscribe(mapObserver);

			map.Center = new Vector2d(60.163200, 24.937700);
			map.Zoom = 13;
			map.MapId = "invalid";
			map.Update();

			this.fs.WaitForAllRequests();
			Assert.AreEqual(0, mapObserver.Tiles.Count);

			map.MapId = "mapbox.terrain-rgb";
			map.Update();

			this.fs.WaitForAllRequests();
			Assert.AreEqual(1, mapObserver.Tiles.Count);

			map.MapId = null; // Use default map ID.
			map.Update();

			this.fs.WaitForAllRequests();
			Assert.AreEqual(2, mapObserver.Tiles.Count);

			// Should have fetched tiles from different map IDs.
			Assert.AreNotEqual(mapObserver.Tiles[0], mapObserver.Tiles[1]);

			map.Unsubscribe(mapObserver);
		}

		[Test]
		public void SetVector2dBoundsZoom() {
			var map1 = new Map<RasterTile>(this.fs);
			var map2 = new Map<RasterTile>(this.fs);

			map1.Zoom = 3;
			map1.Vector2dBounds = Vector2dBounds.World();

			map2.SetVector2dBoundsZoom(Vector2dBounds.World(), 3);

			Assert.AreEqual(map1.Tiles.Count, map2.Tiles.Count);
		}

		[Test]
		public void TileMax() {
			var map = new Map<RasterTile>(this.fs);

			map.SetVector2dBoundsZoom(Vector2dBounds.World(), 2);
			map.Update();
			Assert.Less(map.Tiles.Count, Map<RasterTile>.TileMax); // 16

			// Should stay the same, ignore requests.
			map.SetVector2dBoundsZoom(Vector2dBounds.World(), 5);
			map.Update();
			Assert.AreEqual(16, map.Tiles.Count);
		}

		[Test]
		public void Zoom() {
			var map = new Map<RasterTile>(this.fs);

			map.Zoom = 50;
			Assert.AreEqual(20, map.Zoom);

			map.Zoom = -50;
			Assert.AreEqual(0, map.Zoom);
		}
	}
}
