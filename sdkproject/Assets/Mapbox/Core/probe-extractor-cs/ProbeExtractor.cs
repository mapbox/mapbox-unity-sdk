﻿namespace Mapbox.ProbeExtractorCs
{
	using Mapbox.CheapRulerCs;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	public struct ProbeExtractorOptions
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="minTimeBetweenProbes">seconds</param>
		/// <param name="maxDistanceRatioJump">do not include probes when the distance is X times bigger than the previous one</param>
		/// <param name="maxDurationRatioJump">do not include probes when the duration is X times bigger than the previous one</param>
		/// <param name="maxAcceleration">meters per second per second</param>
		/// <param name="maxDeceleration">meters per second per second</param>
		/// <param name="minProbes"></param>
		/// <param name="ouputBadProbes"></param>
		public ProbeExtractorOptions(
			double minTimeBetweenProbes = 0,
			double maxDistanceRatioJump = double.MaxValue,
			double maxDurationRatioJump = double.MaxValue,
			double maxAcceleration = double.MaxValue,
			double maxDeceleration = double.MaxValue,
			int minProbes = 2,
			bool ouputBadProbes = false
		)
		{
			MinTimeBetweenProbes = minTimeBetweenProbes;
			MaxDistanceRatioJump = maxDistanceRatioJump;
			MaxDurationRatioJump = maxDurationRatioJump;
			MaxAcceleration = maxAcceleration;
			MaxDeceleration = maxDeceleration;
			MinProbes = minProbes;
			OutputBadProbes = ouputBadProbes;
		}

		public double MinTimeBetweenProbes;
		public double MaxDistanceRatioJump;
		public double MaxDurationRatioJump;
		public double MaxAcceleration;
		public double MaxDeceleration;
		public int MinProbes;
		public bool OutputBadProbes;

	}


	/// <summary>
	/// <para>This module allows to pass a list of trace points and extract its probes and their properties.</para>
	/// <para>It can also act as a filter for those probes.</para>
	/// </summary>
	public class ProbeExtractor
	{

		private CheapRuler _ruler;
		private ProbeExtractorOptions _options;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ruler">A CheapRuler instance, expected in kilometers.</param>
		/// <param name="options"></param>
		public ProbeExtractor(CheapRuler ruler, ProbeExtractorOptions options)
		{
			_ruler = ruler;
			_options = options;
		}


		/// <summary>
		/// Extract probes according to ProbeExtractorOptions.
		/// </summary>
		/// <param name="trace">List of trace points</param>
		/// <returns>List of probes. Empty list if no trace point matched the options.</returns>
		public List<Probe> ExtractProbes(List<TracePoint> trace)
		{
			int tracePntCnt = trace.Count;
			double[] durations = new double[tracePntCnt - 1];
			double[] distances = new double[tracePntCnt - 1];
			double[] speeds = new double[tracePntCnt - 1];
			double[] bearings = new double[tracePntCnt - 1];

			for (int i = 1; i < tracePntCnt; i++)
			{
				TracePoint current = trace[i];
				TracePoint previous = trace[i - 1];
				int insertIdx = i - 1;

				durations[insertIdx] = (current.Timestamp - previous.Timestamp) / 1000; //seconds

				double[] currLocation = new double[] { current.Longitude, current.Latitude };
				double[] prevLocation = new double[] { previous.Longitude, previous.Latitude };
				distances[insertIdx] = _ruler.Distance(currLocation, prevLocation);
				speeds[insertIdx] = distances[insertIdx] / durations[insertIdx] * 3600; //kph

				double[] currBearing = new double[] { current.Bearing, current.Bearing };
				double[] prevBearing = new double[] { previous.Bearing, previous.Bearing };
				double bearing = _ruler.Bearing(prevBearing, currBearing);
				bearings[insertIdx] = bearing < 0 ? 360 + bearing : bearing;
			}


			List<Probe> probes = new List<Probe>();

			// 1st pass: iterate trace points and determine if they are good
			// bail early if !_options.OutputBadProbes
			bool negativeDuration = false;
			for (int i = 1; i < speeds.Length; i++)
			{
				//assume tracpoint is good
				bool isGood = true;
				if (negativeDuration)
				{
					// if trace already has a negative duration, then all probes are bad
					isGood = false;
				}
				else if (durations[i] < 0)
				{
					// if a trace has negative duration, the trace is likely noisy
					// bail, if we don't want bad probes
					if (!_options.OutputBadProbes) { return new List<Probe>(); }

					negativeDuration = true;
					isGood = false;
				}
				else if (durations[i] < _options.MinTimeBetweenProbes)
				{
					// if shorter than the minTimeBetweenProbes, filter.
					isGood = false;
				}
				else if (durations[i] > _options.MaxDurationRatioJump * durations[i - 1])
				{
					// if not a gradual decrease in sampling frequency, it's most likely a signal jump
					isGood = false;
				}
				else if (speeds[i] - speeds[i - 1] > _options.MaxAcceleration * durations[i])
				{
					// if accelerating faster than maxAcceleration, it's most likely a glitch
					isGood = false;
				}
				else if (speeds[i - 1] - speeds[i] > _options.MaxDeceleration * durations[i])
				{
					// if decelerating faster than maxDeceleration, it's most likely a glitch
					isGood = false;
				}
				else
				{
					bool isForwardDirection = compareBearing(bearings[i - 1], bearings[i], 89, false);
					if (!isForwardDirection)
					{
						isGood = false;
					}
				}

				//if (!isGood && !_options.OutputBadProbes)
				//{
				//	return new List<Probe>();
				//}

				UnityEngine.Debug.Log(string.Format("{0} good:{1}", i, isGood));

				if (isGood || _options.OutputBadProbes)
				{
					//why this??
					double[] coords = _ruler.Destination(
						//why previous coords: i-1???
						new double[] { trace[i - 1].Longitude, trace[i - 1].Latitude },
						//why half the distance?
						distances[i] / 2,
						bearings[i]
					);

					probes.Add(new Probe()
					{

						Latitude = coords[1],
						Longitude = coords[0],
						// ?? makes sense: from previous point
						StartTime = trace[i - 1].Timestamp,
						Duration = durations[i],
						Distance = distances[i],
						Speed = speeds[i],
						Bearing = bearings[i],
						IsGood = isGood
					});
				}
			}

			// if too few good probes, drop entire trace
			if (!_options.OutputBadProbes && probes.Count < _options.MinProbes)
			{
				return new List<Probe>();
			}

			// MinProbes can be 0, return
			if (probes.Count == 0 && _options.MinProbes == 0)
			{
				return new List<Probe>();
			}


			// 2nd pass, what's this for???????

			// require at least two probes
			if (probes.Count > 1)
			{
				// check first probe in a trace against the average of first two good probes
				var avgSpeed = (probes[0].Speed + probes[1].Speed) / 2;
				var avgDistance = (probes[0].Distance + probes[1].Distance) / 2;
				var avgDuration = (probes[0].Duration + probes[1].Duration) / 2;
				var avgBearing = averageAngle(probes[0].Bearing, probes[1].Bearing);

				bool good = true;

				if (negativeDuration)
				{
					if (!_options.OutputBadProbes)
					{
						return new List<Probe>();
					}

					negativeDuration = true;
					good = false;

					// if a trace has negative duration, the trace is likely noisy
				}
				else if (durations[0] < 0)
				{
					good = false;
				}
				else if (durations[0] < _options.MinTimeBetweenProbes)
				{
					// if shorter than the minTimeBetweenProbes, filter.
					good = false;
				}
				else if (distances[0] > _options.MaxDistanceRatioJump * avgDistance)
				{
					// if not a gradual increase in distance, it's most likely a signal jump
					good = false;
				}
				else if (durations[0] > _options.MaxDurationRatioJump * avgDuration)
				{
					// if not a gradual decrease in sampling frequency, it's most likely a signal jump
					good = false;
				}
				else if (avgSpeed - speeds[0] > _options.MaxAcceleration * durations[0])
				{
					// if accelerating faster than maxAcceleration, it's most likely a glitch
					good = false;
				}
				else if (speeds[0] - avgSpeed > _options.MaxDeceleration * durations[0])
				{
					// if decelerating faster than maxDeceleration, it's most likely a glitch
					good = false;
				}
				else
				{
					// if in reverse direction, it's most likely signal jump
					bool isForwardDirection = compareBearing(bearings[0], avgBearing, 89, false);
					if (!isForwardDirection)
					{
						good = false;
					}
				}

				if (good || _options.OutputBadProbes)
				{
					// create first probe
					//probe = createProbe(prevCoords[0], durations[0], distances[0], speeds[0], bearings[0], ruler, good);

					// need to prepend to track ways
					//probes.unshift(probe);   // prepend

					UnityEngine.Debug.LogWarning("prepending probe: TODO: mingle coords as above ");
					probes.Insert(
						0,
						new Probe()
						{
							Latitude = trace[0].Latitude,
							Longitude = trace[0].Longitude,
							StartTime = trace[0].Timestamp,
							Duration = durations[0],
							Distance = distances[0],
							Speed = speeds[0],
							Bearing = bearings[0],
							IsGood = good
						}
					);
				}
			}

			return probes;
		}


		/// <summary>
		/// Computes the average of two angles.
		/// </summary>
		/// <param name="a">First angle.</param>
		/// <param name="b">Second angle</param>
		/// <returns>Angle midway between a and b.</returns>
		private double averageAngle(double a, double b)
		{
			var anorm = normalizeAngle(a);
			var bnorm = normalizeAngle(b);

			var minAngle = Math.Min(anorm, bnorm);
			var maxAngle = Math.Max(anorm, bnorm);

			var dist1 = Math.Abs(a - b);
			var dist2 = (minAngle + (360 - maxAngle));

			if (dist1 <= dist2) { return normalizeAngle(minAngle + dist1 / 2); }
			else
			{
				return normalizeAngle(maxAngle + dist2 / 2);
			}
		}


		/// <summary>
		/// Map angle to positive modulo 360 space.
		/// </summary>
		/// <param name="angle">An angle in degrees</param>
		/// <returns>Equivalent angle in [0-360] space.</returns>
		private double normalizeAngle(double angle)
		{
			return (angle < 0) ? (angle % 360) + 360 : (angle % 360);
		}


		/// <summary>
		/// Compare bearing `baseBearing` to `bearing`, to determine if they are close enough to each other to be considered matching.
		/// </summary>
		/// <param name="baseBearing">Base bearing</param>
		/// <param name="bearing">Number of degrees difference that is allowed between the bearings.</param>
		/// <param name="range"></param>
		/// <param name="allowReverse">allows bearings that are 180 degrees +/- `range` to be considered matching</param>
		/// <returns></returns>
		private bool compareBearing(double baseBearing, double bearing, double range, bool allowReverse)
		{

			// map base and bearing into positive modulo 360 space
			var normalizedBase = normalizeAngle(baseBearing);
			var normalizedBearing = normalizeAngle(bearing);

			var min = normalizeAngle(normalizedBase - range);
			var max = normalizeAngle(normalizedBase + range);

			if (min < max)
			{
				if (min <= normalizedBearing && normalizedBearing <= max)
				{
					return true;
				}
			}
			else if (min <= normalizedBearing || normalizedBearing <= max)
			{
				return true;
			}

			if (allowReverse)
			{
				return compareBearing(normalizedBase + 180, bearing, range, false);
			}

			return false;
		}

	}
}