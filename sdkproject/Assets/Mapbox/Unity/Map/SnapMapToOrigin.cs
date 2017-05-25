using Mapbox.Unity.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapMapToOrigin : MonoBehaviour
{
	private AbstractMap _map;
	
	void Start()
	{
		var _map = GetComponent<AbstractMap>();
		if(_map == null)
		{
			Debug.Log("Snap Map to Origin script should be on the same object with AbstractMap");
			return;
		}

		_map.OnHeightChanged += (s) =>
		{
			_map.Root.transform.position = new Vector3(
				_map.Root.transform.position.x,
				-s.CenterHeight * _map.Root.transform.localScale.x,
				_map.Root.transform.position.z);
		};
	}
}
