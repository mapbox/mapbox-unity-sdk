namespace Mapbox.Unity.Map
{
	using Mapbox.Utils;
	using UnityEngine;

	// TODO: split into read and write maps?
	public interface IMap
	{
		Vector2d CenterLatitudeLongitude { get; set;}
		int Zoom { get; set; }
		Vector2d CenterMercator { get;}
		float WorldRelativeScale { get; }
		Transform Root { get; }
	}
}