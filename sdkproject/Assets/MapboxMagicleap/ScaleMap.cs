using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;


public class ScaleMap : MonoBehaviour {

	public GameObject mapScale;

	public GameObject controlPoint;
	public GameObject interactionIndicator;
	public GameObject lPoseIndicator;

	private bool isClamped = true;
	private bool isInteracting = false;
	private bool isScaling = false;

	private ControllerConnectionHandler _controllerConnectionHandler;

	// Use this for initialization
	void Start () {

		_controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();

		if (!_controllerConnectionHandler.enabled)
		{
			Debug.LogError("Error ControllerVisualizer starting MLInput, disabling script.");
			enabled = false;
			return;
		}

		MLInput.OnControllerButtonUp += HandleOnButtonUp;
		MLInput.OnControllerButtonDown += HandleOnButtonDown;
		
	}

	private void Update()
	{
		float distance = Vector3.Distance(controlPoint.transform.position, lPoseIndicator.transform.position);

		//update differently while scaling
		if(isScaling)
		{
			interactionIndicator.SetActive(false);

			//scale the thing
			mapScale.transform.localScale = new Vector3(1f, 1f, 1f) + new Vector3( distance, distance, distance) * 5.0f;

			return;
		}

		//can't interact if there's no L Pose
		if (!lPoseIndicator.activeSelf)
		{
			StopScaling();
			return;
		}

		//check if the hand and controller are interacting
		if (distance < 0.1){
			interactionIndicator.SetActive(true);
			isInteracting = true;
		}
		else
		{
			interactionIndicator.SetActive(false);
			isInteracting = false;
		}
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
			// check if the map is clamped
			if( !isClamped)
			{
				return;
			}
			// check if the l pose is colliding with the control point
			if ( !isInteracting )
			{
				return;
			}
			// Start scaling map
			isScaling = true;

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
				if(isScaling)
				{
					StopScaling();
				}
				// stop scaling map if scaling
				// place the map if clamped
			}

		}
	}

	private void StopScaling()
	{
		isScaling = false;

		if( isInteracting )
		{
			mapScale.transform.localScale = new Vector3(1f, 1f, 1f);
			isClamped = true;
		}
	}
}
