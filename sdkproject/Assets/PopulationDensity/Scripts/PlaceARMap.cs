using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using UnityEngine;

public class PlaceARMap : MonoBehaviour {

	public AbstractMap ARMap;
	public Camera ARCamera;
	public float placeDistance;
	public float moveSpeed, rotateSpeed, scaleSpeed;
	public float minScale, maxScale;

	private Vector3 lastPos;
	private static bool IsPlaced;
	private Vector3 mapPlacementPosition;
	private Vector3 originalPos, originalScale;
	private Quaternion originalRot;
	private Vector3 mouseDelta;

	void OnEnable()
	{
		ARMap.GetComponent<AbstractMap>().OnInitialized += Handle_OnInitialized;
	}

	void Handle_OnInitialized()
	{
		ARMap.transform.position = new Vector3(mapPlacementPosition.x, mapPlacementPosition.y + ARMap.transform.position.y, mapPlacementPosition.z); ;
	}

	public void PlaceMap()
	{
		if (IsPlaced)
			return;

		IsPlaced = true;
		var downDistance = 1f;
		mapPlacementPosition = ARCamera.transform.position + ARCamera.transform.forward * placeDistance + Vector3.down.normalized * downDistance;
		ARMap.transform.position = new Vector3(mapPlacementPosition.x, mapPlacementPosition.y + ARMap.transform.position.y, mapPlacementPosition.z);
	}

	void Update()
	{
		if (!IsPlaced && Input.GetMouseButton(0))
		{
			PlaceMap();
		}

		if (Input.touches.Length > 0)
		{
			DetectTouchMovement.Calculate();

			mouseDelta = Input.GetTouch(0).deltaPosition;
			var newPosDelta = ARMap.transform.position + mouseDelta * Time.deltaTime * moveSpeed;
			ARMap.transform.position = new Vector3(ARMap.transform.position.x, newPosDelta.y, ARMap.transform.position.z);
			mapPlacementPosition = ARMap.transform.position;

			//Rotate Script
			if (Mathf.Abs(mouseDelta.x) > 0)
			{
				ARMap.transform.Rotate(ARMap.transform.up, -mouseDelta.x * Time.deltaTime * rotateSpeed); ;
			}

			//Scale Script
			if (Mathf.Abs(DetectTouchMovement.pinchDistanceDelta) > 0)
			{
				var scaleVec = Vector3.one * DetectTouchMovement.pinchDistanceDelta * Time.deltaTime * scaleSpeed;

				var newScale = ARMap.transform.localScale + scaleVec;
				if (newScale.x > maxScale)
				{
					if (scaleVec.x > 0)
						return;
				}
				if (newScale.x < minScale)
				{
					if (scaleVec.x < 0)
						return;
				}

				ARMap.transform.localScale = newScale;
			}
		}
		else
		{
			if (Input.GetMouseButtonDown(0))
			{
				lastPos = Input.mousePosition;
			}
			else if (Input.GetMouseButton(0))
			{
				mouseDelta = Input.mousePosition - lastPos;

				// Do Stuff here
				var newPosDelta = ARMap.transform.position + mouseDelta * Time.deltaTime * moveSpeed;
				ARMap.transform.position = new Vector3(ARMap.transform.position.x, newPosDelta.y, ARMap.transform.position.z);
				mapPlacementPosition = ARMap.transform.position;

				//Rotate Script
				if (Mathf.Abs(mouseDelta.x) > 0)
				{
					ARMap.transform.Rotate(ARMap.transform.up, mouseDelta.x * Time.deltaTime * rotateSpeed); ;
				}
				// End do stuff

				lastPos = Input.mousePosition;
			}
		}
	}
}
