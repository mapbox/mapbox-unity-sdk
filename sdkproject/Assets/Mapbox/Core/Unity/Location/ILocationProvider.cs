namespace Mapbox.Unity.Location
{
    using System;
    using Mapbox.Utils;

    /// <summary>
    /// Implement ILocationProvider to send Heading and Location updates.
    /// </summary>
    public interface ILocationProvider
    {
        event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;
        event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;
        Vector2d Location { get; }
    }

    /// <summary>
    /// Location updated event arguments. 
    /// </summary>
	public class LocationUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// The location, as descibed by a <see cref="T:Mapbox.Utils.Vector2d"/>. 
        /// Location.x represents Latitude.
        /// Location.y represents Longitude.
        /// </summary>
        public Vector2d Location;
    }

    /// <summary>
    /// Heading updated event arguments.
    /// Heading represents a facing angle, generally between 0-359.
    /// </summary>
	public class HeadingUpdatedEventArgs : EventArgs
    {
        public float Heading;
    }
}
