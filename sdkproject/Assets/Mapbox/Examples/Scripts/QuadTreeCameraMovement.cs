namespace Mapbox.Examples
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using UnityEngine;
	using System;

	public class QuadTreeCameraMovement : MonoBehaviour
	{
		[SerializeField]
		[HideInInspector]
		public float _zoomSpeed = 50f;

		[SerializeField]
		public Camera _referenceCamera;

		[SerializeField]
		QuadTreeTileProvider _quadTreeTileProvider;

		[SerializeField]
		AbstractMap _dynamicZoomMap;

		private Vector3 _origin;
		private Vector3 _mousePosition;
		private Vector3 _mousePositionPrevious;
		private bool _shouldDrag;

		void Start()
		{
			if (null == _referenceCamera)
			{
				_referenceCamera = GetComponent<Camera>();
				if (null == _referenceCamera) { Debug.LogErrorFormat("{0}: reference camera not set", this.GetType().Name); }
			}
		}


		private void LateUpdate()
		{
			if (null == _dynamicZoomMap) { return; }

			// zoom
			var scrollDelta = Input.GetAxis("Mouse ScrollWheel");

			if (scrollDelta > 0f)
			{
				_quadTreeTileProvider.UpdateMapProperties(_dynamicZoomMap.CenterLatitudeLongitude, Mathf.Min(_dynamicZoomMap.Zoom + 0.25f, 21.0f));
			}
			else if (scrollDelta < 0f)
			{
				_quadTreeTileProvider.UpdateMapProperties(_dynamicZoomMap.CenterLatitudeLongitude, Mathf.Max(_dynamicZoomMap.Zoom - 0.25f, 0.0f));
			}

			//pan keyboard
			float xMove = Input.GetAxis("Horizontal");
			float zMove = Input.GetAxis("Vertical");
			if (Math.Abs(xMove) > 0.0f || Math.Abs(zMove) > 0.0f)
			{
				float factor = (4.0f * (_dynamicZoomMap.AbsoluteZoom + 1)) / Mathf.Pow(2, _dynamicZoomMap.AbsoluteZoom + 1);

				_quadTreeTileProvider.UpdateMapProperties(new Vector2d(_dynamicZoomMap.CenterLatitudeLongitude.x + zMove * factor * 2.0f, _dynamicZoomMap.CenterLatitudeLongitude.y + xMove * factor * 4.0f), _dynamicZoomMap.Zoom);
			}

			//pan mouse
			if (Input.GetMouseButton(0))
			{
				var mousePosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mousePosScreen.z = _referenceCamera.transform.localPosition.y;
				_mousePosition = _referenceCamera.ScreenToWorldPoint(mousePosScreen);
				_mousePosition.y = 0f;
				if (_shouldDrag == false)
				{
					_shouldDrag = true;
					_origin = _referenceCamera.ScreenToWorldPoint(mousePosScreen);
				}
			}
			else
			{
				_shouldDrag = false;
			}

			if (_shouldDrag == true)
			{
				var changeFromPreviousPosition = _mousePositionPrevious - _mousePosition;
				if (Mathf.Abs(changeFromPreviousPosition.x) > 0.0f || Mathf.Abs(changeFromPreviousPosition.z) > 0.0f)
				{
					_mousePositionPrevious = _mousePosition;
					var offset = _origin - _mousePosition;
					if (Mathf.Abs(offset.x) > 0.0f || Mathf.Abs(offset.z) > 0.0f)
					{
						if (null != _dynamicZoomMap)
						{
							float factor = _dynamicZoomMap.WorldRelativeScale * (Conversions.GetTileScaleInMeters(0, _dynamicZoomMap.AbsoluteZoom) / ((_dynamicZoomMap.AbsoluteZoom + 1) * (_dynamicZoomMap.AbsoluteZoom + 1)));
							_quadTreeTileProvider.UpdateMapProperties(new Vector2d(_dynamicZoomMap.CenterLatitudeLongitude.x + offset.z * factor, _dynamicZoomMap.CenterLatitudeLongitude.y + offset.x * factor), _dynamicZoomMap.Zoom);
						}
					}
				}
			}
		}
	}
}