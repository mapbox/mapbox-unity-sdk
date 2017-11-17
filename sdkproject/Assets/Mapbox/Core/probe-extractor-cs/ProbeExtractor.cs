namespace Mapbox.ProbeExtractorCs
{
	using Mapbox.CheapRulerCs;
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
			double[] durations = new double[tracePntCnt];
			double[] distances = new double[tracePntCnt];
			double[] speeds = new double[tracePntCnt];
			double[] bearings = new double[tracePntCnt];

			for (int i = 1; i < tracePntCnt; i++)
			{
				TracePoint current = trace[i];
				TracePoint previous = trace[i - 1];

				durations[i] = (current.Timestamp - previous.Timestamp) / 1000; //seconds

				double[] currLocation = new double[] { current.Longitude, current.Latitude };
				double[] prevLocation = new double[] { previous.Longitude, previous.Latitude };
				distances[i] = _ruler.Distance(currLocation, prevLocation);
				speeds[i] = distances[i] / durations[i] * 3600; //kph

				double[] currBearing = new double[] { current.Bearing, current.Bearing };
				double[] prevBearing = new double[] { previous.Bearing, previous.Bearing };
				double bearing = _ruler.Bearing(prevBearing, currBearing);
				bearings[i] = bearing < 0 ? 360 + bearing : bearing;
			}


			List<Probe> probes = new List<Probe>();

			// 1st pass: iterate trace points and determine if they are good
			// bail early if !_options.OutputBadProbes
			bool negativeDuration = false;
			for (int i = 0; i < speeds.Length; i++)
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
					// if in reverse direction, it's most likely signal jump
					// TODO: implement 'CompareBearing: https://github.com/mapbox/probematch/blob/7cf7719403f7775121507a46ba3c72bcc33edf1d/probematch.js#L202
					UnityEngine.Debug.LogWarning("implement comparebearing");
				}

				if (!isGood && !_options.OutputBadProbes)
				{
					return new List<Probe>();
				}


				if (isGood || _options.OutputBadProbes)
				{
					probes.Add(new Probe()
					{
						//why i-1???
						Latitude = trace[i - 1].Latitude,
						Longitude = trace[i - 1].Longitude,
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
				return probes;
			}

			// 2nd pass, what's this for???


			return probes;
		}


	}
}