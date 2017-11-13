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
		[Range(1, 20)]
		public float _panSpeed = 1.0f;


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

			if (Input.touchSupported)
			{
				HandleTouch();
			}
			else
			{
				HandleMouseAndKeyBoard();
			}
		}

		void HandleMouseAndKeyBoard()
		{
			// zoom
			float scrollDelta = 0.0f;
			scrollDelta = Input.GetAxis("Mouse ScrollWheel");
			ZoomMapUsingTouchOrMouse(scrollDelta);

			//pan keyboard
			float xMove = Input.GetAxis("Horizontal");
			float zMove = Input.GetAxis("Vertical");

			PanMapUsingKeyBoard(xMove, zMove);


			//pan mouse
			PanMapUsingTouchOrMouse();
		}

		void HandleTouch()
		{
			float zoomFactor = 0.0f;
			//pinch to zoom. 
			switch (Input.touchCount)
			{
				case 1:
					{
						PanMapUsingTouchOrMouse();
					}
					break;
				case 2:
					{
						// Store both touches.
						Touch touchZero = Input.GetTouch(0);
						Touch touchOne = Input.GetTouch(1);

						// Find the position in the previous frame of each touch.
						Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
						Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

						// Find the magnitude of the vector (the distance) between the touches in each frame.
						float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
						float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

						// Find the difference in the distances between each frame.
						zoomFactor = 0.01f * (touchDeltaMag - prevTouchDeltaMag);
					}
					ZoomMapUsingTouchOrMouse(zoomFactor);
					break;
				default:
					break;
			}
		}

		void ZoomMapUsingTouchOrMouse(float zoomFactor)
		{
			_quadTreeTileProvider.UpdateMapProperties(_dynamicZoomMap.CenterLatitudeLongitude, Mathf.Max(0.0f, Mathf.Min(_dynamicZoomMap.Zoom + zoomFactor * 0.25f, 21.0f)));
		}

		void PanMapUsingKeyBoard(float xMove, float zMove)
		{
			if (Math.Abs(xMove) > 0.0f || Math.Abs(zMove) > 0.0f)
			{
				// Get the number of degrees in a tile at the current zoom level. 
				// Divide it by the tile width in pixels ( 256 in our case) 
				// to get degrees represented by each pixel.
				// Keyboard offset is in pixels, therefore multiply the factor with the offset to move the center.
				float factor = _panSpeed * (Conversions.GetTileScaleInDegrees((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.AbsoluteZoom) / 256.0f);
				_quadTreeTileProvider.UpdateMapProperties(new Vector2d(_dynamicZoomMap.CenterLatitudeLongitude.x + zMove * factor * 2.0f, _dynamicZoomMap.CenterLatitudeLongitude.y + xMove * factor * 4.0f), _dynamicZoomMap.Zoom);
			}
		}

		void PanMapUsingTouchOrMouse()
		{
			if (Input.GetMouseButton(0))
			{
				var mousePosScreen = Input.mousePosition;
				_mousePosition = mousePosScreen;
				if (_shouldDrag == false)
				{
					_shouldDrag = true;
					_origin = mousePosScreen;
				}
			}
			else
			{
				_shouldDrag = false;
			}

			if (_shouldDrag == true)
			{
				var changeFromPreviousPosition = _mousePositionPrevious - _mousePosition;
				if (Mathf.Abs(changeFromPreviousPosition.x) > 0.0f || Mathf.Abs(changeFromPreviousPosition.y) > 0.0f)
				{
					_mousePositionPrevious = _mousePosition;
					var offset = _origin - _mousePosition;
					if (Mathf.Abs(offset.x) > 0.0f || Mathf.Abs(offset.y) > 0.0f)
					{
						if (null != _dynamicZoomMap)
						{
							// Get the number of degrees in a tile at the current zoom level. 
							// Divide it by the tile width in pixels ( 256 in our case) 
							// to get degrees represented by each pixel.
							// Mouse offset is in pixels, therefore multiply the factor with the offset to move the center.
							float factorX = _panSpeed * (Conversions.GetTileScaleInDegrees((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.AbsoluteZoom) / (256.0f));
							float factorY = _panSpeed * (Conversions.GetTileScaleInDegrees((float)0, _dynamicZoomMap.AbsoluteZoom) / (256.0f));
							_quadTreeTileProvider.UpdateMapProperties(new Vector2d(_dynamicZoomMap.CenterLatitudeLongitude.x + offset.y * factorY, _dynamicZoomMap.CenterLatitudeLongitude.y + offset.x * factorX), _dynamicZoomMap.Zoom);
						}
					}
					_origin = _mousePosition;
				}
				else
				{
					_mousePositionPrevious = _mousePosition;
					_origin = _mousePosition;
				}
			}
		}
	}
}