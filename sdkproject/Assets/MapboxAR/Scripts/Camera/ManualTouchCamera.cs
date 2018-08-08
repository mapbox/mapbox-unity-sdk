namespace Mapbox.Examples
{
	// Just add this script to your camera. It doesn't need any configuration.

	using UnityEngine;
	using Mapbox.Unity.Location;

	public class ManualTouchCamera : MonoBehaviour
	{
		[SerializeField]
		Camera _camera;

		[SerializeField]
		Transform _mapRoot;

		[SerializeField]
		Transform _mapHolder;

		Vector2?[] _oldTouchPositions = { null, null };

		Vector2 _oldTouchVector;
		Vector3 _delta;
		float _oldTouchDistance;

		bool _wasTouching;

		void Update()
		{

			if (Input.touchCount == 0)
			{
				_oldTouchPositions[0] = null;
				_oldTouchPositions[1] = null;

			}
			else if (Input.touchCount == 1)
			{
				if (_oldTouchPositions[0] == null || _oldTouchPositions[1] != null)
				{
					_oldTouchPositions[0] = Input.GetTouch(0).position;
					_oldTouchPositions[1] = null;
				}

				if (Input.GetTouch(0).phase == TouchPhase.Moved)
				{
					var touchDelta = Input.GetTouch(0).deltaPosition;
					var offset = new Vector3(touchDelta.x, 0f, touchDelta.y);
					offset = _camera.transform.rotation * offset;
					var newPos = new Vector3(offset.x, 0, offset.y);
					_mapRoot.position = newPos + _mapRoot.position;
				}
			}
			else
			{
				if (_oldTouchPositions[1] == null)
				{
					_oldTouchPositions[0] = Input.GetTouch(0).position;
					_oldTouchPositions[1] = Input.GetTouch(1).position;
					_oldTouchVector = (Vector2)(_oldTouchPositions[0] - _oldTouchPositions[1]);
					_oldTouchDistance = _oldTouchVector.magnitude;
				}
				else
				{
					//Vector2 screen = new Vector2(_camera.pixelWidth, _camera.pixelHeight);
					Vector2[] newTouchPositions = { Input.GetTouch(0).position, Input.GetTouch(1).position };
					Vector2 newTouchVector = newTouchPositions[0] - newTouchPositions[1];
					float newTouchDistance = newTouchVector.magnitude;
					_mapHolder.rotation *= Quaternion.Euler(new Vector3(0, Mathf.Asin(Mathf.Clamp((_oldTouchVector.y * newTouchVector.x - _oldTouchVector.x * newTouchVector.y) / _oldTouchDistance / newTouchDistance, -1f, 1f)) / 0.0174532924f, 0));
					_oldTouchPositions[0] = newTouchPositions[0];
					_oldTouchPositions[1] = newTouchPositions[1];
					_oldTouchVector = newTouchVector;
					_oldTouchDistance = newTouchDistance;
				}
			}
		}
	}
}
