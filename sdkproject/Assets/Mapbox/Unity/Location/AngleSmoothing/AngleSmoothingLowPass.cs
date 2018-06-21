namespace Mapbox.Unity.Location
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;


	/// <summary>
	/// Smoothing via low pass filter
	/// </summary>
	public class AngleSmoothingLowPass : AngleSmoothingAbstractBase
	{


		[SerializeField]
		[Tooltip("Factor to change smoothing. The higher the factor the slower the angle changes.")]
		[Range(0.01f, 0.9f)]
		private double _smoothingFactor = 0.5;


		/* v1 implementation 
		private double _lastSinus;
		private double _lastCosinus;
		*/

		public AngleSmoothingLowPass() : base() { }


		public AngleSmoothingLowPass(double smoothingFactor) : base()
		{
			_smoothingFactor = smoothingFactor;
		}


		public override double Calculate()
		{

			//// v1: using just previous angle ////
			/*
			double angle = _angles[0];
			_lastSinus = _smoothingFactor * _lastSinus + (1 - _smoothingFactor) * Math.Sin(angle * Math.PI / 180.0);
			_lastCosinus = _smoothingFactor * _lastCosinus + (1 - _smoothingFactor) * Math.Cos(angle * Math.PI / 180.0);

			double finalAngle = (Math.Atan2(_lastSinus, _lastCosinus) * 180.0 / Math.PI);
			//stay positive ;-)
			if (finalAngle < 0) { finalAngle += 360.0; }

			return finalAngle;
			*/

			//// v2: using sum of collected angles
			// infered from pseudo code: https://en.wikipedia.org/wiki/Low-pass_filter#Simple_infinite_impulse_response_filter


			/*
			double sumSin = 0.0;
			double sumCos = 0.0;

			// reverse order, latest in _angles is at [0]
			for (int i = _angles.Count - 2; i >= 0; i--)
			{
				double angle = _angles[i];
				double prevAngle = _angles[i + 1];

				_lastSinus = _smoothingFactor * Math.Sin(prevAngle * DEG2RAD);
				_lastCosinus = _smoothingFactor * Math.Cos(prevAngle * DEG2RAD);

				_lastSinus = _smoothingFactor * _lastSinus + (1 - _smoothingFactor) * Math.Sin(angle * DEG2RAD);
				_lastCosinus = _smoothingFactor * _lastCosinus + (1 - _smoothingFactor) * Math.Cos(angle * DEG2RAD);

				double debugAngle = Math.Atan2(_lastSinus, _lastCosinus) * RAD2DEG;
				debugAngle = debugAngle < 0 ? debugAngle + 360 : debugAngle > 360 ? debugAngle - 360 : debugAngle;
				Debug.Log(string.Format("{0:0.000} => {1:0.000}", angle, debugAngle));

				sumSin += _lastSinus;
				sumCos += _lastCosinus;
			}

			double finalAngle = Math.Atan2(sumSin / _angles.Count, sumCos / _angles.Count) * RAD2DEG;

			//stay positive ;-)
			if (finalAngle < 0) { finalAngle += 360.0; }
			// safe measure as we don't know which angles were put in
			if (finalAngle >= 360) { finalAngle -= 360; }

			Debug.Log(finalAngle);

			return finalAngle;
			*/


			/*
			// reverse order, latest in _angles is at [0]
			double[] angles = _angles.Reverse().ToArray();

			double lastSinus = Math.Sin(_smoothingFactor * angles[0] * DEG2RAD);
			double lastCosinus = Math.Cos(_smoothingFactor * angles[0] * DEG2RAD);
			double sumSinus = lastSinus;
			double sumCosinus = lastCosinus;

			double debugAngle = Math.Atan2(lastSinus, lastCosinus) * RAD2DEG;
			debugAngle = debugAngle < 0 ? debugAngle + 360 : debugAngle >= 360 ? debugAngle - 360 : debugAngle;
			Debug.Log(string.Format("{0:0.000} => {1:0.000}", angles[0], debugAngle));

			for (int i = 1; i < angles.Length; i++)
			{
				double prevAngle = angles[i - 1];
				double angle = angles[i];
				Debug.Log(angle);

				// wrong: '+ (1 - _smoothingFactor) * lastSinus;'
				// as this would apply smoothing factor to the sinus and not the angle
				lastSinus = Math.Sin(_smoothingFactor * angle * DEG2RAD) + +(1 - _smoothingFactor) * lastSinus;
				lastCosinus = Math.Cos(_smoothingFactor * angle * DEG2RAD) + (1 - _smoothingFactor) * lastCosinus;
				//lastCosinus = Math.Cos(_smoothingFactor * angle * DEG2RAD) + lastCosinus * Math.Cos(((1 - _smoothingFactor) * (_smoothingFactor * prevAngle)) * DEG2RAD);

				debugAngle = Math.Atan2(lastSinus, lastCosinus) * RAD2DEG;
				debugAngle = debugAngle < 0 ? debugAngle + 360 : debugAngle > 360 ? debugAngle - 360 : debugAngle;
				Debug.Log(string.Format("{0:0.000} => {1:0.000}", angle, debugAngle));

				sumSinus += lastSinus;
				sumCosinus += lastCosinus;
			}

			double finalAngle = Math.Atan2(sumSinus / angles.Length, sumCosinus / angles.Length) * RAD2DEG;

			// stay within [0..<360]
			finalAngle = finalAngle < 0 ? finalAngle + 360 : finalAngle >= 360 ? finalAngle - 360 : finalAngle;

			Debug.Log("finalAngle: " + finalAngle);

			return finalAngle;
			*/


			// reverse order, latest in _angles is at [0]
			double[] angles = _angles.Reverse().ToArray();

			double last = _smoothingFactor * angles[0];
			double sum = last;

			debugAngle(angles[0], last);


			for (int i = 1; i < angles.Length; i++)
			{
				double angle = angles[i];
				Debug.Log(angle);

				last = _smoothingFactor * angle + (1 - _smoothingFactor) * last;
				sum += last;
				debugAngle(angle, last);
			}

			sum /= (double)angles.Length;
			double finalAngle = Math.Atan2(Math.Sin(sum * DEG2RAD), Math.Cos(sum * DEG2RAD)) * RAD2DEG;

			// stay within [0..<360]
			finalAngle = finalAngle < 0 ? finalAngle + 360 : finalAngle >= 360 ? finalAngle - 360 : finalAngle;

			Debug.Log("finalAngle: " + finalAngle);

			return finalAngle;




		}


		private void debugAngle(double raw, double smoothed)
		{
			double debugAngle = Math.Atan2(Math.Sin(smoothed * DEG2RAD), Math.Cos(smoothed * DEG2RAD)) * RAD2DEG;
			debugAngle = debugAngle < 0 ? debugAngle + 360 : debugAngle >= 360 ? debugAngle - 360 : debugAngle;
			Debug.Log(string.Format("{0:0.000} => {1:0.000}", raw, smoothed));
		}

	}
}
