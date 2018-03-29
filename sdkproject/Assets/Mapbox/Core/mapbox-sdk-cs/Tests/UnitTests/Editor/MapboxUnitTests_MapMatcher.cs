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
	using System.Collections.Generic;
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

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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
			Assert.That(matchingResponse.Matchings[0].Weight > 0 && matchingResponse.Matchings[0].Weight < 100, "Wrong Weight: {0}", matchingResponse.Matchings[0].Weight);
			Assert.AreEqual("routability", matchingResponse.Matchings[0].WeightName, "Wrong WeightName");
			Assert.AreEqual(6, matchingResponse.Matchings[0].Legs.Count, "Wrong number of legs");
			Assert.AreEqual(8, matchingResponse.Matchings[0].Geometry.Count, "Wrong number of vertices in geometry");
		}


		[UnityTest]
		public IEnumerator Profiles()
		{
			//walking
			IEnumerator<MapMatchingResponse> enumerator = profile(Profile.MapboxWalking);
			MapMatchingResponse matchingResponse = null;
			while (enumerator.MoveNext())
			{
				matchingResponse = enumerator.Current;
				yield return null;
			}

			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Duration, 300, "'mapbox/walking' duration [{0}] less than expected", matchingResponse.Matchings[0].Duration);

			//cycling
			enumerator = profile(Profile.MapboxCycling);
			matchingResponse = null;
			while (enumerator.MoveNext())
			{
				matchingResponse = enumerator.Current;
				yield return null;
			}
			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Duration, 100, "'mapbox/cycling' duration less than expected");

			//driving traffic
			enumerator = profile(Profile.MapboxDrivingTraffic);
			matchingResponse = null;
			while (enumerator.MoveNext())
			{
				matchingResponse = enumerator.Current;
				yield return null;
			}
			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Duration, 100, "'driving-traffic' duration less than expected");

			//driving
			enumerator = profile(Profile.MapboxDriving);
			matchingResponse = null;
			while (enumerator.MoveNext())
			{
				matchingResponse = enumerator.Current;
				yield return null;
			}
			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Duration, 100, "'driving' duration less than expected");
		}


		private IEnumerator<MapMatchingResponse> profile(Profile profile)
		{
			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.27275388447381,16.34687304496765),
				new Vector2d(48.271925526874405,16.344040632247925),
				new Vector2d(48.27190410365491,16.343783140182495),
				new Vector2d(48.27198265541583,16.343053579330444),
				new Vector2d(48.27217546377159,16.342334747314453),
				new Vector2d(48.27251823238551,16.341615915298462),
				new Vector2d(48.27223259203358,16.3416588306427),
				new Vector2d(48.27138280254541,16.34069323539734),
				new Vector2d(48.27114714413402,16.34015679359436 )
			};
			resource.Profile = profile;

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			yield return matchingResponse;
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

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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
		public IEnumerator Radiuses()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.28585,16.55267),
				new Vector2d(48.28933,16.55211)
			};
			resource.Radiuses = new uint[] { 50, 50 };

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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
			Assert.GreaterOrEqual(matchingResponse.Matchings[0].Weight, 22.5, "Wrong Weight");
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

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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
		public IEnumerator Timestamps()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.1974721043879,16.36202484369278),
				new Vector2d(48.197922645046546,16.36285901069641)
			};
			resource.Timestamps = new long[]
			{
				946684800,
				946684980
			};

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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
		}


		[UnityTest]
		public IEnumerator Annotation()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.1974721043879,16.36202484369278),
				new Vector2d(48.197922645046546,16.36285901069641)
			};
			//need to pass 'Overview.Full' to get 'Congestion'
			resource.Overview = Overview.Full;
			resource.Annotations = Annotations.Distance | Annotations.Duration | Annotations.Speed | Annotations.Congestion;

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			Directions.Leg leg = matchingResponse.Matchings[0].Legs[0];
			Assert.IsNotNull(leg.Annotation, "Annotation is NULL");
			Assert.IsNotNull(leg.Annotation.Distance, "Distance is NULL");
			Assert.IsNotNull(leg.Annotation.Duration, "Duration is NULL");
			Assert.IsNotNull(leg.Annotation.Speed, "Speed is NULL");
			Assert.IsNotNull(leg.Annotation.Congestion, "Congestion is NULL");

			Assert.GreaterOrEqual(leg.Annotation.Distance[1], 42, "Annotation has wrong distnce");
		}


		[UnityTest]
		public IEnumerator Tidy()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.187092481625704,16.312205493450165),
				new Vector2d(48.187083540475875,16.312505900859833),
				new Vector2d(48.18709426985548,16.312503218650818),
				new Vector2d(48.18707281109407,16.312503218650818),
				new Vector2d(48.18709605808517,16.312524676322937),
				new Vector2d(48.18707817578527,16.312530040740967),
				new Vector2d(48.1870656581716,16.312524676322937),
				new Vector2d(48.187079964015524,16.312484443187714),
				new Vector2d(48.18704598762968,16.312776803970337)
			};
			resource.Tidy = true;

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			Tracepoint[] tps = matchingResponse.Tracepoints;
			//tracepoints removed by 'Tidy' are set to 'null'
			Assert.IsNotNull(tps, "Tracepoints is NULL");
			Assert.IsNull(tps[6], "Tracepoints is NULL");
			Assert.IsNull(tps[7], "Tracepoints is NULL");
		}



		[UnityTest]
		public IEnumerator LanguageEnglish()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.1974721043879,16.36202484369278),
				new Vector2d(48.197922645046546,16.36285901069641)
			};
			//set Steps to true to get turn-by-turn-instructions
			resource.Steps = true;
			//no language parameter needed: English is default

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			Directions.Step step0 = matchingResponse.Matchings[0].Legs[0].Steps[0];
			Directions.Step step1 = matchingResponse.Matchings[0].Legs[0].Steps[1];
			Assert.AreEqual("Head northeast on Rechte Wienzeile (B1)", step0.Maneuver.Instruction, "Step[0]:Instruction not as expected");
			Assert.AreEqual("You have arrived at your destination", step1.Maneuver.Instruction, "Step[1]:Instruction not as expected");
		}


		[UnityTest]
		public IEnumerator LanguageGerman()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.1974721043879,16.36202484369278),
				new Vector2d(48.197922645046546,16.36285901069641)
			};
			//set Steps to true to get turn-by-turn-instructions
			resource.Steps = true;
			resource.Language = InstructionLanguages.German;

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			Directions.Step step0 = matchingResponse.Matchings[0].Legs[0].Steps[0];
			Directions.Step step1 = matchingResponse.Matchings[0].Legs[0].Steps[1];
			Assert.AreEqual("Fahren Sie Richtung Nordosten auf Rechte Wienzeile (B1)", step0.Maneuver.Instruction, "Step[0]:Instruction not as expected");
			Assert.AreEqual("Sie haben Ihr Ziel erreicht", step1.Maneuver.Instruction, "Step[1]:Instruction not as expected");
		}



		[UnityTest]
		public IEnumerator AllParameters()
		{

			MapMatchingResource resource = new MapMatchingResource();
			resource.Profile = Profile.MapboxWalking;
			resource.Geometries = Geometries.Polyline6;
			resource.Coordinates = new Vector2d[]
			{
				new Vector2d(48.28585,16.55267),
				new Vector2d(48.28933,16.55211)
			};
			resource.Timestamps = new long[]
			{
				946684800,
				946684980
			};
			resource.Radiuses = new uint[] { 50, 50 };
			//set Steps to true to get turn-by-turn-instructions
			resource.Steps = true;
			//need to pass 'Overview.Full' to get 'Congestion'
			resource.Overview = Overview.Full;
			resource.Annotations = Annotations.Distance | Annotations.Duration | Annotations.Speed | Annotations.Congestion;
			resource.Tidy = true;
			resource.Language = InstructionLanguages.German;


			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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

			Directions.Leg leg = matchingResponse.Matchings[0].Legs[0];
			Assert.IsNotNull(leg.Annotation, "Annotation is NULL");
			Assert.IsNotNull(leg.Annotation.Distance, "Distance is NULL");
			Assert.IsNotNull(leg.Annotation.Duration, "Duration is NULL");
			Assert.IsNotNull(leg.Annotation.Speed, "Speed is NULL");
			Assert.IsNotNull(leg.Annotation.Congestion, "Congestion is NULL");

			Directions.Step step1 = matchingResponse.Matchings[0].Legs[0].Steps[1];
			Assert.IsTrue(step1.Maneuver.Instruction.Contains("Sie haben Ihr Ziel erreicht"), "Step[1]:Instruction not as expected");

		}


		[UnityTest]
		public IEnumerator CoordinatesNull()
		{

			MapMatchingResource resource = new MapMatchingResource();
			Assert.Throws(
				typeof(System.Exception)
				, () => resource.Coordinates = null
				, "MapMatchingResource did not throw when setting null coordinates"
			);

			yield return null;


			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
			MapMatchingResponse matchingResponse = null;

			Assert.Throws(
				typeof(System.Exception)
				, () =>
				{
					mapMatcher.Match(
						resource,
						(MapMatchingResponse response) =>
						{
							matchingResponse = response;
						}
					);
				}
				, "MapMatcher.Match did not throw with null coordinates"
			);

			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }

			Assert.IsNull(matchingResponse, "Matching response was expected to be null");
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

			MapMatcher mapMatcher = new MapMatcher(_fs, _timeout);
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



		#region disabledTests

		//These tests don't work as we don't access the raw response and cannot verify the digits returned by the API
		/*
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
		*/

		#endregion


	}
}

#endif
#endif