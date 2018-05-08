using System.Collections;
using System.Collections.Generic;
using KDTree;
using UnityEngine;

public class RemoveBuildingsScript : MonoBehaviour {

	LayerMask mask;
	void Start () {
		//Ray ray = new Ray(transform.position, -Vector3.up);
		//RaycastHit hitInfo;
		//Physics.Raycast(ray,out hitInfo,100,mask)
	}


	//raycast from ground plane to avoid using colliders
	private Vector3 getGroundPlaneHitPoint(Ray ray)
	{
		Plane _groundPlane = new Plane(Vector3.up, 0);
		float distance;
		if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
		return ray.GetPoint(distance);
	}

	private void OnTriggerEnter(Collider other)
	{
		if(other.tag=="POIPrefab")
		{
			Destroy(gameObject);
		}
	}
}
