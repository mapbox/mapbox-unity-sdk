using System;
using Mapbox.Unity.Utilities;
using UnityEngine;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class MapLocationOptions : MapboxDataProperty
	{
		[Geocode]
		[Tooltip("The coordinates to build a map around")]
		public string latitudeLongitude = "0,0";

		public float MinZoom;
		public float MaxZoom;

		[Tooltip("The zoom level of the map")]
		public float zoom = 4.0f;

		//TODO : Add Coordinate conversion class. 
		[NonSerialized]
		public MapCoordinateSystemType coordinateSystemType = MapCoordinateSystemType.WebMercator;
	}
}
