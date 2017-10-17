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
					matchingResponse=response;
				}
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
			Assert.IsNotNull(matchingResponse, "Matching response is NULL");
			Assert.IsFalse(matchingResponse.HasRequestError, "Error during web request");
			Assert.AreEqual("Ok", matchingResponse.Code, "Matching code != 'Ok'");
			Assert.IsNotNull(matchingResponse.Tracepoints, "Tracepoints are NULL");
			Assert.AreEqual(3, matchingResponse.Tracepoints[3].WaypointIndex, "Wrong WaypointIndex");
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




	}
}

#endif
#endif