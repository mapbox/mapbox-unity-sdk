namespace Mapbox.Unity.Location
{
    using System;
    using Mapbox.Utils;

    public interface ILocationProvider
	{
		event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;
		event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;
		Vector2d Location { get; }
	}

	public class LocationUpdatedEventArgs: EventArgs
	{
		public Vector2d Location;
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
