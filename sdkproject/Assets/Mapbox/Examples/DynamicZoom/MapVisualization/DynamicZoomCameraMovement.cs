namespace Mapbox.Unity.Examples.DynamicZoom
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using System;
	using UnityEngine;

	public class DynamicZoomCameraMovement : MonoBehaviour
	{

		[SerializeField]
		public float _zoomSpeed = 50f;

		[SerializeField]
		public Camera _referenceCamera;

		[HideInInspector]
		public DynamicZoomMap Map;


		private Vector3 _origin;


		void Start()
		{
			if (null == _referenceCamera)
			{
				_referenceCamera = GetComponent<Camera>();
				if (null == _referenceCamera) { Debug.LogErrorFormat("{0}: reference camera not set", this.GetType().Name); }
			}


			transform.localPosition.Set(
				transform.localPosition.x
				, _referenceCamera.farClipPlane
				, transform.localPosition.z
			);
		}



		private void LateUpdate()
		{
			//if (null == Map) { Debug.LogErrorFormat("{0}: map not set", this.GetType().Name); }
			if (null == Map) { return; }


			//development short cut: reset center to 0/0 with right click
			if (Input.GetMouseButton(1))
			{
				Map.CenterWebMerc.x = Map.CenterWebMerc.y = 0;
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
				float factor = Conversions.GetTileScaleInMeters((float)Map.CenterLatitudeLongitude.x, Map.Zoom) * 256 / Map.UnityTileSize;
				xMove *= factor;
				zMove *= factor;
				Debug.LogFormat("xMove:{0} zMove:{1}", xMove, zMove);
				Map.CenterWebMerc.x += xMove;
				Map.CenterWebMerc.y += zMove;
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
					if (null != Map)
					{
						float factor = Conversions.GetTileScaleInMeters((float)Map.CenterLatitudeLongitude.x, Map.Zoom) * 256 / Map.UnityTileSize;
						var centerOld = Map.CenterWebMerc;
						Map.CenterWebMerc.x += offset.x * factor;
						Map.CenterWebMerc.y += offset.z * factor;

						Debug.LogFormat("old center:{0} new center:{1} offset:{2}", centerOld, Map.CenterWebMerc, offset);
					}
				}
			}
		}



	}
}