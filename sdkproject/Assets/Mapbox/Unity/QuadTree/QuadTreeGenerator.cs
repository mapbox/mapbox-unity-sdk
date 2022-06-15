using System;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Platform.Cache;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.Unity.QuadTree
{
	public class QuadTreeGenerator : MonoBehaviour
	{
		public Camera ViewCamera;
		private QuadTreeMap _map;
		private float WorldScale;

		private Plane[] _planes;
		private Vector3[] _nearFrustumCorners;
		private Vector3[] _farFrustumCorners;
		private Vector3[] _frustumCorners;

		public int MinZoom;
		public int MaxZoom;

		private QuadTreeView _view;

		public void Initialize(float worldScale, Camera cam, QuadTreeMap map)
		{
			WorldScale = worldScale;
			ViewCamera = cam;
			_map = map;

			_view = new QuadTreeView();
			_nearFrustumCorners = new Vector3[4];
			_farFrustumCorners = new Vector3[4];
			_frustumCorners = new Vector3[8];
		}

		private float _pitch = 1.2566370f;
		private float _cameraToCenterDistance = 1566;
		private float tileSize = 512;

		public QuadTreeView UpdateQuadTree3(float elevationAtCenter)
		{
			_view = new QuadTreeView();
			_planes = GeometryUtility.CalculateFrustumPlanes(ViewCamera);

			var z = _map.AbsoluteZoom;
			var centerCoord = Conversions.ToThing(_map.CenterLatitudeLongitude);
			var numTiles = 1 << z;
			var centerPoint = new Vector2d(numTiles * centerCoord.x, numTiles * centerCoord.y);
			var meterToTile = numTiles * Conversions.MercatorZFromAltitude(1, _map.CenterMercator.x);
			var cameraCoord = centerCoord;
			var cameraAltitude = ViewCamera.transform.position.y / Conversions.MercatorZFromAltitude(1, _map.CenterMercator.x);
			var cameraPoint = new Vector3((float) (numTiles * cameraCoord.x), (float) (numTiles * cameraCoord.y), (float) cameraAltitude);
			var zoomSplitDistance = _cameraToCenterDistance / tileSize;

			var worldBaseBounds = new UnityRectD(new UnwrappedTileId(0, 0, 0), -_map.CenterMercator, WorldScale, elevationAtCenter);
			var stack = new Stack<UnityRectD>();
			stack.Push(worldBaseBounds);
			while (stack.Count > 0)
			{
				var tile = stack.Pop();

				if (!GeometryUtility.TestPlanesAABB(_planes, tile.UnityBounds))
				{
					continue;
				}

				if (tile.Id.Z == MaxZoom || !JsShouldSplit(tile))
				{
					_view.Tiles.Add(tile.Id, tile);

					if (tile.UnityBounds.Contains(Vector3.zero))
					{
						_view.CenterRect = tile;
					}
					continue;
				}

				for (var i = 0; i < 4; i++)
				{
					var child  = tile.Quadrant(i);
					stack.Push(child);
				}
			}

			return _view;
		}

		private bool JsShouldSplit(UnityRectD tile)
		{
			return false;
			// if (tile.Id.Z < MinZoom)
			// 	return true;
			// else if (tile.Id.Z >= MaxZoom)
			// 	return false;
			//
			// var dx = Mathf.Max(Mathf.Min(tile.UnityBoundsMax.x, camera))
		}

		//
		// private Vector2d CameraCenterPoint => new Vector2d(Screen.width / 2, Screen.height / 2);
		//
		// private Vector2d GetCameraPoint(Vector2d centerPoint)
		// {
		// 	var pitch = _pitch;
		// 	var yOffset = Math.Tan(pitch) * _cameraToCenterDistance;
		// 	return CameraCenterPoint + new Vector2d(0, yOffset);
		// }
		//
		// private Vector2d PointCoordinate(Vector2d p)
		// {
		// 	return
		// }



		public QuadTreeView UpdateQuadTree(float elevationAtCenter)
		{
			_view = new QuadTreeView();
			MaxZoom = _map.AbsoluteZoom;
			_planes = GeometryUtility.CalculateFrustumPlanes(ViewCamera);
			var worldBaseBounds = new UnityRectD(new UnwrappedTileId(0, 0, 0), -_map.CenterMercator, WorldScale, elevationAtCenter);
			var stack = new Stack<UnityRectD>();
			stack.Push(worldBaseBounds);
			while (stack.Count > 0)
			{
				var tile = stack.Pop();

				// var rot = _camera.transform.rotation.eulerAngles.y;
				// var result =
				// 	TestPlanesAABBInternalFast(
				// 		planes,
				// 		ref tile.UnityBounds,
				// 		(Quaternion.Euler(0,-rot, 0) * Vector3.right).normalized,
				// 		Vector3.up,
				// 		(Quaternion.Euler(0,-rot, 0) * Vector3.forward).normalized);
				//if (!GeometryUtility.TestPlanesAABB(planes, tile.UnityBounds))
				// _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, _nearFrustumCorners);
				// _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, _farFrustumCorners);

				// for (int i = 0; i < 4; i++)
				// {
				// 	_frustumCorners[i] = _camera.transform.TransformPoint(_nearFrustumCorners[i]);
				// 	_frustumCorners[i + 4] = _camera.transform.TransformPoint(_farFrustumCorners[i]);
				// }
				//
				// for (int i = 0; i < 7; i++)
				// {
				// 	Debug.DrawLine(_frustumCorners[i], _frustumCorners[i + 1], Color.red);
				// }

				//if(!TestPlanesAABBGLJS(_planes, _frustumCorners, ref tile.UnityBounds))
				if (!GeometryUtility.TestPlanesAABB(_planes, tile.UnityBounds))
				{
					continue;
				}

				if (tile.Id.Z == MaxZoom || !ShouldSplit(tile))
				{
					_view.Tiles.Add(tile.Id, tile);

					if (tile.UnityBounds.Contains(Vector3.zero))
					{
						_view.CenterRect = tile;
					}
					continue;
				}

				for (var i = 0; i < 4; i++)
				{
					var child  = tile.Quadrant(i);
					stack.Push(child);
				}
			}

			return _view;
		}

		private bool ShouldSplit(UnityRectD tile)
		{
			if (tile.Id.Z < MinZoom)
			{
				return true;
			}
			else if (tile.Id.Z == MaxZoom)
			{
				return false;
			}

			//var dist = tile.ShortestDistanceTo(Vector3.zero);
			var dist = tile.UnityBounds.SqrDistance(Vector3.zero);
			return dist < Mathf.Pow(tile.UnityBounds.size.x * .5f, 2);
		}


		public enum TestPlanesResults
        {
            /// <summary>
            /// The AABB is completely in the frustrum.
            /// </summary>
            Inside = 0,
            /// <summary>
            /// The AABB is partially in the frustrum.
            /// </summary>
            Intersect,
            /// <summary>
            /// The AABB is completely outside the frustrum.
            /// </summary>
            Outside
        }


		private float Dot(Plane plane, Vector4 vector)
		{
			return plane.normal.x * vector.x +
			       plane.normal.y * vector.y +
			       plane.normal.z * vector.z +
			       plane.distance * vector.w;
		}

        /// <summary>
        /// This is a faster AABB cull than brute force that also gives additional info on intersections.
        /// Calling Bounds.Min/Max is actually quite expensive so as an optimization you can precalculate these.
        /// http://www.lighthouse3d.com/tutorials/view-frustum-culling/geometric-approach-testing-boxes-ii/
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="boundsMin"></param>
        /// <param name="boundsMax"></param>
        /// <returns></returns>
        public bool TestPlanesAABBInternalFast (Plane [] planes, Vector3[] frustrumCorners, ref Bounds bounds)
        {
	        int result  = 0;
	        // check box outside/inside of frustum
	        for( int i=0; i<6; i++ )
	        {
		        result  = 0;
		        result  += ((Dot(planes[i], new Vector4(bounds.min.x, bounds.min.y, bounds.min.z, 1)) < 0) ? 1 : 0);
		        result  += ((Dot(planes[i], new Vector4(bounds.max.x, bounds.min.y, bounds.min.z, 1)) < 0) ? 1 : 0);
		        result  += ((Dot(planes[i], new Vector4(bounds.min.x, bounds.max.y, bounds.min.z, 1)) < 0) ? 1 : 0);
		        result  += ((Dot(planes[i], new Vector4(bounds.max.x, bounds.max.y, bounds.min.z, 1)) < 0) ? 1 : 0);
		        result  += ((Dot(planes[i], new Vector4(bounds.min.x, bounds.min.y, bounds.max.z, 1)) < 0) ? 1 : 0);
		        result  += ((Dot(planes[i], new Vector4(bounds.max.x, bounds.min.y, bounds.max.z, 1)) < 0) ? 1 : 0);
		        result  += ((Dot(planes[i], new Vector4(bounds.min.x, bounds.max.y, bounds.max.z, 1)) < 0) ? 1 : 0);
		        result  += ((Dot(planes[i], new Vector4(bounds.max.x, bounds.max.y, bounds.max.z, 1)) < 0) ? 1 : 0);
		        if (result == 8)
		        {
			        return false;
		        }
	        }

	        // check frustum outside/inside box
	        //int result ;
	        result =0;
	        for( int i=0; i<8; i++ ) result += ((frustrumCorners[i].x > bounds.max.x)?1:0);
	        if( result==8 )
		        return false;
	        result =0; for( int i=0; i<8; i++ ) result += ((frustrumCorners[i].x < bounds.min.x)?1:0);
	        if( result==8 )
		        return false;
	        result =0; for( int i=0; i<8; i++ ) result += ((frustrumCorners[i].y > bounds.max.y)?1:0);
	        if( result==8 )
		        return false;
	        result =0; for( int i=0; i<8; i++ ) result += ((frustrumCorners[i].y < bounds.min.y)?1:0);
	        if( result==8 )
		        return false;
	        result =0; for( int i=0; i<8; i++ ) result += ((frustrumCorners[i].z > bounds.max.z)?1:0);
	        if( result==8 )
		        return false;
	        result =0; for( int i=0; i<8; i++ ) result += ((frustrumCorners[i].z < bounds.min.z)?1:0);
	        if( result==8 )
		        return false;

	        return true;
        }

        public bool TestPlanesAABBGLJS (Plane [] planes, Vector3[] frustrumCorners, ref Bounds bounds)
        {
	        var fullyInside = true;

	        var aabbPoints = new Vector4[8]
	        {
		        new Vector4(bounds.min.x, bounds.min.y, bounds.min.z, 1),
		        new Vector4(bounds.max.x, bounds.min.y, bounds.min.z, 1),
		        new Vector4(bounds.min.x, bounds.max.y, bounds.min.z, 1),
		        new Vector4(bounds.max.x, bounds.max.y, bounds.min.z, 1),
		        new Vector4(bounds.min.x, bounds.min.y, bounds.max.z, 1),
		        new Vector4(bounds.max.x, bounds.min.y, bounds.max.z, 1),
		        new Vector4(bounds.min.x, bounds.max.y, bounds.max.z, 1),
		        new Vector4(bounds.max.x, bounds.max.y, bounds.max.z, 1)
	        };

	        for (int p = 0; p < planes.Length; p++)
	        {
		        var plane = planes[p];
		        var pointsInside = 0;

		        for (int i = 0; i < frustrumCorners.Length; i++)
		        {
			        pointsInside += (Dot(plane, aabbPoints[i]) >= 0) ? 1 : 0;
		        }

		        if (pointsInside == 0)
			        return false;

		        if (pointsInside != aabbPoints.Length)
			        fullyInside = false;
	        }

	        if (fullyInside)
		        return true;

	        for (int axis = 0; axis < 3; axis++)
	        {
		        var projMin = float.MaxValue;
		        var projMax = float.MinValue;

		        for (int i = 0; i < frustrumCorners.Length; i++)
		        {
			        var projectPoint = frustrumCorners[i][axis] - bounds.min[axis];

			        projMin = Math.Min(projMin, projectPoint);
			        projMax = Math.Max(projMax, projectPoint);
		        }

		        if (projMax < 0 || projMin > (bounds.max[axis] - bounds.min[axis]))
			        return false;
	        }

	        return true;
        }
	}
}