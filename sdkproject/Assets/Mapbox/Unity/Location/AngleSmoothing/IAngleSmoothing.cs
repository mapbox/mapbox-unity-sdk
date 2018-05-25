namespace Mapbox.Unity.Location
{

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public interface IAngleSmoothing
	{

		void Add(double angle);
		double Calculate();

	}
}
