//-----------------------------------------------------------------------
// <copyright file="FileSourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'

namespace Mapbox.CheapRulerCs.UnitTest
{


	using Mapbox.Platform;
	using NUnit.Framework;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System;
	using System.Linq;
	using UnityEngine;
	using Mapbox.CheapRulerCs;
	using Mapbox.Json.Linq;

	[TestFixture]
	internal class ProbeExtractorCsTest
	{


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

			Assert.AreEqual(1.609344, distKm / distMiles, 1e-12, "wrong distance in miles");
		}




		private List<line> loadFixtures()
		{
			string fixturePath = Application.dataPath + "/Mapbox/Core/cheap-ruler-cs/Tests/Editor/lines.json";
			string fixtureAsText;
			using (TextReader tw = new StreamReader(fixturePath, Encoding.UTF8)) { fixtureAsText = tw.ReadToEnd(); }

			var json = JArray.Parse(fixtureAsText);
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