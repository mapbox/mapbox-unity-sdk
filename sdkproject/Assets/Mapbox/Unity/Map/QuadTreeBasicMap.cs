namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.Utilities;
	using Utils;
	using Mapbox.Map;
	using UnityEngine;

	public class QuadTreeBasicMap : BasicMap
	{
		public override UnityEngine.Vector3 GeoToWorldPosition(Vector2d latitudeLongitude)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for. 
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));

			var worldPos = Conversions.GeoToWorldPosition(latitudeLongitude, CenterMercator, WorldRelativeScale).ToVector3xz();
			return _root.TransformPoint(worldPos) * scaleFactor;
		}

		public override Vector2d WorldToGeoPosition(Vector3 realworldPoint)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for. 
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));

			return (_root.InverseTransformPoint(realworldPoint) / scaleFactor).GetGeoPosition(CenterMercator, WorldRelativeScale);
		}
	}
}
