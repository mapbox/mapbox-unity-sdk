//-----------------------------------------------------------------------
// <copyright file="MapMatcherTest.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'
#if UNITY_EDITOR
#if UNITY_5_6_OR_NEWER

namespace Mapbox.MapboxSdkCs.UnitTest
{


	using Mapbox.Platform;
	using NUnit.Framework;
	using UnityEngine.TestTools;
	using System.Collections;
	using Mapbox.MapMatching;
	using Mapbox.Utils;



	[TestFixture]
	internal class MapMatcherTest
	{

		private const string _url = "https://api.mapbox.com/matching/v5/mapbox/driving/-117.1728265285492,32.71204416018209;-117.17288821935652,32.712258556224;-117.17293113470076,32.712443613445814;-117.17292040586472,32.71256999376694;-117.17298477888109,32.712603845608285;-117.17314302921294,32.71259933203019;-117.17334151268004,32.71254065549407";
		private FileSource _fs;
		private int _timeout = 10;


		[SetUp]
		public void SetUp()
		{
			_fs = new FileSource(Unity.MapboxAccess.Instance.Configuration.AccessToken);
			_timeout = Unity.MapboxAccess.Instance.Configuration.DefaultTimeout;
		}




		[UnityTest]
		public IEnumerator AsSimpleAsPossible()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(32.71204416018209,-117.1728265285492),
				new Vector2d(32.712258556224,-117.17288821935652),
				new Vector2d(32.712443613445814,-117.17293113470076),
				new Vector2d(32.71256999376694,-117.17292040586472),
				new Vector2d(32.712603845608285,-117.17298477888109),
				new Vector2d(32.71259933203019,-117.17314302921294),
				new Vector2d(32.71254065549407,-117.17334151268004),
			};

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			commonBasicResponseAsserts(matchingResponse);

			Assert.AreEqual(7, matchingResponse.Tracepoints.Length, "Wrong number of tracepoints");
			Assert.AreEqual(3, matchingResponse.Tracepoints[3].WaypointIndex, "Wrong WaypointIndex");

