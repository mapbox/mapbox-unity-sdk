using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;

public class SetCameraHeight : MonoBehaviour
{
	[SerializeField]
	AbstractMap _map;

	[SerializeField]
	Camera _referenceCamera;
	[SerializeField]
	float _cameraOffset = 100f;
	// Use this for initialization
	void Start()
	{
		if (_map == null)
		{
			_map = FindObjectOfType<AbstractMap>();
		}
		if (_referenceCamera == null)
		{
			_referenceCamera = FindObjectOfType<Camera>();
		}

	}

	// Update is called once per frame
	void Update()
	{
		var position = _referenceCamera.transform.position;
		position.y = _map.QueryElevationInMetersAt(_map.CenterLatitudeLongitude) + _cameraOffset;
		_referenceCamera.transform.position = position;

	}
}
