namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using System.Collections.Generic;
	using System.Linq;
	using System;

	public class QuadTreeTileProvider : AbstractTileProvider
	{
		Plane _groundPlane;
		float _elapsedTime;
		bool _shouldUpdate;

		CameraBoundsTileProviderOptions _cbtpOptions;

		public override void OnInitialized()
		{
			_cbtpOptions = (CameraBoundsTileProviderOptions)_options;
			if (_cbtpOptions.camera == null)
			{
				_cbtpOptions.camera = Camera.main;
			}
			_groundPlane = new Plane(Vector3.up, 0);
			_shouldUpdate = true;
		}

		void Update()
		{
			//Camera Debugging
			//Vector3[] frustumCorners = new Vector3[4];
			//_cbtpOptions.camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _cbtpOptions.camera.transform.position.y, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

			//for (int i = 0; i < 4; i++)
			//{
			//	var worldSpaceCorner = _cbtpOptions.camera.transform.TransformVector(frustumCorners[i]);
			//	Debug.DrawRay(_cbtpOptions.camera.transform.position, worldSpaceCorner, Color.blue);
			//}

			if (!_shouldUpdate)
			{
				return;
			}

			_elapsedTime += Time.deltaTime;

			if (_elapsedTime >= _cbtpOptions.updateInterval)
			{
				_elapsedTime = 0f;

				//update viewport in case it was changed by switching zoom level
				Vector2dBounds _viewPortWebMercBounds = getcurrentViewPortWebMerc();

				var tilesToRequest = TileCover.GetWithWebMerc(_viewPortWebMercBounds, _map.AbsoluteZoom);

				var activeTiles = _activeTiles.Keys.ToList();
				List<UnwrappedTileId> toRemove = activeTiles.Except(tilesToRequest).ToList();
				foreach (var t2r in toRemove) { RemoveTile(t2r); }
				var finalTilesNeeded = tilesToRequest.Except(activeTiles);

				foreach (var tile in activeTiles)
				{
					// Reposition tiles in case we panned.
					RepositionTile(tile);
				}

				foreach (var tile in finalTilesNeeded)
				{
					AddTile(tile);
				}
			}
		}

		private Vector2dBounds getcurrentViewPortWebMerc(bool useGroundPlane = true)
		{
			Vector3[] hitPnt = new Vector3[4];

			if (useGroundPlane)
			{
				// rays from camera to groundplane: lower left and upper right
				Ray ray00 = _cbtpOptions.camera.ViewportPointToRay(new Vector3(0, 0));
				Ray ray01 = _cbtpOptions.camera.ViewportPointToRay(new Vector3(0, 1));
				Ray ray10 = _cbtpOptions.camera.ViewportPointToRay(new Vector3(1, 0));
				Ray ray11 = _cbtpOptions.camera.ViewportPointToRay(new Vector3(1, 1));
				hitPnt[0] = getGroundPlaneHitPoint(ray00);
				hitPnt[1] = getGroundPlaneHitPoint(ray01);
				hitPnt[2] = getGroundPlaneHitPoint(ray10);
				hitPnt[3] = getGroundPlaneHitPoint(ray11);
			}

			// Find min max bounding box. 
			// TODO : Find a better way of doing this. 
			float minX = float.MaxValue;
			float minZ = float.MaxValue;
			float maxX = float.MinValue;
			float maxZ = float.MinValue;
			for (int i = 0; i < 4; i++)
			{
				if (minX > hitPnt[i].x)
				{
					minX = hitPnt[i].x;
				}

				if (minZ > hitPnt[i].z)
				{
					minZ = hitPnt[i].z;
				}

				if (maxX < hitPnt[i].x)
				{
					maxX = hitPnt[i].x;
				}

				if (maxZ < hitPnt[i].z)
				{
					maxZ = hitPnt[i].z;
				}
			}

			Vector3 hitPntLL = new Vector3(minX, 0, minZ);
			Vector3 hitPntUR = new Vector3(maxX, 0, maxZ);

			//Debug.Log(hitPntLL + " - " + hitPntUR);

			var llLatLong = _map.WorldToGeoPosition(hitPntLL);
			var urLatLong = _map.WorldToGeoPosition(hitPntUR);

			Vector2dBounds tileBounds = new Vector2dBounds(Conversions.LatLonToMeters(llLatLong), Conversions.LatLonToMeters(urLatLong));

			// Bounds debugging. 
			Debug.DrawLine(_cbtpOptions.camera.transform.position, hitPntLL, Color.blue);
			Debug.DrawLine(_cbtpOptions.camera.transform.position, hitPntUR, Color.red);
			return tileBounds;
		}
		private Vector3 getGroundPlaneHitPoint(Ray ray)
		{
			float distance;
			if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
			return ray.GetPoint(distance);
		}
	}
}