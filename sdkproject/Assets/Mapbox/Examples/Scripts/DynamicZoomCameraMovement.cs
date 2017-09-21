namespace Mapbox.Examples
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using UnityEngine;

	public class DynamicZoomCameraMovement : MonoBehaviour
	{
		[SerializeField]
		[HideInInspector]
		public float _zoomSpeed = 50f;

		[SerializeField]
		[HideInInspector]
		public Camera _referenceCamera;

		[SerializeField]
		DynamicZoomTileProvider _dynamicZoomTileProvider;

		[SerializeField]
		DynamicZoomMap _dynamicZoomMap;

		private Vector3 _origin;

		void Start()
		{
			if (null == _referenceCamera)
			{
				_referenceCamera = GetComponent<Camera>();
				if (null == _referenceCamera) { Debug.LogErrorFormat("{0}: reference camera not set", this.GetType().Name); }
			}

			//put camera into the middle of the allowed y movement range
			Vector3 localPosition = _referenceCamera.transform.position;
			localPosition.x = 0;
			localPosition.y = (_dynamicZoomTileProvider.CameraZoomingRangeMaxY + _dynamicZoomTileProvider.CameraZoomingRangeMinY) / 2;
			localPosition.z = 0;
			_referenceCamera.transform.localPosition = localPosition;
			_referenceCamera.transform.rotation = new Quaternion(0.7f, 0, 0, 0.7f);

			//link zoomspeed to tilesize
			_zoomSpeed = _dynamicZoomMap.UnityTileSize / 2f;
		}


		private void LateUpdate()
		{
			//if (null == Map) { Debug.LogErrorFormat("{0}: map not set", this.GetType().Name); }
			if (null == _dynamicZoomMap) { return; }


			//development short cut: reset center to 0/0 via right click
			if (Input.GetMouseButton(1))
			{
				//Map.CenterMercator.x = Map.CenterMercator.y = 0;
				_dynamicZoomMap.SetCenterMercator(Vector2d.zero);
				return;
			}


			// zoom
			var y = Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
			//avoid unnecessary translation
			if (0 != y)
			{
				_referenceCamera.transform.Translate(new Vector3(0, y, 0), Space.World);

				// TODO:
				//current approach doesn't work nicely when camera is tilted
				//maybe move camera so that center of viewport is always at 0/0
				//_referenceCamera.transform.Translate(new Vector3(0, y, 0), Space.Self);
			}

			//pan keyboard
			float xMove = Input.GetAxis("Horizontal");
			float zMove = Input.GetAxis("Vertical");
			if (0 != xMove || 0 != zMove)
			{
				float factor = Conversions.GetTileScaleInMeters((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.Zoom) * 256 / _dynamicZoomMap.UnityTileSize;
				xMove *= factor;
				zMove *= factor;
				Debug.LogFormat("xMove:{0} zMove:{1}", xMove, zMove);
				_dynamicZoomMap.SetCenterMercator(_dynamicZoomMap.CenterMercator + new Vector2d(xMove, zMove));
				//Map.CenterWebMerc.x += xMove;
				//Map.CenterWebMerc.y += zMove;
			}

			//pan mouse
			if (Input.GetMouseButtonDown(0))
			{
				var mouseDownPosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				mouseDownPosScreen.z = _referenceCamera.transform.localPosition.y;
				_origin = _referenceCamera.ScreenToWorldPoint(mouseDownPosScreen);
				Debug.LogFormat("button down, mousePosScreen:{0} mousePosWorld:{1}", mouseDownPosScreen, _origin);
			}

			if (Input.GetMouseButtonUp(0))
			{
				var mouseUpPosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mouseUpPosScreen.z = _referenceCamera.transform.localPosition.y;
				var mouseUpPosWorld = _referenceCamera.ScreenToWorldPoint(mouseUpPosScreen);
				Debug.LogFormat("button up, mousePosScreen:{0} mousePosWorld:{1}", mouseUpPosScreen, mouseUpPosWorld);

				//has position changed?
				if (_origin != mouseUpPosWorld)
				{
					var offset = _origin - mouseUpPosWorld;
					if (null != _dynamicZoomMap)
					{
						float factor = Conversions.GetTileScaleInMeters((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.Zoom) * 256 / _dynamicZoomMap.UnityTileSize;
						var centerOld = _dynamicZoomMap.CenterMercator;
						_dynamicZoomMap.SetCenterMercator(_dynamicZoomMap.CenterMercator + new Vector2d(offset.x * factor, offset.z * factor));
						//Map.CenterWebMerc.x += offset.x * factor;
						//Map.CenterWebMerc.y += offset.z * factor;

						Debug.LogFormat("old center:{0} new center:{1} offset:{2}", centerOld, _dynamicZoomMap.CenterMercator, offset);
					}
				}
			}
		}
	}
}