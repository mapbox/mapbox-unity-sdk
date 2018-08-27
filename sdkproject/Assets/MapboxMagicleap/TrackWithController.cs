using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;


public class TrackWithController : MonoBehaviour {

	public Transform targetTransform;


	private Vector3 _initialTargetPosition;
	private Vector3 _initialSelfPosition;
	private bool _shouldMove = false;

	private ControllerConnectionHandler _controllerConnectionHandler;


	// Update is called once per frame
	void Update () {

		if(!_shouldMove)
		{
			return;
		}


		if( _initialTargetPosition == null
		   || _initialSelfPosition == null)
		{
			_initialSelfPosition = transform.position;
			_initialSelfPosition = targetTransform.position;
			return;
		}

		Vector3 offset = targetTransform.position - _initialTargetPosition;
		transform.position = _initialSelfPosition + offset;

	}


	// Use this for initialization
	void Start()
	{

		_controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();

		if (!_controllerConnectionHandler.enabled)
		{
			Debug.LogError("Error ControllerVisualizer starting MLInput, disabling script.");
			//enabled = false;
			return;
		}

		MLInput.OnControllerButtonUp += HandleOnButtonUp;
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
			MLInput.OnControllerButtonUp -= HandleOnButtonUp;
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
			// start tracking
			_initialSelfPosition = transform.position;
			_initialTargetPosition = targetTransform.position;
			_shouldMove = true;

			// update start position

		}
	}

	/// <summary>
	/// Handles the event for button up.
	/// </summary>
	/// <param name="controller_id">The id of the controller.</param>
	/// <param name="button">The button that is being released.</param>
	private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button)
	{
		MLInputController controller = _controllerConnectionHandler.ConnectedController;
		if (controller != null && controller.Id == controllerId)
		{
			if (button == MLInputControllerButton.Bumper)
			{
				//stop tracking
				_shouldMove = false;
				//apply offset
			}

		}
	}
}
