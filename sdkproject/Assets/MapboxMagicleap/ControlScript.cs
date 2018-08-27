using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class ControlScript : MonoBehaviour
{
	private GameObject _cube;
	private MLInputController _controller;
	private const float _rotationSpeed = 30.0f;
	private const float _distance = 2.0f;
	private const float _moveSpeed = 1.2f;
	private bool _enabled = false;
	private bool _bumper = false;

	void Awake()
	{
		_cube = GameObject.Find("Cube");
		_cube.SetActive(false);
		MLInput.Start();
		MLInput.OnControllerButtonDown += OnButtonDown;
		MLInput.OnControllerButtonUp += OnButtonUp;
		_controller = MLInput.GetController(MLInput.Hand.Left);
	}

	void OnDestroy()
	{
		MLInput.OnControllerButtonDown -= OnButtonDown;
		MLInput.OnControllerButtonUp -= OnButtonUp;
		MLInput.Stop();
	}

	void Update()
	{
		if (_bumper && _enabled)
		{
			_cube.transform.Rotate(Vector3.up, +_rotationSpeed * Time.deltaTime);
		}
		CheckControl();
	}

	void CheckControl()
	{
		if (_controller.TriggerValue > 0.2f && _enabled)
		{
			_bumper = false;
			_cube.transform.Rotate(Vector3.up, -_rotationSpeed * Time.deltaTime);
		}
		else if (_controller.Touch1PosAndForce.z > 0.0f && _enabled)
		{
			float X = _controller.Touch1PosAndForce.x;
			float Y = _controller.Touch1PosAndForce.y;
			Vector3 forward = Vector3.Normalize(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
			Vector3 right = Vector3.Normalize(Vector3.ProjectOnPlane(transform.right, Vector3.up));
			Vector3 force = Vector3.Normalize((X * right) + (Y * forward));
			_cube.transform.position += force * Time.deltaTime * _moveSpeed;
		}
	}

	void OnButtonDown(byte controller_id, MLInputControllerButton button)
	{
		if ((button == MLInputControllerButton.Bumper && _enabled))
		{
			_bumper = true;
		}
	}

	void OnButtonUp(byte controller_id, MLInputControllerButton button)
	{
		if (button == MLInputControllerButton.HomeTap)
		{
			_cube.SetActive(true);
			_cube.transform.position = transform.position + transform.forward * _distance;
			_cube.transform.rotation = transform.rotation;
			_enabled = true;
		}
	}

}