namespace Mapbox.Examples
{
	// Just add this script to your camera. It doesn't need any configuration.

	using UnityEngine;
	using Mapbox.Unity.Location;

	public class TouchCamera : MonoBehaviour
	{
		[SerializeField]
		Camera _camera;

		[SerializeField]
		TransformLocationProvider _locationProvider;

		Vector2?[] oldTouchPositions = { null, null };

		Vector2 oldTouchVector;
		Vector3 _delta;
		float oldTouchDistance;
		Vector3 _origin;

		bool _wasTouching;

		bool _shouldDrag;

		void Update()
		{
			if (Input.touchCount == 0)
			{
				oldTouchPositions[0] = null;
				oldTouchPositions[1] = null;
				_shouldDrag = false;
				if (_wasTouching)
				{
					if (_locationProvider != null)
					{
						_locationProvider.SendLocationEvent();
					}
					_wasTouching = false;
				}
			}
			else if (Input.touchCount == 1)
			{
				_wasTouching = true;
				if (oldTouchPositions[0] == null || oldTouchPositions[1] != null)
				{
					oldTouchPositions[0] = Input.GetTouch(0).position;
					oldTouchPositions[1] = null;
				}
				else
				{
					Vector3 newTouchPosition = Input.GetTouch(0).position;
					newTouchPosition.z = _camera.transform.localPosition.y;
					_delta = _camera.ScreenToWorldPoint(newTouchPosition) - _camera.transform.localPosition;
					if (_shouldDrag == false)
					{
						_shouldDrag = true;
						_origin = _camera.ScreenToWorldPoint(newTouchPosition);
					}

					oldTouchPositions[0] = newTouchPosition;
					_camera.transform.localPosition = _origin - _delta;
				}
			}
			else
			{
				_wasTouching = true;
				if (oldTouchPositions[1] == null)
				{
					oldTouchPositions[0] = Input.GetTouch(0).position;
					oldTouchPositions[1] = Input.GetTouch(1).position;
					oldTouchVector = (Vector2)(oldTouchPositions[0] - oldTouchPositions[1]);
					oldTouchDistance = oldTouchVector.magnitude;
				}
				else
				{
					//Vector2 screen = new Vector2(_camera.pixelWidth, _camera.pixelHeight);

					Vector2[] newTouchPositions = { Input.GetTouch(0).position, Input.GetTouch(1).position };
					Vector2 newTouchVector = newTouchPositions[0] - newTouchPositions[1];
					float newTouchDistance = newTouchVector.magnitude;
					transform.localRotation *= Quaternion.Euler(new Vector3(0, 0, Mathf.Asin(Mathf.Clamp((oldTouchVector.y * newTouchVector.x - oldTouchVector.x * newTouchVector.y) / oldTouchDistance / newTouchDistance, -1f, 1f)) / 0.0174532924f));
					oldTouchPositions[0] = newTouchPositions[0];
					oldTouchPositions[1] = newTouchPositions[1];
					oldTouchVector = newTouchVector;
					oldTouchDistance = newTouchDistance;
				}
			}
		}
	}
}
