namespace Mapbox.Examples
{
	using UnityEngine;

	public class CameraMovement : MonoBehaviour
	{
		[SerializeField]
		float Speed = 20;

		Vector3 _dragOrigin;
		Vector3 _cameraPosition;
		Vector3 _panOrigin;

		void Update()
		{
			if (Input.GetKey(KeyCode.A))
			{
				transform.Translate(-1 * Speed * Time.deltaTime, 0, 0, Space.World);
			}

			if (Input.GetKey(KeyCode.W))
			{
				transform.Translate(0, 0, 1 * Speed * Time.deltaTime, Space.World);
			}

			if (Input.GetKey(KeyCode.S))
			{
				transform.Translate(0, 0, -1 * Speed * Time.deltaTime, Space.World);
			}

			if (Input.GetKey(KeyCode.D))
			{
				transform.Translate(1 * Speed * Time.deltaTime, 0, 0, Space.World);
			}

			if (Input.GetMouseButtonDown(0))
			{
				_cameraPosition = transform.position;
				_panOrigin = Camera.main.ScreenToViewportPoint(Input.mousePosition);
			}

			if (Input.GetMouseButton(0))
			{
				LeftMouseDrag();
			}
		}

		// TODO: add acceleration!
		void LeftMouseDrag()
		{
			Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition) - _panOrigin;
			pos.z = pos.y;
			pos.y = 0;
			transform.position = _cameraPosition + -pos * Speed;
		}
	}
}