//-----------------------------------------------------------------------
// <copyright file="FileSourceTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'

namespace Mapbox.ProbeExtractorCs.UnitTest
{


	using Mapbox.Platform;
	using NUnit.Framework;
	using UnityEngine.TestTools;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System;
	using UnityEngine;
	using System.Globalization;
	using Mapbox.CheapRulerCs;

	[TestFixture]
	internal class ProbeExtractorCsTest
	{

		private List<TracePoint> _trace;

		[SetUp]
		public void SetUp()

		{
			string fixture = Application.dataPath + "/Mapbox/Core/probe-extractor-cs/Tests/Editor/trace.csv";
			_trace = new List<TracePoint>();
			using (TextReader tw = new StreamReader(fixture, Encoding.UTF8))
			{
				string line;
				while (null != (line = tw.ReadLine()))
				{
					string[] tokens = line.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length != 4)
					{
						Debug.LogWarning("trace.csv has wrong number of columns");
						continue;
					}

					double lng;
					double lat;
					double bearing;
					long unixTimestamp;

					if (!double.TryParse(tokens[0], NumberStyles.Any, CultureInfo.InvariantCulture, out lng)) { Debug.LogWarning("could not parse longitude"); continue; }
					if (!double.TryParse(tokens[1], NumberStyles.Any, CultureInfo.InvariantCulture, out lat)) { Debug.LogWarning("could not parse latitude"); continue; }
					if (!double.TryParse(tokens[2], NumberStyles.Any, CultureInfo.InvariantCulture, out bearing)) { Debug.LogWarning("could not parse bearing"); continue; }
					if (!long.TryParse(tokens[3], NumberStyles.Any, CultureInfo.InvariantCulture, out unixTimestamp)) { Debug.LogWarning("could not parse timestamp"); continue; }

					_trace.Add(new TracePoint()
					{
						Longitude = lng,
						Latitude = lat,
						Bearing = bearing,
						Timestamp = unixTimestamp
					});
				}
			}
		}



		[Test, Order(1)]
		public void AllTracePointsLoaded()
		{
			Assert.AreEqual(14, _trace.Count);
		}


		[Test]
		public void yada()
		{
			CheapRuler ruler = CheapRuler.FromTile(49, 7);

			ProbeExtractorOptions options = new ProbeExtractorOptions(
				//ouputBadProbes: true,
				minTimeBetweenProbes: 1, // seconds
				maxDistanceRatioJump: 3, // do not include probes when the distance is 3 times bigger than the previous one
				maxDurationRatioJump: 3, // do not include probes when the duration is 3 times bigger than the previous one
				maxAcceleration: 15, // meters per second per second
				maxDeceleration: 18 // meters per second per second
			);

			ProbeExtractor extractor = new ProbeExtractor(ruler, options);
			List<Probe> probes = extractor.ExtractProbes(_trace);

			Assert.AreEqual(12, probes.Count, "12 probes were expected to be extracted");
		}


	}
}