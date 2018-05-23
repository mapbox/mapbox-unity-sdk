namespace Mapbox.Unity.Location
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

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
