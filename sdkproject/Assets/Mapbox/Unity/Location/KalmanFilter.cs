/*
From SackOverflow: Smooth GPS data https://stackoverflow.com/a/15657798

General Kalman filter theory is all about estimates for vectors, with the accuracy of the estimates
represented by covariance matrices. However, for estimating location on Android devices the general
theory reduces to a very simple case. Android location providers give the location as a latitude and
longitude, together with an accuracy which is specified as a single number measured in metres. 
This means that instead of a covariance matrix, the accuracy in the Kalman filter can be measured by
a single number, even though the location in the Kalman filter is a measured by two numbers. Also the
fact that the latitude, longitude and metres are effectively all different units can be ignored, 
because if you put scaling factors into the Kalman filter to convert them all into the same units, 
then those scaling factors end up cancelling out when converting the results back into the original units.

The code could be improved, because it assumes that the best estimate of current location is the last
known location, and if someone is moving it should be possible to use Android's sensors to produce a 
better estimate. The code has a single free parameter Q, expressed in metres per second, which describes 
how quickly the accuracy decays in the absence of any new location estimates. A higher Q parameter means 
that the accuracy decays faster. Kalman filters generally work better when the accuracy decays a bit 
quicker than one might expect, so for walking around with an Android phone I find that Q=3 metres per 
second works fine, even though I generally walk slower than that. But if travelling in a fast car a much 
larger number should obviously be used.
*/

namespace Mapbox.Unity.Location
{

	using System;


	/// <summary>
	/// <para>From SackOverflow: Smooth GPS data</para>
	/// <para>https://stackoverflow.com/a/15657798</para>
	/// </summary>
	public class KalmanLatLong
	{

		private float _minAccuracy = 1;
		private float _qMetresPerSecond;
		private long _timeStampMilliseconds;
		private double _lat;
		private double _lng;
		private float _variance; // P matrix.  Negative means object uninitialised.  NB: units irrelevant, as long as same units used throughout


		public KalmanLatLong(float Q_metres_per_second)
		{
			_qMetresPerSecond = Q_metres_per_second;
			_variance = -1;
		}


		public long TimeStamp { get { return _timeStampMilliseconds; } }

		public double Lat { get { return _lat; } }

		public double Lng { get { return _lng; } }

		public float Accuracy { get { return (float)Math.Sqrt(_variance); } }



		public void SetState(double lat, double lng, float accuracy, long TimeStamp_milliseconds)
		{
			_lat = lat;
			_lng = lng;
			_variance = accuracy * accuracy;
			_timeStampMilliseconds = TimeStamp_milliseconds;
		}



		/// <summary>
		/// Kalman filter processing for lattitude and longitude
		/// </summary>
		/// <param name="lat_measurement_degrees">new measurement of lattidude</param>
		/// <param name="lng_measurement">new measurement of longitude</param>
		/// <param name="accuracy">measurement of 1 standard deviation error in metres</param>
		/// <param name="TimeStamp_milliseconds">time of measurement</param>
		/// <returns>new state</returns>
		public void Process(double lat_measurement, double lng_measurement, float accuracy, long TimeStamp_milliseconds)
		{
			if (accuracy < _minAccuracy)
			{
				accuracy = _minAccuracy;
			}

			if (_variance < 0)
			{
				// if variance < 0, object is unitialised, so initialise with current values
				_timeStampMilliseconds = TimeStamp_milliseconds;
				_lat = lat_measurement; _lng = lng_measurement; _variance = accuracy * accuracy;
			}
			else
			{
				// else apply Kalman filter methodology

				long TimeInc_milliseconds = TimeStamp_milliseconds - TimeStamp_milliseconds;
				if (TimeInc_milliseconds > 0)
				{
					// time has moved on, so the uncertainty in the current position increases
					_variance += TimeInc_milliseconds * _qMetresPerSecond * _qMetresPerSecond / 1000;
					_timeStampMilliseconds = TimeStamp_milliseconds;
					// TO DO: USE VELOCITY INFORMATION HERE TO GET A BETTER ESTIMATE OF CURRENT POSITION
				}

				// Kalman gain matrix K = Covarariance * Inverse(Covariance + MeasurementVariance)
				// NB: because K is dimensionless, it doesn't matter that variance has different units to lat and lng
				float K = _variance / (_variance + accuracy * accuracy);
				// apply K
				_lat += K * (lat_measurement - _lat);
				_lng += K * (lng_measurement - _lng);
				// new Covarariance  matrix is (IdentityMatrix - K) * Covarariance 
				_variance = (1 - K) * _variance;
			}
		}
	}
}