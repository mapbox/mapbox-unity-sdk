//-----------------------------------------------------------------------
// <copyright file="FileSourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'

namespace Mapbox.CheapRulerCs.UnitTest
{


	using NUnit.Framework;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.CheapRulerCs;
	using Mapbox.Json.Linq;

	[TestFixture]
	internal class CheapRulerCsTest
	{


		// TODO more tests ////////////////////
		// see https://github.com/mapbox/cheap-ruler/blob/master/test/test.js
		//////////////////////////


		internal class point { public double x; public double y; }
		internal class line
		{
			public List<point> vertices = new List<point>();
			public void Add(double x, double y) { vertices.Add(new point() { x = x, y = y }); }
		}

		private List<line> _lineFixtures;

		[SetUp]
		public void SetUp()
		{
			_lineFixtures = loadFixtures();
		}



		[Test, Order(1)]
		public void FixturesLoaded()
		{
			Assert.AreEqual(58, _lineFixtures.Count);
		}



		[Test]
		public void DistanceInMiles()
		{
			CheapRuler ruler = new CheapRuler(32.8351);
			CheapRuler rulerMiles = new CheapRuler(32.8351, CheapRulerUnits.Miles);

			double distKm = ruler.Distance(new double[] { 30.5, 32.8351 }, new double[] { 30.51, 32.8451 });
			double distMiles = rulerMiles.Distance(new double[] { 30.5, 32.8351 }, new double[] { 30.51, 32.8451 });

			Debug.LogFormat("{0} {1}", distKm, distMiles);
			Assert.AreEqual(1.609344, distKm / distMiles, 1e-12, "wrong distance in miles");
		}


		[Test]
		public void DistanceInNauticalMiles()
		{
			CheapRuler ruler = new CheapRuler(32.8351);
			CheapRuler rulerMiles = new CheapRuler(32.8351, CheapRulerUnits.Miles);
			CheapRuler rulerNauticalMiles = new CheapRuler(32.8351, CheapRulerUnits.NauticalMiles);

			double distKm = ruler.Distance(new double[] { 30.5, 32.8351 }, new double[] { 30.51, 32.8451 });
			double distMiles = rulerMiles.Distance(new double[] { 30.5, 32.8351 }, new double[] { 30.51, 32.8451 });
			double distNauticalMiles = rulerNauticalMiles.Distance(new double[] { 30.5, 32.8351 }, new double[] { 30.51, 32.8451 });

			Debug.LogFormat("{0} {1}", distKm, distNauticalMiles);
			Assert.AreEqual(1.852, distKm / distNauticalMiles, 1e-12, "wrong distance km vs nautical miles");
			Assert.AreEqual(1.15078, distMiles / distNauticalMiles, 1e-6, "wrong distance miles vs nautical miles");
		}


		[Test]
		public void FromTile()
		{
			CheapRuler ruler1 = new CheapRuler(50.5);
			CheapRuler ruler2 = CheapRuler.FromTile(11041, 15);

			var p1 = new double[] { 30.5, 50.5 };
			var p2 = new double[] { 30.51, 50.51 };

			Assert.AreEqual(ruler1.Distance(p1, p2), ruler2.Distance(p1, p2), 3e-5, "CheapRuler.FromTile distance");
		}



		private List<line> loadFixtures()
		{
			TextAsset fixturesAsset = Resources.Load<TextAsset>("ChearRulerCs_fixtures");
			var json = JArray.Parse(fixturesAsset.text);
			List<line> fixtures = new List<line>();

			foreach (var line in json)
			{
				line fixtureLine = new line();

				foreach (var coordinates in line)
				{
					fixtureLine.Add(coordinates[0].Value<double>(), coordinates[1].Value<double>());
				}
				fixtures.Add(fixtureLine);
			}

			return fixtures;
		}



	}
}
