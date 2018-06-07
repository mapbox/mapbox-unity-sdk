namespace Mapbox.Unity.Location
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
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

		private double _lastSinus;
		private double _lastCosinus;


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

			double sumSin = 0.0;
			double sumCos = 0.0;


			for (int i = _angles.Count - 2; i >= 0; i--)
			{
				double angle = _angles[i];
				double prevAngle = _angles[i + 1];

				_lastSinus = _smoothingFactor * Math.Sin(prevAngle * Math.PI / 180.0);
				_lastCosinus = _smoothingFactor * Math.Cos(prevAngle * Math.PI / 180.0);

				sumSin += _smoothingFactor * _lastSinus + (1 - _smoothingFactor) * Math.Sin(angle * Math.PI / 180.0);
				sumCos += _smoothingFactor * _lastCosinus + (1 - _smoothingFactor) * Math.Cos(angle * Math.PI / 180.0);
			}

			double finalAngle = Math.Atan2(sumSin / _angles.Count, sumCos / _angles.Count) * 180.0 / Math.PI;
			//stay positive ;-)
			if (finalAngle < 0) { finalAngle += 360.0; }

			return finalAngle;
		}


	}
}
