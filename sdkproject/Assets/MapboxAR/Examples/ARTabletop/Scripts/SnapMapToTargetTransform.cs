using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;

public class SnapMapToTargetTransform : MonoBehaviour
{
	[SerializeField]
	private AbstractMap _map;
	[SerializeField]
	private Transform _target;

	private void Awake()
	{
		if (_map == null)
		{
			_map = FindObjectOfType<AbstractMap>();
		}
	}

	void Start()
	{
		_map.OnUpdated += SnapMapToTarget;
	}

	void SnapMapToTarget()
	{
		var h = _map.QueryElevationInUnityUnitsAt(_map.CenterLatitudeLongitude);
		_map.Root.transform.position = new Vector3(
			 _map.Root.transform.position.x,
			  _target.transform.position.y - h,
			 _map.Root.transform.position.z);
	}
}
