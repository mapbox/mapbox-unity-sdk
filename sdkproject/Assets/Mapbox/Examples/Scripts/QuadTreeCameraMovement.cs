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
		float _zoomSpeed = 0.25f;

		[SerializeField]
		public Camera _referenceCamera;

		//[SerializeField]
		//QuadTreeTileProvider _quadTreeTileProvider;

		[SerializeField]
		MapAPI _mapManager;

		[SerializeField]
		bool _useDegreeMethod;

		private Vector3 _origin;
		private Vector3 _mousePosition;
		private Vector3 _mousePositionPrevious;
		private bool _shouldDrag;
		private bool _isInitialized = false;

		void Awake()
		{
			if (null == _referenceCamera)
			{
				_referenceCamera = GetComponent<Camera>();
				if (null == _referenceCamera) { Debug.LogErrorFormat("{0}: reference camera not set", this.GetType().Name); }
			}
			_mapManager.OnInitialized += () =>
			{
				_isInitialized = true;
				Debug.Log("Camera Init");
			};
		}


		private void LateUpdate()
		{
			if (!_isInitialized) { return; }

			if (Input.touchSupported && Input.touchCount > 0)
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

			//PanMapUsingKeyBoard(xMove, zMove);


			//pan mouse
			//PanMapUsingTouchOrMouse();
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
			MapLocationOptions locationOptions = new MapLocationOptions();
			{
				locationOptions.latitudeLongitude = String.Format("{0},{1}", _mapManager.Map.CenterLatitudeLongitude.x, _mapManager.Map.CenterLatitudeLongitude.y);
				locationOptions.zoom = Mathf.Max(0.0f, Mathf.Min(_mapManager.Map.Zoom + zoomFactor * _zoomSpeed, 21.0f));
			}
			_mapManager.Map.UpdateMap(locationOptions);
			//.UpdateMapProperties(_mapManager.Map.CenterLatitudeLongitude, Mathf.Max(0.0f, Mathf.Min(_mapManager.Map.Zoom + zoomFactor * _zoomSpeed, 21.0f)));
		}

		void PanMapUsingKeyBoard(float xMove, float zMove)
		{
			if (Math.Abs(xMove) > 0.0f || Math.Abs(zMove) > 0.0f)
			{
				// Get the number of degrees in a tile at the current zoom level. 
				// Divide it by the tile width in pixels ( 256 in our case) 
				// to get degrees represented by each pixel.
				// Keyboard offset is in pixels, therefore multiply the factor with the offset to move the center.
				float factor = _panSpeed * (Conversions.GetTileScaleInDegrees((float)_mapManager.Map.CenterLatitudeLongitude.x, _mapManager.Map.AbsoluteZoom));
				MapLocationOptions locationOptions = new MapLocationOptions
				{
					latitudeLongitude = String.Format("{0},{1}", _mapManager.Map.CenterLatitudeLongitude.x + zMove * factor * 2.0f, _mapManager.Map.CenterLatitudeLongitude.y + xMove * factor * 4.0f),
					zoom = _mapManager.Map.Zoom
				};
				_mapManager.Map.UpdateMap(locationOptions);

				//_quadTreeTileProvider.UpdateMapProperties(new Vector2d(_mapManager.Map.CenterLatitudeLongitude.x + zMove * factor * 2.0f, _mapManager.Map.CenterLatitudeLongitude.y + xMove * factor * 4.0f), _mapManager.Map.Zoom);
			}
		}

		void PanMapUsingTouchOrMouse()
		{
			if (_useDegreeMethod)
			{
				UseDegreeConversion();
			}
			else
			{
				UseMeterConversion();
			}
		}

		void UseMeterConversion()
		{
			if (Input.GetMouseButton(0))
			{
				var mousePosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mousePosScreen.z = _referenceCamera.transform.localPosition.y;
				_mousePosition = _referenceCamera.ScreenToWorldPoint(mousePosScreen);

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
				if (Mathf.Abs(changeFromPreviousPosition.x) > 0.0f || Mathf.Abs(changeFromPreviousPosition.y) > 0.0f)
				{
					_mousePositionPrevious = _mousePosition;
					var offset = _origin - _mousePosition;

					if (Mathf.Abs(offset.x) > 0.0f || Mathf.Abs(offset.z) > 0.0f)
					{
						if (null != _mapManager.Map)
						{
							float factor = Conversions.GetTileScaleInMeters((float)0, _mapManager.Map.AbsoluteZoom) * 256.0f / _mapManager.Map.UnityTileSize;
							var latlongDelta = Conversions.MetersToLatLon(new Vector2d(offset.x * factor, offset.z * factor));
							//Debug.Log("LatLong Delta : " + latlongDelta);
							var newLatLong = _mapManager.Map.CenterLatitudeLongitude + latlongDelta;
							MapLocationOptions locationOptions = new MapLocationOptions
							{
								latitudeLongitude = String.Format("{0},{1}", newLatLong.x, newLatLong.y),
								zoom = _mapManager.Map.Zoom
							};
							//_mapManager.Map.UpdateMap(locationOptions);

							//_quadTreeTileProvider.UpdateMapProperties(_mapManager.Map.CenterLatitudeLongitude + latlongDelta, _mapManager.Map.Zoom);
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

		void UseDegreeConversion()
		{
			if (Input.GetMouseButton(0))
			{
				var mousePosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mousePosScreen.z = _referenceCamera.transform.localPosition.y;
				_mousePosition = _referenceCamera.ScreenToWorldPoint(mousePosScreen);

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
				if (Mathf.Abs(changeFromPreviousPosition.x) > 0.0f || Mathf.Abs(changeFromPreviousPosition.y) > 0.0f)
				{
					_mousePositionPrevious = _mousePosition;
					var offset = _origin - _mousePosition;

					if (Mathf.Abs(offset.x) > 0.0f || Mathf.Abs(offset.z) > 0.0f)
					{
						if (null != _mapManager.Map)
						{
							// Get the number of degrees in a tile at the current zoom level. 
							// Divide it by the tile width in pixels ( 256 in our case) 
							// to get degrees represented by each pixel.
							// Mouse offset is in pixels, therefore multiply the factor with the offset to move the center.
							float factor = _panSpeed * Conversions.GetTileScaleInDegrees((float)_mapManager.Map.CenterLatitudeLongitude.x, _mapManager.Map.AbsoluteZoom) * 256.0f / _mapManager.Map.UnityTileSize;
							MapLocationOptions locationOptions = new MapLocationOptions
							{
								latitudeLongitude = String.Format("{0},{1}", _mapManager.Map.CenterLatitudeLongitude.x + offset.z * factor, _mapManager.Map.CenterLatitudeLongitude.y + offset.x * factor),
								zoom = _mapManager.Map.Zoom
							};
							_mapManager.Map.UpdateMap(locationOptions);
							//_quadTreeTileProvider.UpdateMapProperties(new Vector2d(_dynamicZoomMap.CenterLatitudeLongitude.x + offset.z * factor, _dynamicZoomMap.CenterLatitudeLongitude.y + offset.x * factor), _dynamicZoomMap.Zoom);
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