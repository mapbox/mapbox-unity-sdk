using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class MatchTransform : MonoBehaviour {

	private ControllerConnectionHandler _controllerConnectionHandler;

	//map movement
	[SerializeField, Tooltip("The Game Object showing the touch model on the touchpad")]
	private Transform _touchIndicatorTransform;
	private Vector3 _lastTouchIndicatorPosition;

	[SerializeField, Tooltip("The controller's touchpad model.")]
	private GameObject _touchpad;
	public Transform Map;


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
	}



	// Update is called once per frame
	void Update () {

		UpdateTouchpadIndicator();
		
	}

	/// <summary>
	/// Update the touchpad's indicator: (location, directions, color).
	/// Also updates the color of the touchpad, based on pressure.
	/// </summary>
	private void UpdateTouchpadIndicator()
	{
		if (!_controllerConnectionHandler.IsControllerValid())
		{
			return;
		}
		MLInputController controller = _controllerConnectionHandler.ConnectedController;
		Vector3 updatePosition = new Vector3(controller.Touch1PosAndForce.x, 0.0f, controller.Touch1PosAndForce.y);
		float touchY = _touchIndicatorTransform.localPosition.y;
		_touchIndicatorTransform.localPosition = new Vector3(updatePosition.x * 0.1f, touchY, updatePosition.z * 0.1f);

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
			//_touchIndicatorTransform.localPosition = Vector3.zero;
		}

		if(_lastTouchIndicatorPosition == Vector3.zero)
		{
			_lastTouchIndicatorPosition = _touchIndicatorTransform.position;
			return;
		}

		var mapDirection = _touchIndicatorTransform.position - _lastTouchIndicatorPosition;
		mapDirection.y = 0f;
		Map.transform.position += mapDirection * 1.5f;

		_lastTouchIndicatorPosition = _touchIndicatorTransform.position;
	}
}
