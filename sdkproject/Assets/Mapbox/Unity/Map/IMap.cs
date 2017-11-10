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
		/// <summary>
		/// Gets the zoom value of the map.
		/// This allows for zoom values in between actual zoom level "AbsoluteZoom" requested from the service.
		/// </summary>
		float Zoom { get; }
		/// <summary>
		/// Gets the zoom value at which the map was intialized.
		/// </summary>
		int InitialZoom { get; }
		/// <summary>
		/// Gets the zoom value at which the tiles will be requested from the service.
		/// Use this only for calls which require an integer value of zoom to be passed in. 
		/// </summary>
		int AbsoluteZoom { get; }
		Transform Root { get; }
		float UnityTileSize { get; }
		event Action OnInitialized;
	}

	public interface IMapWritable
	{
		void SetCenterMercator(Vector2d centerMercator);
		void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude);
		void SetZoom(float zoom);
		void SetWorldRelativeScale(float scale);
	}
}