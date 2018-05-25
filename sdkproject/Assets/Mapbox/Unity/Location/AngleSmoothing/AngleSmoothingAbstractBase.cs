namespace Mapbox.Unity.Location
{
	using Mapbox.Utils;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public abstract class AngleSmoothingAbstractBase : MonoBehaviour, IAngleSmoothing
	{

		[SerializeField]
		[Tooltip("Number of measurements used for smoothing. Keep that number as low as feasible as collection of measurements depends on update time of location provider (minimum 500ms). eg 6 smoothes over the last 3 seconds.")]
		public int _measurements = 5;


		public AngleSmoothingAbstractBase()
		{
			_angles = new CircularBuffer<double>(_measurements);
		}


		protected CircularBuffer<double> _angles;

		/// <summary>
		/// Add angle to list of angles used for calculation.
		/// </summary>
		/// <param name="angle"></param>
		public void Add(double angle) { _angles.Add(angle); }


		/// <summary>
		/// Calculate smoothed angle from previously added angles.
		/// </summary>
		/// <returns>Smoothed angle</returns>
		public abstract double Calculate();


	}
}
