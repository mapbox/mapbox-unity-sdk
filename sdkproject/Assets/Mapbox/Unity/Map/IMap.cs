namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Utils;
	using UnityEngine;

	public interface IMap : IMapReadable, IMapWritable { }

	public interface IMapReadable
	{
		Vector2d CenterMercator { get; }
		float WorldRelativeScale { get; }
		Vector2d CenterLatitudeLongitude { get; }
		int Zoom { get; }
		Transform Root { get; }
		float UnityTileSize { get; }
		event Action OnInitialized;
	}

	public interface IMapWritable
	{
		void SetCenterMercator(Vector2d centerMercator);
		void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude);
		void SetZoom(int zoom);
	}
}