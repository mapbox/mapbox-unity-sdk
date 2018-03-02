namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.Utilities;
	[Serializable]
	public class MapLocationOptions
	{
		[Geocode]
		//[SerializeField]
		public string latitudeLongitude = "0,0";
		[Range(0, 22)]
		public float zoom = 4.0f;

		//TODO : Add Coordinate conversion class. 
		[NonSerialized]
		public MapCoordinateSystemType coordinateSystemType;
	}
}
