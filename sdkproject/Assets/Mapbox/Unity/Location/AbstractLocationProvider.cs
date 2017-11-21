namespace Mapbox.Unity.Location
{
	using System;
	using UnityEngine;

	public abstract class AbstractLocationProvider : MonoBehaviour, ILocationProvider
	{
		public event Action<Location> OnLocationUpdated = delegate { };

		protected void SendLocation(Location location)
		{
			OnLocationUpdated(location);
		}
	}
}
