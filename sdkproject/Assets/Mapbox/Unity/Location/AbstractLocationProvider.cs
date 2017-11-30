namespace Mapbox.Unity.Location
{
	using System;
	using UnityEngine;

	public abstract class AbstractLocationProvider : MonoBehaviour, ILocationProvider
	{
		public Location _latestLocation;
		public event Action<Location> OnLocationUpdated = delegate { };

		protected virtual void SendLocation(Location location)
		{
			_latestLocation = location;
			OnLocationUpdated(location);
		}
	}
}