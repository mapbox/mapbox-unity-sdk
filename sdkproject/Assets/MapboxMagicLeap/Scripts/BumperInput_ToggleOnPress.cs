namespace Mapbox.Examples.MagicLeap
{

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.XR.MagicLeap;

	public class BumperInput_ToggleOnPress : MonoBehaviour
	{
		[SerializeField]
		private GameObject _objectToToggle;
		private ControllerConnectionHandler _controllerConnectionHandler;

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

			MLInput.OnControllerButtonDown += HandleOnButtonDown;
		}


		/// <summary>
		/// Stop input api and unregister callbacks.
		/// </summary>
		void OnDestroy()
		{
			if (MLInput.IsStarted)
			{
				MLInput.OnControllerButtonDown -= HandleOnButtonDown;
			}
		}

		/// <summary>
		/// Handles the event for button down.
		/// </summary>
		/// <param name="controller_id">The id of the controller.</param>
		/// <param name="button">The button that is being pressed.</param>
		private void HandleOnButtonDown(byte controllerId, MLInputControllerButton button)
		{
			MLInputController controller = _controllerConnectionHandler.ConnectedController;
			if (controller != null && controller.Id == controllerId &&
				button == MLInputControllerButton.Bumper)
			{

				_objectToToggle.SetActive(!_objectToToggle.activeSelf);

			}
		}
	}
}