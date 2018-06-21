namespace Mapbox.Unity.Location
{


	using Mapbox.Utils;
	using System;
	using UnityEngine;


	/// <summary>
	/// Base class for implementing different smoothing strategies
	/// </summary>
	public abstract class AngleSmoothingAbstractBase : MonoBehaviour, IAngleSmoothing
	{


		[SerializeField]
		[Tooltip("Number of measurements used for smoothing. Keep that number as low as feasible as collection of measurements depends on update time of location provider (minimum 500ms). eg 6 smoothes over the last 3 seconds.")]
		[Range(5, 20)]
		public int _measurements = 5;


		public AngleSmoothingAbstractBase()
		{
			_angles = new CircularBuffer<double>(_measurements);
		}


		/// <summary>
		/// Internal storage for latest 'n' values. Latest value at [0], <see cref="Mapbox.Utils.CircularBuffer{T}"/>
		/// </summary>
		protected CircularBuffer<double> _angles;


		/// <summary>
		/// For conversions from degrees to radians needed for Math functions.
		/// </summary>
		protected readonly double DEG2RAD = Math.PI / 180.0d;


		/// <summary>
		/// For conversions from radians to degrees.
		/// </summary>
		protected readonly double RAD2DEG = 180.0d / Math.PI;


		/// <summary>
		/// Add angle to list of angles used for calculation.
		/// </summary>
		/// <param name="angle"></param>
		public void Add(double angle) {
			// safe measures to stay within [0..360]
			if (angle < 0) { angle += 360; }
			if (angle >= 360) { angle -= 360; }
			_angles.Add(angle);
		}


		/// <summary>
		/// Calculate smoothed angle from previously added angles.
		/// </summary>
		/// <returns>Smoothed angle</returns>
		public abstract double Calculate();


	}
}
