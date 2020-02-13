using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.Examples
{
	public class DragableDirectionWaypoint : MonoBehaviour
	{
		public Action MouseDown = () => { };
		public Action MouseDraging = () => { };
		public Action MouseDrop = () => { };

		public Transform MoveTarget;
		private Vector3 screenPoint;
		private Vector3 offset;
		private Plane _yPlane;

		public void Start()
		{
			_yPlane = new Plane(Vector3.up, Vector3.zero);
		}

		void OnMouseDrag()
		{
			MouseDraging();
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			float enter = 0.0f;
			if (_yPlane.Raycast(ray, out enter))
			{
				MoveTarget.position = ray.GetPoint(enter);
			}
		}

		private void OnMouseDown()
		{
			MouseDown();
		}

		private void OnMouseUp()
		{
			MouseDrop();
		}
	}
}
