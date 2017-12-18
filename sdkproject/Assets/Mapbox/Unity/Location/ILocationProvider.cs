namespace Mapbox.Unity.Location
{
	using System;

	/// <summary>
	/// Implement ILocationProvider to send Heading and Location updates.
	/// </summary>
	public interface ILocationProvider
	{
		event Action<Location> OnLocationUpdated;
		Location CurrentLocation { get; }
	}
}