//-----------------------------------------------------------------------
// <copyright file="FileSourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'
#if UNITY_5_6_OR_NEWER

namespace Mapbox.MapboxSdkCs.UnitTest
{


	using Mapbox.Platform;
	using NUnit.Framework;
	using UnityEngine.TestTools;
	using System.Collections;
	using Mapbox.Platform.TilesetTileJSON;


	[TestFixture]
	internal class TileJSONTest
	{



		[UnityTest]
		public IEnumerator MapboxStreets()
		{
			string id = "mapbox.mapbox-streets-v7";
			int minZoom = 0;
			int maxZoom = 16;

			TileJSONResponse response = null;

			Unity.MapboxAccess.Instance.TileJSON.Get(
				id
				, (TileJSONResponse tjr) =>
				{
					response = tjr;
				}
			);


			IEnumerator enumerator = ((FileSource)Unity.MapboxAccess.Instance.TileJSON.FileSource).WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			testsCommonToVectorAndRasterTilesets(response, id, minZoom, maxZoom);
			testsForVectorTilesets(response);
		}


		[UnityTest]
		public IEnumerator ConcatenatedVectorTilesets()
		{
			string id = "mapbox.mapbox-traffic-v1,mapbox.mapbox-streets-v7";
			int minZoom = 0;
			int maxZoom = 16;

			TileJSONResponse response = null;

			Unity.MapboxAccess.Instance.TileJSON.Get(
				id
				, (TileJSONResponse tjr) =>
				{
					response = tjr;
				}
			);


			IEnumerator enumerator = ((FileSource)Unity.MapboxAccess.Instance.TileJSON.FileSource).WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			testsCommonToVectorAndRasterTilesets(
				response
				, id
				, minZoom
				, maxZoom
				, boundsSouth: -90
				, boundsNorth: 90
			);
			testsForVectorTilesets(response);
		}


		[UnityTest]
		public IEnumerator MapboxSatellite()
		{
			string id = "mapbox.satellite";
			int minZoom = 0;
			int maxZoom = 22;

			TileJSONResponse response = null;

			Unity.MapboxAccess.Instance.TileJSON.Get(
				id
				, (TileJSONResponse tjr) =>
				{
					response = tjr;
				}
			);


			IEnumerator enumerator = ((FileSource)Unity.MapboxAccess.Instance.TileJSON.FileSource).WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			testsCommonToVectorAndRasterTilesets(response, id, minZoom, maxZoom, boundsSouth: -85, boundsNorth: 85);
		}


		[UnityTest]
		public IEnumerator MapboxEmerald()
		{
			string id = "mapbox.emerald";
			int minZoom = 0;
			int maxZoom = 22;

			TileJSONResponse response = null;

			Unity.MapboxAccess.Instance.TileJSON.Get(
				id
				, (TileJSONResponse tjr) =>
				{
					response = tjr;
				}
			);


			IEnumerator enumerator = ((FileSource)Unity.MapboxAccess.Instance.TileJSON.FileSource).WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			testsCommonToVectorAndRasterTilesets(response, id, minZoom, maxZoom);

			Assert.IsNotEmpty(response.Source, "'Source' not set properly");
		}



		private void testsForVectorTilesets(TileJSONResponse response)
		{
			Assert.IsNotNull(response.VectorLayers, "'VectorLayers' not set properly");
			Assert.GreaterOrEqual(response.VectorLayers.Length, 1, "Not enough 'VectorLayers'");

			TileJSONObjectVectorLayer vl1 = response.VectorLayers[0];
			Assert.IsNotNull(vl1.Fields, "VectorLayer fields not parsed properly");
			Assert.GreaterOrEqual(vl1.Fields.Count, 1, "Not enough vector layer fields");
			Assert.IsNotEmpty(vl1.Id, "'Id' of vector layer not parsed properly");
			Assert.IsNotEmpty(vl1.Source, "'Source' of vector layer not parsed properly");
			Assert.IsNotEmpty(vl1.SourceName, "'SourceName' of vector layer not parsed properly");
		}


		private void testsCommonToVectorAndRasterTilesets(
			TileJSONResponse response
			, string id
			, int minZoom
			, int maxZoom
			, double boundsWest = -180
			, double boundsSouth = -85.0511
			, double boundsEast = 180
			, double boundsNorth = 85.0511
		)
		{
			Assert.IsNotNull(response, "Parsing error or no data received from the servers.");

			Assert.IsNotEmpty(response.Attribution, "Attribution not set.");

			Assert.AreEqual(boundsWest, response.BoundsParsed.West, "Bounds.West does not match");
			Assert.AreEqual(boundsSouth, response.BoundsParsed.South, 0.003, "Bounds.South does not match");
			Assert.AreEqual(boundsEast, response.BoundsParsed.East, "Bounds.East does not match");
			Assert.AreEqual(boundsNorth, response.BoundsParsed.North, 0.003, "Bounds.North does not match");

			// this does not work as some tilesets report whole world bounds despite covering a small area only
			// revisit some time in the future
			//Assert.AreEqual(response.BoundsParsed.Center.x, response.CenterParsed.x, 0.003, "Center.x does not match");
			//Assert.AreEqual(response.BoundsParsed.Center.y, response.CenterParsed.y, "Center.y does not match");

			//concatenated tilesets don't have created property
			if (response.Created.HasValue)
			{
				Assert.Greater(response.Created.Value, 0, "'Created' not set");
				Assert.IsNotNull(response.CreatedUtc, "'CreatedUtc' not set properly'");
			}

			// mapbox.satellite doesn't set 'format': bug??
			// revisit in the future
			//Assert.IsNotEmpty(response.Format, "'Format' is empty");

			// concatenated tilesets don't report 'id'
			if (!id.Contains(","))
			{
				Assert.IsNotEmpty(response.Id, "'Id' is empty");
				Assert.AreEqual(id, response.Id, "'Id' not set properly");
			}
			Assert.AreEqual(minZoom, response.MinZoom, "'MinZoom' not set properly");
			Assert.AreEqual(maxZoom, response.MaxZoom, "'MaxZoom' not set properly");

			//Unmodified tilesets don't have a modified property
			if (response.Modified.HasValue)
			{
				Assert.Greater(response.Modified.Value, 0, "'Modified not set'");
				Assert.IsTrue(response.ModifiedUtc.HasValue, "'Modified not properly parsed'");
			}

			Assert.IsNotEmpty(response.Name, "'Name' not set properly");
			Assert.IsFalse(response.Private, "'Private' not set properly");
			Assert.AreEqual("xyz", response.Scheme, "'Scheme' not set properly");
			Assert.IsNotEmpty(response.TileJSONVersion, "'TileJSONVersion not set properly");

			Assert.IsNotNull(response.Tiles, "'Tiles' not set properly");
			Assert.GreaterOrEqual(response.Tiles.Length, 1, "Not enough 'Tiles'");

			// concatenated tilesets don't report 'webpage'
			if (!id.Contains(","))
			{
				Assert.IsNotEmpty(response.WebPage, "'WebPage' not set properly");
			}
		}



	}
}

#endif
