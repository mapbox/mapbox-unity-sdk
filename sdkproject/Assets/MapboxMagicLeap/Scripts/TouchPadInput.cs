namespace Mapbox.Examples.MagicLeap
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.XR.MagicLeap;

	public class TouchPadInput : MonoBehaviour
	{

		//controller input
		private ControllerConnectionHandler _controllerConnectionHandler;

		[SerializeField, Tooltip("Higher sensitivity makes panning more responsive to input")]
		private float Sensitivity = 1.5f;

		[SerializeField, Tooltip("The gameobject to be controlled by touchpad input")]
		private Transform Map;

		[SerializeField, Tooltip("The controller's touchpad model.")]
		private GameObject _touchpad;

		[SerializeField]
		private Transform _touchIndicatorTransform;

		//the last touch position in world-space
		private Vector3 _lastTouchIndicatorPosition = Vector3.zero;

		void Start()
		{
			_controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();
			if (!_controllerConnectionHandler.enabled)
			{
				Debug.LogError("Error ControllerVisualizer starting MLInput, disabling script.");
				enabled = false;
				return;
			}
		}

		private void Update()
		{
			UpdateTouchpad();
		}

		private void UpdateTouchpad()
		{
			if (!_controllerConnectionHandler.IsControllerValid())
			{
				return;
			}

			MLInputController controller = _controllerConnectionHandler.ConnectedController;
			Vector3 updatePosition = new Vector3(controller.Touch1PosAndForce.x, 0.0f, controller.Touch1PosAndForce.y);
			float touchY = _touchIndicatorTransform.localPosition.y;
			_touchIndicatorTransform.localPosition = new Vector3(updatePosition.x * 0.1f, touchY, updatePosition.z * 0.1f);

			//update the touch indicator's transform based on the input value.
			//copied from magic leap sample code.
			if (controller.Touch1Active)
			{
				_touchIndicatorTransform.gameObject.SetActive(true);
				float angle = Mathf.Atan2(controller.Touch1PosAndForce.x, controller.Touch1PosAndForce.y);
				_touchIndicatorTransform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
			}
			else
			{
				_touchIndicatorTransform.gameObject.SetActive(false);
				_lastTouchIndicatorPosition = Vector3.zero;
				return;
			}

			//don't update the map if this is the first touch, store the input position and return.
			if (_lastTouchIndicatorPosition == Vector3.zero)
			{
				_lastTouchIndicatorPosition = _touchIndicatorTransform.position;
				return;
			}

			//pan the map based on the change in touch input.
			//using world-space allows input from both controller movement and touchpad movement.
			var mapDirection = _touchIndicatorTransform.position - _lastTouchIndicatorPosition;
			mapDirection.y = 0f;
			Map.transform.position += mapDirection * Sensitivity;

			_lastTouchIndicatorPosition = _touchIndicatorTransform.position;
		}
	}
}
