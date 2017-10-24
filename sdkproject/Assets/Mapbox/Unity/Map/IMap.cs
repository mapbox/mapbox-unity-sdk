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
		Vector2d CenterLatitudeLongitude { get;}
        Vector2d PanRange { get; }
        float ZoomRange { get; }
        int InitialZoom { get; }
		int Zoom { get; }
		Transform Root { get; }
		float UnityTileSize { get; }
		event Action OnInitialized;
        bool MapPanned { get;}
	}

	public interface IMapWritable
	{
		void SetCenterMercator(Vector2d centerMercator);
		void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude);
		void SetZoom(int zoom);
        void SetZoomRange(float zoom);
        void SetPanRange(Vector2d pan, bool reset = false);
	}
}