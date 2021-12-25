using System;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.Unity.QuadTree
{
	public class UnityRectD
	{
		public UnwrappedTileId Id;
		public RectD TileBounds;
		public Vector3 BottomLeft;
		public Vector3 BottomRight;
		public Vector3 TopLeft;
		public Vector3 TopRight;
		public Vector3 Center;
		public Bounds UnityBounds;
		public float DistanceToCamera;

		private Vector2d _offset;
		private float _worldScale;
		private float _currentElevationSample;
		private Vector3 UnityBoundsMin;
		private Vector3 UnityBoundsMax;

		// public bool OneCornerInView(Camera camera)
		// {
		// 	if(IsPointInView(camera, BottomLeft)) return true;
		// 	if(IsPointInView(camera, BottomRight)) return true;
		// 	if(IsPointInView(camera, TopLeft)) return true;
		// 	if(IsPointInView(camera, TopRight)) return true;
		// 	return false;
		// }
		//
		// private bool IsPointInView(Camera camera, Vector3 point)
		// {
		// 	var viewport = camera.WorldToViewportPoint(point);
		// 	return Vector3.Dot(camera.transform.forward, point) < 0 &&
		// 	       viewport.x > 0 &&
		// 	       viewport.x < 1 &&
		// 	       viewport.y > 0 &&
		// 	       viewport.y < 1;
		// }
		//
		// private void UnitySphereSpaceCalculations(RectD bounds, float radius)
		// {
		// 	TopLeft = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x, bounds.TopLeft.y)), radius);
		// 	TopRight = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x + bounds.Size.x, bounds.TopLeft.y)), radius);
		// 	BottomLeft = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x, bounds.TopLeft.y - bounds.Size.y)), radius);
		// 	BottomRight = LatLngToSphere(Conversions.MetersToLatLon(new Vector2d(bounds.TopLeft.x + bounds.Size.x, bounds.TopLeft.y - bounds.Size.y)), radius);
		// 	Center = (BottomLeft + TopRight) / 2;
		// 	//UnityBounds = new Bounds(new Vector3((float) Center.x, 0, (float) Center.y), new Vector3(Mathf.Abs((float) bounds.Size.x), 0, Mathf.Abs((float) bounds.Size.x)));
		// 	UnityBounds = GeometryUtility.CalculateBounds(new[]
		// 	{
		// 		TopLeft, TopRight, BottomLeft, BottomRight
		// 	}, Matrix4x4.identity);
		// }

		private void UnityFlatSpaceCalculations(RectD bounds, float worldScale, float boundHeight)
		{
			var boundsTopLeft = bounds.TopLeft;
			TopLeft = (boundsTopLeft / worldScale).ToVector3xz();
			var boundsSize = bounds.Size;

			TopRight = new Vector3((float)(boundsTopLeft.x + boundsSize.x) / worldScale, 0, (float)boundsTopLeft.y / worldScale);
			//TopRight = ((new Vector2d(boundsTopLeft.x + boundsSize.x, boundsTopLeft.y)) / worldScale).ToVector3xz();
			BottomLeft = new Vector3((float) boundsTopLeft.x / worldScale, 0, (float) (boundsTopLeft.y - boundsSize.y) / worldScale);
			//BottomLeft = ((new Vector2d(boundsTopLeft.x, boundsTopLeft.y - boundsSize.y)) / worldScale).ToVector3xz();
			BottomRight = new Vector3((float)(boundsTopLeft.x + boundsSize.x) / worldScale, 0, (float)(boundsTopLeft.y - boundsSize.y) / worldScale);
			//BottomRight = ((new Vector2d(boundsTopLeft.x + boundsSize.x, boundsTopLeft.y - boundsSize.y)) / worldScale).ToVector3xz();
			Center = (BottomLeft + TopRight) / 2;
			UnityBounds = new Bounds(Center, new Vector3((float)boundsSize.x / worldScale, boundHeight * 2, (float)boundsSize.y / worldScale));
			UnityBoundsMin = new Vector3(BottomLeft.x, -boundHeight, BottomLeft.z);
			UnityBoundsMax = new Vector3(TopRight.x, boundHeight, TopRight.z);
		}

		private Vector3 LatLngToSphere(Vector2d ll, float radius = 100)
		{
			var latitude = (float) (Mathf.Deg2Rad * ll.x);
			var longitude = (float) (Mathf.Deg2Rad * ll.y);

			float xPos = (radius) * Mathf.Cos(latitude) * Mathf.Cos(longitude);
			float zPos = (radius) * Mathf.Cos(latitude) * Mathf.Sin(longitude);
			float yPos = (radius) * Mathf.Sin(latitude);

			return new Vector3(xPos, yPos, zPos);
		}

		public UnityRectD(UnwrappedTileId id, Vector2d vector2d, float worldScale, float unityBoundHeight = 1)
		{
			_currentElevationSample = unityBoundHeight;
			_offset = vector2d;
			_worldScale = worldScale;
			Id = id;
			var bound = Conversions.TileBounds(Id);
			TileBounds = new RectD(new Vector2d(bound.TopLeft.x + vector2d.x, bound.TopLeft.y + vector2d.y), bound.Size);
			//UnitySphereSpaceCalculations(TileBounds, worldScale);
			UnityFlatSpaceCalculations(TileBounds, worldScale, _currentElevationSample);
		}

		public UnityRectD Quadrant(int i)
		{
			var childX  = (Id.X << 1) + (i % 2);
			var childY  = (Id.Y << 1) + (i >> 1);

			return new UnityRectD(new UnwrappedTileId(Id.Z + 1, childX, childY), _offset, _worldScale, _currentElevationSample);
		}

		public UnwrappedTileId QuadrantTileId(int i)
		{
			var childX  = (Id.X << 1) + (i % 2);
			var childY  = (Id.Y << 1) + (i >> 1);

			return new UnwrappedTileId(Id.Z + 1, childX, childY);
		}

		// public bool AnyCornerTowardsCamera(Camera camera, float comp)
		// {
		// 	return Vector3.Dot(Center.normalized, camera.transform.forward) < comp ||
		// 	       Vector3.Dot(TopLeft.normalized, camera.transform.forward) < comp ||
		// 	       Vector3.Dot(TopRight.normalized, camera.transform.forward) < comp ||
		// 	       Vector3.Dot(BottomLeft.normalized, camera.transform.forward) < comp ||
		// 	       Vector3.Dot(BottomRight.normalized, camera.transform.forward) < comp;
		// }
		//
		// public float GetDistanceToCamera(Camera camera)
		// {
		// 	var dist = float.MaxValue;
		// 	var corners = new[] {TopLeft, TopRight, BottomLeft, BottomRight, Center};
		// 	foreach (var corner in corners)
		// 	{
		// 		var cornerDistance = Vector3.SqrMagnitude(corner - camera.transform.position);
		// 		if (cornerDistance < dist)
		// 		{
		// 			dist = cornerDistance;
		// 		}
		// 	}
		//
		// 	return dist;
		// }

		public float ShortestDistanceTo(Vector3 p)
		{
			var dx = Math.Max(0, Math.Max(UnityBoundsMin.x - p.x, p.x - UnityBoundsMax.x));
			var dz = Math.Max(0, Math.Max(UnityBoundsMin.z - p.z, p.z - UnityBoundsMax.z));
			return (float) Math.Sqrt(dx*dx + dz*dz);
		}
	}
}