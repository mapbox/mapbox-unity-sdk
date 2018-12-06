using UnityEngine;
using UnityARInterface;
using UnityEngine.XR.ARFoundation;
using System;

public class PlaceMapOnARPlane : MonoBehaviour
{

	[SerializeField]
	private Transform _mapTransform;

	void Start()
	{
		ARPlaneHandler.returnARPlane += PlaceMap;
		ARPlaneHandler.resetARPlane += ResetPlane;
		Debug.Log("Place Map on AR");
	}

	void PlaceMap(ARPlane plane)
	{
		try
		{
			if (!_mapTransform.gameObject.activeSelf)
			{
				_mapTransform.gameObject.SetActive(true);
				Debug.Log("Map Active");
			}
			else
			{
				Debug.Log("ActiveSelf failed");
			}

			_mapTransform.position = plane.boundedPlane.Center;
			Debug.Log("Updating Map Position");
		}
		catch (Exception e)
		{
			Debug.Log("Exception ----> " + e.Message);
		}
	}

	void ResetPlane()
	{
		_mapTransform.gameObject.SetActive(false);
	}

	private void OnDisable()
	{
		ARPlaneHandler.returnARPlane -= PlaceMap;
	}
}