			Assert.AreEqual(1, matchingResponse.Matchings.Length, "Wrong number of matchings");
			Assert.AreEqual(45, matchingResponse.Matchings[0].Weight, "Wrong Weight");
			Assert.AreEqual("routability", matchingResponse.Matchings[0].WeightName, "Wrong WeightName");
			Assert.AreEqual(6, matchingResponse.Matchings[0].Legs.Count, "Wrong number of legs");
			Assert.AreEqual(8, matchingResponse.Matchings[0].Geometry.Count, "Wrong number of vertices in geometry");
		}


		[UnityTest]
		public IEnumerator NoSegment()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.28585,16.55267),
				new Vector2d(48.28933,16.55211)
			};

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			Assert.IsNotNull(matchingResponse, "Matching response is NULL");
			Assert.IsFalse(matchingResponse.HasRequestError, "Error during web request");
			Assert.AreEqual("NoSegment", matchingResponse.Code, "Matching code != 'NoSegment'");
			Assert.AreEqual("Could not find a matching segment for input coordinates", matchingResponse.Message, "Message not as expected");

			Assert.IsNull(matchingResponse.Tracepoints, "Tracepoints are not NULL");

			Assert.IsNotNull(matchingResponse.Matchings, "Matchings are NULL");
			Assert.AreEqual(0, matchingResponse.Matchings.Length, "Wrong number of matchings");
		}


		[UnityTest]
		public IEnumerator GeometriesWith5Digits()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.1974721043879,16.36202484369278),
				new Vector2d(48.197922645046546,16.36285901069641)
			};
			//no extra parameters needed: 5 digits default

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			commonBasicResponseAsserts(matchingResponse);

			string locationX = matchingResponse.Matchings[0].Geometry[0].x.ToString(System.Globalization.CultureInfo.InvariantCulture);
			locationX = locationX.Substring(locationX.IndexOf(".") + 1);
			Assert.AreEqual(5, locationX.Length, "Precision not as expected");
		}


		[UnityTest]
		public IEnumerator GeometriesWith6Digits()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.1974721043879,16.36202484369278),
				new Vector2d(48.197922645046546,16.36285901069641)
			};
			resource.Geometries = Geometries.Polyline6;

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			commonBasicResponseAsserts(matchingResponse);

			string locationX = matchingResponse.Matchings[0].Geometry[0].x.ToString(System.Globalization.CultureInfo.InvariantCulture);
			locationX = locationX.Substring(locationX.IndexOf(".") + 1);
			Assert.AreEqual(6, locationX.Length, "Precision not as expected");
		}


		[UnityTest]
		public IEnumerator Radiuses()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.28585,16.55267),
				new Vector2d(48.28933,16.55211)
			};
			resource.Radiuses = new uint[] { 50, 50 };

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			commonBasicResponseAsserts(matchingResponse);

			Assert.AreEqual(2, matchingResponse.Tracepoints.Length, "Wrong number of tracepoints");
			Assert.AreEqual(1, matchingResponse.Tracepoints[1].WaypointIndex, "Wrong WaypointIndex");

			Assert.AreEqual(1, matchingResponse.Matchings.Length, "Wrong number of matchings");
			Assert.AreEqual(22.5, matchingResponse.Matchings[0].Weight, "Wrong Weight");
			Assert.AreEqual("routability", matchingResponse.Matchings[0].WeightName, "Wrong WeightName");
			Assert.AreEqual(1, matchingResponse.Matchings[0].Legs.Count, "Wrong number of legs");
			Assert.AreEqual(2, matchingResponse.Matchings[0].Geometry.Count, "Wrong number of vertices in geometry");
		}



		[UnityTest]
		public IEnumerator AlternativesWithSteps()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.31331,16.49062),
				new Vector2d(48.31638,16.49243)
			};
			resource.Radiuses = new uint[] { 10, 30 };
			resource.Steps = true;

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			commonBasicResponseAsserts(matchingResponse);

			Assert.AreEqual(2, matchingResponse.Tracepoints.Length, "Wrong number of tracepoints");
			Assert.GreaterOrEqual(2, matchingResponse.Tracepoints[0].AlternativesCount, "Wrong 'AlternativesCount' for Tracepoint[0]");
			Assert.GreaterOrEqual(19, matchingResponse.Tracepoints[1].AlternativesCount, "Wrong 'AlternativesCount' for Tracepoint[1]");

			Assert.IsNotNull(matchingResponse.Matchings[0].Legs[0].Steps, "Steps are NULL");
			Assert.AreEqual(2, matchingResponse.Matchings[0].Legs[0].Steps.Count, "Wrong number of steps");
			Assert.IsNotNull(matchingResponse.Matchings[0].Legs[0].Steps[0].Intersections, "Intersections are NULL");
			Assert.AreEqual(3, matchingResponse.Matchings[0].Legs[0].Steps[0].Intersections.Count, "Wrong number of intersections");
		}


		[UnityTest]
		public IEnumerator OverviewSimplified()
		{
			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.28514194095631,16.32358074188232),
				new Vector2d(48.28528472524657,16.324278116226196),
				new Vector2d(48.28502771323672,16.325350999832153),
				new Vector2d(48.284999156266906,16.326016187667847),
				new Vector2d(48.284870649705155,16.326134204864502),
				new Vector2d(48.28467074996644,16.32594108581543),
				new Vector2d(48.28467074996644,16.325050592422485),
				new Vector2d(48.28459935701301,16.324610710144043)

			};
			resource.Overview = Overview.Simplified;

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			commonBasicResponseAsserts(matchingResponse);

			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Geometry.Count, 14, "Wrong number of vertices in match geometry");
		}


		[UnityTest]
		public IEnumerator OverviewFull()
		{
			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.28514194095631,16.32358074188232),
				new Vector2d(48.28528472524657,16.324278116226196),
				new Vector2d(48.28502771323672,16.325350999832153),
				new Vector2d(48.284999156266906,16.326016187667847),
				new Vector2d(48.284870649705155,16.326134204864502),
				new Vector2d(48.28467074996644,16.32594108581543),
				new Vector2d(48.28467074996644,16.325050592422485),
				new Vector2d(48.28459935701301,16.324610710144043)

			};
			resource.Overview = Overview.Full;

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			commonBasicResponseAsserts(matchingResponse);

			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Geometry.Count, 20, "Wrong number of vertices in match geometry");
		}



		[UnityTest]
		public IEnumerator InvalidCoordinate()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(-117.1728265285492, 32.71204416018209),
				new Vector2d(-117.17288821935652,32.712258556224),
			};

			MapMatcher mapMatcher = new MapMatcher(_fs);
			MapMatchingResponse matchingResponse = null;
			mapMatcher.Match(
				resource,
				(MapMatchingResponse response) =>
				{
					matchingResponse = response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
			Assert.IsNotNull(matchingResponse, "Matching response is NULL");
			Assert.IsTrue(matchingResponse.HasRequestError, "No web request error");
			Assert.IsTrue(matchingResponse.HasMatchingError, "No matching error");
			Assert.AreEqual("InvalidInput", matchingResponse.Code, "Matching code != 'InvalidInput'");
			Assert.IsNotNull(matchingResponse.Message, "Matching message is NULL");
			Assert.IsNotEmpty(matchingResponse.Message, "Matching message is empty");
			Assert.AreEqual("Coordinate is invalid: 32.71204,-117.17283", matchingResponse.Message, "Matching message not as expected");
		}


		private void commonBasicResponseAsserts(MapMatchingResponse matchingResponse)
		{
			Assert.IsNotNull(matchingResponse, "Matching response is NULL");
			Assert.IsFalse(matchingResponse.HasRequestError, "Error during web request");
			Assert.AreEqual("Ok", matchingResponse.Code, "Matching code != 'Ok'");
			Assert.IsFalse(matchingResponse.HasMatchingError, "Macthing error");

			Assert.IsNotNull(matchingResponse.Tracepoints, "Tracepoints are NULL");
			Assert.GreaterOrEqual(matchingResponse.Tracepoints.Length, 2, "Less than 2 tracepoints");

			Assert.IsNotNull(matchingResponse.Matchings, "Matchings are NULL");
			Assert.GreaterOrEqual(1, matchingResponse.Matchings.Length, "Less than 1 matchings");
			Assert.IsNotNull(matchingResponse.Matchings[0].Legs, "Legs are NULL");
			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Legs.Count, 1, "1st match has no legs");
		}


	}
}

#endif
#endif