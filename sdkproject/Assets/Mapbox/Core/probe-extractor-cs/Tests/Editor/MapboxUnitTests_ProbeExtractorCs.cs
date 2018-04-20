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
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System;
	using System.Linq;
	using UnityEngine;
	using Mapbox.CheapRulerCs;

	[TestFixture]
	internal class ProbeExtractorCsTest
	{

		private List<TracePoint> _trace;
		private List<TracePoint> _footTrace;
		private List<Probe> _probes;

		[SetUp]
		public void SetUp()
		{
			_trace = loadTraceFixture("ProbeExtractorCs_fixture_trace");
			_footTrace = loadTraceFixture("ProbeExtractorCs_fixture_trace-foot");
			_probes = loadProbeFixture("ProbeExtractorCs_fixture_probes");
		}



		[Test, Order(1)]
		public void FixturesLoaded()
		{
			Assert.AreEqual(14, _trace.Count);
			Assert.AreEqual(12, _probes.Count);
		}


		[Test]
		public void ExtractProbes()
		{
			CheapRuler ruler = CheapRuler.FromTile(49, 7);

			ProbeExtractorOptions options = new ProbeExtractorOptions()
			{
				MinTimeBetweenProbes = 1, // seconds
				MaxDistanceRatioJump = 3, // do not include probes when the distance is 3 times bigger than the previous one
				MaxDurationRatioJump = 3, // do not include probes when the duration is 3 times bigger than the previous one
				MaxAcceleration = 15, // meters per second per second
				MaxDeceleration = 18 // meters per second per second
			};

			ProbeExtractor extractor = new ProbeExtractor(ruler, options);
			List<Probe> extractedProbes = extractor.ExtractProbes(_trace);

			Assert.AreEqual(12, extractedProbes.Count, "12 probes were expected to be extracted");

			for (int i = 0; i < extractedProbes.Count; i++)
			{
				Probe fp = _probes[i]; // fixture probe
				Probe ep = extractedProbes[i]; // extracted probe

				Assert.AreEqual(fp.Longitude, ep.Longitude, 0.001, "probe[" + i.ToString() + "]: longitude doesn't match");
				Assert.AreEqual(fp.Latitude, ep.Latitude, 0.001, "probe[" + i.ToString() + "]: latitude doesn't match");
				Assert.AreEqual(fp.StartTime, ep.StartTime, "probe[" + i.ToString() + "]: start time doesn't match");
				Assert.AreEqual(fp.Duration, ep.Duration, "probe[" + i.ToString() + "]: duration doesn't match");
				Assert.AreEqual(fp.Speed, ep.Speed, 0.001, "probe[" + i.ToString() + "]: speed doesn't match");
				Assert.AreEqual(fp.Bearing, ep.Bearing, 0.001, "probe[" + i.ToString() + "]: bearing doesn't match");
				Assert.AreEqual(fp.Distance, ep.Distance, 0.001, "probe[" + i.ToString() + "]: distance doesn't match");
				Assert.AreEqual(fp.IsGood, ep.IsGood, "probe[" + i.ToString() + "]: longitude doesn't match");
			}


			options.MinTimeBetweenProbes = 2;
			extractor = new ProbeExtractor(ruler, options);
			extractedProbes = extractor.ExtractProbes(_trace);

			Assert.AreEqual(5, extractedProbes.Count, "5 probes were expected to be extracted");


			options.OutputBadProbes = true;
			extractor = new ProbeExtractor(ruler, options);
			extractedProbes = extractor.ExtractProbes(_trace);

			Assert.AreEqual(13, extractedProbes.Count, "13 probes were expected to be extracted");
		}


		[Test]
		public void ExtractFootTrace()
		{
			CheapRuler ruler = new CheapRuler(_footTrace[0].Latitude);
			ProbeExtractorOptions options = new ProbeExtractorOptions();

			ProbeExtractor extractor = new ProbeExtractor(ruler, options);
			List<Probe> extractedProbes = extractor.ExtractProbes(_footTrace);

			//Debug.Log(probesToGeojson(extractedProbes));

			Assert.AreEqual(40, extractedProbes.Count);
		}


		private List<TracePoint> loadTraceFixture(string fixtureName)
		{
			TextAsset fixtureAsset = Resources.Load<TextAsset>(fixtureName);
			List<TracePoint> trace = new List<TracePoint>();
			using (StringReader sr = new StringReader(fixtureAsset.text))
			{
				// skip header
				sr.ReadLine();
				string line;
				while (null != (line = sr.ReadLine()))
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
					long timestamp;

					if (!double.TryParse(tokens[0], NumberStyles.Any, CultureInfo.InvariantCulture, out lng)) { Debug.LogWarning("could not parse longitude"); continue; }
					if (!double.TryParse(tokens[1], NumberStyles.Any, CultureInfo.InvariantCulture, out lat)) { Debug.LogWarning("could not parse latitude"); continue; }
					if (!double.TryParse(tokens[2], NumberStyles.Any, CultureInfo.InvariantCulture, out bearing)) { Debug.LogWarning("could not parse bearing"); continue; }
					if (!long.TryParse(tokens[3], NumberStyles.Any, CultureInfo.InvariantCulture, out timestamp)) { Debug.LogWarning("could not parse timestamp"); continue; }

					trace.Add(new TracePoint()
					{
						Longitude = lng,
						Latitude = lat,
						Bearing = bearing,
						Timestamp = timestamp
					});
				}
			}
			return trace;
		}


		private List<Probe> loadProbeFixture(string fixtureName)
		{
			TextAsset fixtureAsset = Resources.Load<TextAsset>(fixtureName);
			List<Probe> probes = new List<Probe>();
			using (StringReader sr = new StringReader(fixtureAsset.text))
			{
				// skip header
				sr.ReadLine();
				string line;
				while (null != (line = sr.ReadLine()))
				{
					string[] tokens = line.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length != 8)
					{
						Debug.LogWarning("probes.csv has wrong number of columns");
						continue;
					}

					double lng;
					double lat;
					long startTime;
					long duration;
					double speed;
					double bearing;
					double distance;
					bool isGood;

					if (!double.TryParse(tokens[0], NumberStyles.Any, CultureInfo.InvariantCulture, out lng)) { Debug.LogWarning("could not parse longitude"); continue; }
					if (!double.TryParse(tokens[1], NumberStyles.Any, CultureInfo.InvariantCulture, out lat)) { Debug.LogWarning("could not parse latitude"); continue; }
					if (!long.TryParse(tokens[2], NumberStyles.Any, CultureInfo.InvariantCulture, out startTime)) { Debug.LogWarning("could not parse timestamp"); continue; }
					if (!long.TryParse(tokens[3], NumberStyles.Any, CultureInfo.InvariantCulture, out duration)) { Debug.LogWarning("could not parse duration"); continue; }
					if (!double.TryParse(tokens[4], NumberStyles.Any, CultureInfo.InvariantCulture, out speed)) { Debug.LogWarning("could not parse speed"); continue; }
					if (!double.TryParse(tokens[5], NumberStyles.Any, CultureInfo.InvariantCulture, out bearing)) { Debug.LogWarning("could not parse bearing"); continue; }
					if (!double.TryParse(tokens[6], NumberStyles.Any, CultureInfo.InvariantCulture, out distance)) { Debug.LogWarning("could not parse distance"); continue; }
					if (!bool.TryParse(tokens[7], out isGood)) { Debug.LogWarning("could not parse good"); continue; }

					probes.Add(new Probe()
					{
						Longitude = lng,
						Latitude = lat,
						StartTime = startTime,
						Duration = duration,
						Speed = speed,
						Bearing = bearing,
						Distance = distance,
						IsGood = isGood
					});
				}
			}
			return probes;
		}


		// quick hack for visualizing output of ProbeExtractor on http://geojson.io
		private string probesToGeojson(List<Probe> probes)
		{
			StringBuilder sb = new StringBuilder();

			// line
			sb.Append("{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"properties\":{},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[");
			sb.Append(string.Join(",", probes.Select(p => string.Format(CultureInfo.InvariantCulture, "[{0},{1}]", p.Longitude, p.Latitude)).ToArray()));
			sb.Append("]}}");

			//points - too much noise - find a way to just show small circles on vertices
			/*
			List<string> points = new List<string>();
			foreach (var p in probes)
			{
				points.Add(string.Format(
					CultureInfo.InvariantCulture
					, "{{\"type\":\"Feature\",\"properties\":{{\"marker-size\":\"small\",\"marker-symbol\":\"star\"}},\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{0},{1}]}}}}"
					, p.Longitude
					, p.Latitude
				));
			}
			sb.Append("," + string.Join(",", points.ToArray()));
			*/

			sb.Append("]}");
			return sb.ToString();
		}

	}
}
