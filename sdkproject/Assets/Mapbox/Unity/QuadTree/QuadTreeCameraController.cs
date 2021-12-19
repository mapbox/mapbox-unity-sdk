using Mapbox.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mapbox.Unity.QuadTree
{
	public class QuadTreeCameraController : MonoBehaviour
	{
		private Camera _camera;
		private QuadTreeMap _map;
		private float _worldScale;

		[Range(15, 90)]
		public float Pitch;
		[Range(-180, 180)]
		public float Bearing;

		public float CamDistanceMultiplier = 2;
		public float ZoomSpeed = 0.25f;
		public float RotationSpeed = 50.0f;
		public float CameraDistance;
		public AnimationCurve CameraCurve;

		public void Initialize(float worldScale, Camera cam, QuadTreeMap map)
		{
			_worldScale = worldScale;
			_camera = cam;
			_map = map;
			_camera.transform.position = new Vector3(0, 1, 0);
			_camera.transform.rotation = Quaternion.Euler(Pitch, Bearing, 0);
		}

		private Vector3 _lastMousePos;
		private float deltaAngleH;
		private float deltaAngleV;
		private bool _viewChanged;

		private float _prevCameraDistance = 0;

		public bool UpdateCamera()
		{
			if (EventSystem.current.IsPointerOverGameObject())
				return false;

			_viewChanged = false;
			if (Input.mouseScrollDelta.magnitude > 0)
			{
				var mouseWorld = GetPlaneIntersection(Input.mousePosition);
				var preMeters = WorldToMeterPosition(mouseWorld);
				var postZoom = Mathf.Max(0.0f, Mathf.Min(_map.Zoom + Input.GetAxis("Mouse ScrollWheel") * ZoomSpeed, 21.0f));


				var camDistanceAfterZoom = CalculateCameraDistance(postZoom);
				var mouseDistanceAfterZoomPan = (camDistanceAfterZoom * (preMeters - _map.CenterMercator)) / CameraDistance;
				var deltaZoomPan = (preMeters - _map.CenterMercator) - mouseDistanceAfterZoomPan;

				var final = _map.CenterMercator - deltaZoomPan;
				_map.SetCenterMercator(final);
				_map.SetCenterLatitudeLongitude(Conversions.MetersToLatLon(final));
				_map.SetZoom(postZoom);
				_viewChanged = true;
			}
			else if (Input.GetMouseButton(0))
			{
				var lastMouseWorld = GetPlaneIntersection(_lastMousePos);
				var mouseWorld = GetPlaneIntersection(Input.mousePosition);

				var positionMeterAfterPan = WorldToMeterPosition(mouseWorld - lastMouseWorld);
				_map.SetCenterMercator(positionMeterAfterPan);
				_map.SetCenterLatitudeLongitude(Conversions.MetersToLatLon(positionMeterAfterPan));
				_viewChanged = true;
			}

			if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.LeftShift))
			{
				var deltaMousePos = (Input.mousePosition - _lastMousePos);
				deltaAngleH = deltaMousePos.x;
				deltaAngleV = deltaMousePos.y;
				if (deltaAngleH != 0 || deltaAngleV != 0)
				{
					Pitch -= deltaAngleV * Time.deltaTime * RotationSpeed;
					Pitch = Mathf.Min(90, Mathf.Max(15, Pitch));
					Bearing += deltaAngleH * Time.deltaTime * RotationSpeed;
					_viewChanged = true;
				}
			}

			CameraDistance = CalculateCameraDistance(_map.Zoom);
			_camera.transform.rotation = Quaternion.Euler(Pitch, Bearing, 0);
			_camera.transform.position = Vector3.zero + _camera.transform.forward * (-1f * CameraDistance);

			_lastMousePos = Input.mousePosition;

			_prevCameraDistance = CameraDistance;
			return _viewChanged;
		}

		private Vector3 GetPlaneIntersection(Vector3 screenPosition)
		{
			var ray = _camera.ScreenPointToRay(screenPosition);
			var dirNorm = ray.direction / ray.direction.y;
			var intersectionPos = ray.origin - dirNorm * ray.origin.y;
			return intersectionPos;
		}

		private float CalculateCameraDistance(float zoom)
		{
			//return Mathf.Pow((20 - zoom), 2) * 100 / _worldScale;
			// var floorCamDistance = CamDistanceMultiplier * (float) Conversions.TileBounds(Conversions.LatitudeLongitudeToTileId(_map.CenterLatitudeLongitude.x, _map.CenterLatitudeLongitude.y, (int)zoom)).Size.x / _worldScale;
			// var nextCamDistance = CamDistanceMultiplier * (float) Conversions.TileBounds(Conversions.LatitudeLongitudeToTileId(_map.CenterLatitudeLongitude.x, _map.CenterLatitudeLongitude.y, (int)zoom - 1)).Size.x / _worldScale;
			// return Mathf.Lerp(nextCamDistance, floorCamDistance, zoom % 1);
			var distance = CameraCurve.Evaluate(zoom);
			return CamDistanceMultiplier * distance / _worldScale;
		}

		private Vector2d WorldToMeterPosition(Vector3 worldPos)
		{
			var scaleFactor = Mathf.Pow(2, (_map.InitialZoom - _map.AbsoluteZoom));
			return _map.CenterMercator - new Vector2d(worldPos.x, worldPos.z) / GetWorldScale() * (_worldScale / 100) / scaleFactor;
		}

		private float GetWorldScale()
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.AbsoluteZoom));
			return (float)(100 / referenceTileRect.Size.x);
		}
	}
}