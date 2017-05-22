namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Utils;
	using UnityEngine;

	// TODO: split into read and write maps?
	public interface IMap
	{
		// Writable.
		Vector2d CenterLatitudeLongitude { get; set; }
		int Zoom { get; set; }

		// Readable.
		Vector2d CenterMercator { get; }
		float WorldRelativeScale { get; }
		Transform Root { get; }
		event Action OnInitialized;
	}
}