namespace Mapbox.Examples
{
	using UnityEngine;

	public class CameraMovement : MonoBehaviour
	{
		[SerializeField]
		float _panSpeed = 20f;

		[SerializeField]
		float _zoomSpeed = 50f;

		[SerializeField]
		float _referenceScreenHeight = 1080f;

		Quaternion _originalRotation;

		void Awake()
		{
			_originalRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
		}

		void Update()
		{
			var x = 0f;
			var y = 0f;
			var z = 0f;

			if (Input.GetMouseButton(0))
			{
				x = _panSpeed * -Input.GetAxis("Mouse X");
				z = _panSpeed * -Input.GetAxis("Mouse Y") * (_referenceScreenHeight / Screen.height);
			}
			else
			{
				x = _panSpeed * Input.GetAxis("Horizontal");
				z = _panSpeed * Input.GetAxis("Vertical") * (_referenceScreenHeight / Screen.height);
				y = -Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
			}

			transform.localPosition += transform.forward * y + (_originalRotation * new Vector3(x, 0, z));
		}
	}
}