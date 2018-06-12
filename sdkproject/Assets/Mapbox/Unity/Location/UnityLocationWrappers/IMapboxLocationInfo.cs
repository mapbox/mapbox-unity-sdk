namespace Mapbox.Unity.Location
{


	public interface IMapboxLocationInfo
	{

		float latitude { get; }

		float longitude { get; }

		float altitude { get; }

		float horizontalAccuracy { get; }

		float verticalAccuracy { get; }

		double timestamp { get; }
	}
}
