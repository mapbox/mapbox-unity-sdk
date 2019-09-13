//-----------------------------------------------------------------------
// <copyright file="VectorTileTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'
#if UNITY_5_6_OR_NEWER


namespace Mapbox.MapboxSdkCs.UnitTest
{

	using System.Linq;
	using Mapbox.Map;
	using Mapbox.Platform;
	using Mapbox.Utils;
	using NUnit.Framework;
#if UNITY_5_6_OR_NEWER
	using UnityEngine.TestTools;
	using System.Collections;
#endif


	[TestFixture]
	internal class VectorTileTest
	{


		private FileSource _fs;


		[SetUp]
		public void SetUp()
		{
#if UNITY_5_6_OR_NEWER
			_fs = new FileSource(Unity.MapboxAccess.Instance.Configuration.GetMapsSkuToken, Unity.MapboxAccess.Instance.Configuration.AccessToken);
#else
			// when run outside of Unity FileSource gets the access token from environment variable 'MAPBOX_ACCESS_TOKEN'
			_fs = new FileSource();
#endif
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator ParseSuccess()
#else
		[Test]
		public void ParseSuccess()
#endif
		{
			var map = new Map<VectorTile>(_fs);

			var mapObserver = new Utils.VectorMapObserver();
			map.Subscribe(mapObserver);

			// Helsinki city center.
			map.Center = new Vector2d(60.163200, 24.937700);

			for (int zoom = 15; zoom > 0; zoom--)
			{
				map.Zoom = zoom;
				map.Update();
#if UNITY_5_6_OR_NEWER
				IEnumerator enumerator = _fs.WaitForAllRequests();
				while (enumerator.MoveNext()) { yield return null; }
#else
				_fs.WaitForAllRequests();
#endif
			}

			// We must have all the tiles for Helsinki from 0-15.
			Assert.AreEqual(15, mapObserver.Tiles.Count);

			foreach (var tile in mapObserver.Tiles)
			{
				Assert.Greater(tile.LayerNames().Count, 0, "Tile contains at least one layer");
				Mapbox.VectorTile.VectorTileLayer layer = tile.GetLayer("water");
				Assert.NotNull(layer, "Tile contains 'water' layer. Layers: {0}", string.Join(",", tile.LayerNames().ToArray()));
				Assert.Greater(layer.FeatureCount(), 0, "Water layer has features");
				Mapbox.VectorTile.VectorTileFeature feature = layer.GetFeature(0);
				Assert.Greater(feature.Geometry<long>().Count, 0, "Feature has geometry");
				Assert.Greater(tile.GeoJson.Length, 1000);
			}

			map.Unsubscribe(mapObserver);
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator SeveralTiles()
#else
		[Test]
		public void ParseSuccess
#endif
		{
			var map = new Map<VectorTile>(_fs);

			var mapObserver = new Utils.VectorMapObserver();
			map.Subscribe(mapObserver);

			map.Vector2dBounds = Vector2dBounds.World();
			map.Zoom = 3; // 64 tiles.
			map.Update();

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif

			Assert.AreEqual(64, mapObserver.Tiles.Count);

			foreach (var tile in mapObserver.Tiles)
			{
				if (!tile.HasError)
				{
					Assert.Greater(tile.GeoJson.Length, 41);
				}
				else
				{
					Assert.GreaterOrEqual(tile.Exceptions.Count, 1, "not set enough exceptions set on 'Tile'");
				}
			}

			map.Unsubscribe(mapObserver);
		}


	}
}

#endif
