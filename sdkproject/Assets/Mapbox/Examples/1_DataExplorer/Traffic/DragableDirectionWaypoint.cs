using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragableDirectionWaypoint : MonoBehaviour
{
	private Vector3 screenPoint;
	private Vector3 offset;
	private Plane _yPlane;

	public void Start()
	{
		_yPlane = new Plane(Vector3.up, Vector3.zero);
	}

	void OnMouseDrag()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		float enter = 0.0f;
		if (_yPlane.Raycast(ray, out enter))
		{
			transform.position = ray.GetPoint(enter);
		}
	}
}
