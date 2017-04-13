using System;
using UnityEngine;

namespace Scripts.Location
{
	public interface ILocationProvider
	{
		event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;
		event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;
		Vector2 Location { get; }
	}

	public class LocationUpdatedEventArgs: EventArgs
	{
		public Vector2 Location;
	}

	public class HeadingUpdatedEventArgs : EventArgs
	{
		public float Heading;
	}

	class LocationProviderMissingException : Exception
	{
		public LocationProviderMissingException(string message) : base(message)
		{
		}
	}
}
