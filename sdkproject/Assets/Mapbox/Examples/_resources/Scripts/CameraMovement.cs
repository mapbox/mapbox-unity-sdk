namespace Mapbox.Examples
{
	using System;
	using UnityEngine;

	public class CameraMovement : MonoBehaviour
	{
		[SerializeField]
		float _panSpeed = 20f;

		[SerializeField]
		float _zoomSpeed = 50f;

		[SerializeField]
		float _referenceScreenWidth = 1920;

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
				x = -Input.GetAxis("Mouse X");
				z = -Input.GetAxis("Mouse Y") * (_referenceScreenHeight / Screen.height);

				// Handle device touches and Unity Remote.
				if (Input.touchCount > 0)
				{
					x = -Input.GetTouch(0).deltaPosition.x / Screen.width;
					z = -Input.GetTouch(0).deltaPosition.y / Screen.height * (_referenceScreenHeight / Screen.height);
				}
			}
			else
			{
				x = Input.GetAxis("Horizontal");
				z = Input.GetAxis("Vertical");// * (_referenceScreenHeight / Screen.height);
				y = -Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
			}

			transform.localPosition += transform.forward * y + (_originalRotation * new Vector3(x * _panSpeed, 0, z * _panSpeed));
		}
	}
}