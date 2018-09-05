namespace Mapbox.Examples.MagicLeap
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.XR.MagicLeap;


	public class TriggerInput_MoveOnPress : MonoBehaviour
	{

		public Transform targetTransform;
		public Transform Map;


		private Vector3 _initialTargetPosition;
		private Vector3 _initialMapPosition;
		private bool _shouldMove = false;

		private ControllerConnectionHandler _controllerConnectionHandler;


		// Update is called once per frame
		void Update()
		{

			UpdateTrigger();

			if (_shouldMove == false)
			{
				return;
			}

			Vector3 offset = targetTransform.position - _initialTargetPosition;
			Map.transform.position = _initialMapPosition + offset;

		}


		// Use this for initialization
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

		//trigger value from 0-1;
		private void UpdateTrigger()
		{
			if (!_controllerConnectionHandler.IsControllerValid())
			{
				return;
			}
			MLInputController controller = _controllerConnectionHandler.ConnectedController;


			//trigger is up
			if (controller.TriggerValue < 0.1
				&& _shouldMove)
			{
				_shouldMove = false;

			}

			// trigger is down
			if (controller.TriggerValue > 0.5
				&& _shouldMove == false)
			{
				_shouldMove = true;
				_initialMapPosition = Map.transform.position;
				_initialTargetPosition = targetTransform.position;
			}
		}
	}
}
