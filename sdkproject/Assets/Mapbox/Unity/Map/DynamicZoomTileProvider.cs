namespace Mapbox.Unity.Map
{
	using Mapbox.Map;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public class DynamicZoomTileProvider : AbstractTileProvider
	{
		[HideInInspector]
		public Camera _referenceCamera;

		/// <summary>previous Camera.Y</summary>
		private float _previousY = float.MinValue;
		/// <summary>previous Camera.Y</summary>
		private Vector2d _previousWebMercCenter;
		/// <summary>current viewport in WebMercator coordinates</summary>
		private Vector2dBounds _viewPortWebMercBounds;
		/// <summary>min of y range camera is allowed to move in</summary>
		private int _cameraZoomingRangeMinY;
		public int CameraZoomingRangeMinY { get { return _cameraZoomingRangeMinY; } }
		/// <summary>max of y range camera is allowed to move in</summary>
		private int _cameraZoomingRangeMaxY;

		public int CameraZoomingRangeMaxY { get { return _cameraZoomingRangeMaxY; } }

		private Plane _groundPlane;
		private DynamicZoomMap _dynamicZoomMap;
		private string _className;

		public override void OnInitialized()
		{
			_className = this.GetType().Name;

			//ground plane for raycasting
			_groundPlane = new Plane(Vector3.up, 0);
			_dynamicZoomMap = _map as DynamicZoomMap;
			if (null == _dynamicZoomMap) { Debug.LogErrorFormat("{0}: assigned map is not of type 'DynamicZoomMap'", _className); }


			_previousWebMercCenter = _dynamicZoomMap.CenterMercator;

			//set some defaults
			_dynamicZoomMap.MaxZoom = _dynamicZoomMap.MaxZoom == 0 ? 10 : _dynamicZoomMap.MaxZoom;
			_cameraZoomingRangeMaxY = (int)(_dynamicZoomMap.UnityTileSize * 2.5f);
			_cameraZoomingRangeMinY = (int)(_dynamicZoomMap.UnityTileSize * 1.25f);
		}

		void Update()
		{
			Vector2dBounds currentViewPortWebMercBnds = getcurrentViewPortWebMerc();
			bool bboxChanged = !(_viewPortWebMercBounds.ToString() == currentViewPortWebMercBnds.ToString());
			float cameraY = _referenceCamera.transform.localPosition.y;

			//no zoom, no pan -> don't change tiles
			if (cameraY == _previousY && !bboxChanged) { return; }
			_previousY = cameraY;

			//camera moves within one zoom level, and no panning, don't do anything
			if (
				(cameraY > _cameraZoomingRangeMinY && cameraY < _cameraZoomingRangeMaxY)
				&& !bboxChanged
			)
			{
				//no changes, bail
				return;
			}

			_viewPortWebMercBounds = currentViewPortWebMercBnds;

			//panning
			//TODO: move active tiles on pan
			//HACK: just deactivate all active tiles
			//!!!BEWARE!!!: don't compare Vector2d via '==' use 'Equals()'
			if (!_previousWebMercCenter.Equals(_dynamicZoomMap.CenterMercator))
			{
				_previousWebMercCenter = _dynamicZoomMap.CenterMercator;
				var remove = _activeTiles.Keys.ToList();
				foreach (var r in remove) 
				{ 
					RemoveTile(r); 
				}
			}

			Vector3 localPosition = _referenceCamera.transform.position;
			//close to ground, zoom in
			if (cameraY < _cameraZoomingRangeMinY)
			{
				//already at highest level, don't do anything -> camera free to move closer
				if (_dynamicZoomMap.Zoom == _dynamicZoomMap.MaxZoom) { return; }
				_dynamicZoomMap.SetZoom(_dynamicZoomMap.Zoom + 1);
				//reposition camera at max distance
				localPosition.y = _cameraZoomingRangeMaxY;
				_referenceCamera.transform.localPosition = localPosition;
			}
			//arrived at max distance, zoom out
			else if (cameraY > _cameraZoomingRangeMaxY)
			{
				//already at lowest level, don't do anything -> camera free to move further away
				if (_dynamicZoomMap.Zoom == _dynamicZoomMap.MinZoom) { return; }
				_dynamicZoomMap.SetZoom(_dynamicZoomMap.Zoom - 1);
				//reposition camera at min distance
				localPosition.y = _cameraZoomingRangeMinY;
				_referenceCamera.transform.localPosition = localPosition;
			}

			//update viewport in case it was changed by switching zoom level
			_viewPortWebMercBounds = getcurrentViewPortWebMerc();

			var tilesNeeded = TileCover.GetWithWebMerc(_viewPortWebMercBounds, _dynamicZoomMap.Zoom);

			var activeTiles = _activeTiles.Keys.ToList();
			List<UnwrappedTileId> toRemove = activeTiles.Except(tilesNeeded).ToList();
			foreach (var t2r in toRemove) { RemoveTile(t2r); }
			var finalTilesNeeded = tilesNeeded.Except(activeTiles);
			foreach (var tile in finalTilesNeeded)
			{
				AddTile(tile);
			}
		}


		private Vector2dBounds getcurrentViewPortWebMerc(bool useGroundPlane = true)
		{
			Vector3 hitPntLL;
			Vector3 hitPntUR;

			if (useGroundPlane)
			{
				// rays from camera to groundplane: lower left and upper right
				Ray rayLL = _referenceCamera.ViewportPointToRay(new Vector3(0, 0));
				Ray rayUR = _referenceCamera.ViewportPointToRay(new Vector3(1, 1));
				hitPntLL = getGroundPlaneHitPoint(rayLL);
				hitPntUR = getGroundPlaneHitPoint(rayUR);
			}
			else
			{
				hitPntLL = _referenceCamera.ViewportToWorldPoint(new Vector3(0, 0, _referenceCamera.transform.localPosition.y));
				hitPntUR = _referenceCamera.ViewportToWorldPoint(new Vector3(1, 1, _referenceCamera.transform.localPosition.y));
			}

			//get tile scale at equator, otherwise calucations don't work at higher latitudes
			double factor = Conversions.GetTileScaleInMeters(0, _dynamicZoomMap.Zoom) * 256 / _dynamicZoomMap.UnityTileSize;
			//convert Unity units to WebMercator and LatLng to get real world bounding box
			double llx = _dynamicZoomMap.CenterMercator.x + hitPntLL.x * factor;
			double lly = _dynamicZoomMap.CenterMercator.y + hitPntLL.z * factor;
			double urx = _dynamicZoomMap.CenterMercator.x + hitPntUR.x * factor;
			double ury = _dynamicZoomMap.CenterMercator.y + hitPntUR.z * factor;
			llx = llx > 0 ? Math.Min(llx, Mapbox.Utils.Constants.WebMercMax) : Math.Max(llx, -Mapbox.Utils.Constants.WebMercMax);
			lly = lly > 0 ? Math.Min(lly, Mapbox.Utils.Constants.WebMercMax) : Math.Max(lly, -Mapbox.Utils.Constants.WebMercMax);
			urx = urx > 0 ? Math.Min(urx, Mapbox.Utils.Constants.WebMercMax) : Math.Max(urx, -Mapbox.Utils.Constants.WebMercMax);
			ury = ury > 0 ? Math.Min(ury, Mapbox.Utils.Constants.WebMercMax) : Math.Max(ury, -Mapbox.Utils.Constants.WebMercMax);
			Vector2d llWebMerc = new Vector2d(llx, lly);
			Vector2d urWebMerc = new Vector2d(urx, ury);


			return new Vector2dBounds(
				llWebMerc
				, urWebMerc
			);
		}


		private Vector3 getGroundPlaneHitPoint(Ray ray)
		{
			float distance;
			if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
			return ray.GetPoint(distance);
		}
	}
}