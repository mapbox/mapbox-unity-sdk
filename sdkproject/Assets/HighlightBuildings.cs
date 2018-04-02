using System.Collections;
using System.Collections.Generic;
using KDTree;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration;
using Mapbox.Unity.MeshGeneration.Data;

public class HighlightBuildings : MonoBehaviour
{
	public KdTreeCollection Collection;
	private Vector3 _bump = new Vector3(0, 5, 0);
	public int MaxCount = 100;
	public float Range = 10;
	Ray ray;
	Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
	Vector3 pos;
	private NearestNeighbour<VectorEntity> pIter;

	void Update()
	{
		if (Input.GetMouseButton(0))
		{
			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			float rayDistance;
			if (groundPlane.Raycast(ray, out rayDistance))
			{
				pos = ray.GetPoint(rayDistance);
				pIter = Collection.Entities.NearestNeighbors(new double[] { pos.x, pos.z }, MaxCount, Range);
				while (pIter.MoveNext())
				{
					//pIter._Current.RangeAnimator.InRange(Color, (Range - pIter.CurrentDistance) / Range);
					pIter._Current.GameObject.transform.localScale = new Vector3(1, 0, 1);
				}
			}
		}
	}
}
