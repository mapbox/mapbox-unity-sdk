namespace Mapbox.Unity.Ar
{

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityARInterface;

	public class PlaceOnARPlane : MonoBehaviour
	{

		void Start()
		{
			ARInterface.planeAdded += PlaceOnPlane;
			ARInterface.planeUpdated += PlaceOnPlane;
		}

		void PlaceOnPlane(BoundedPlane plane)
		{
			var pos = transform.position;
			transform.position = new Vector3(pos.x, plane.center.y, pos.z);
		}

	}
}
