namespace Mapbox.Unity.Map
{
	using Mapbox.Utils;
	using UnityEngine;

	public interface IMap
	{
		Vector2d CenterLatitudeLongitude { get; set;}
		int Zoom { get; set; }
		Vector2d CenterMercator { get;}
		float WorldRelativeScale { get; }
		Transform Root { get; }
	}
}
