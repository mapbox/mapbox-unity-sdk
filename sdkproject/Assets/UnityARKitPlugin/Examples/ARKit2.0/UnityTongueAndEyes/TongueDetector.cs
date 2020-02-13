using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
using UnityEngine.UI;

public class TongueDetector : MonoBehaviour 
{
	public GameObject tongueImage;
	bool shapeEnabled = false;
	Dictionary<string, float> currentBlendShapes;

	// Use this for initialization
	void Start () 
	{
		UnityARSessionNativeInterface.ARFaceAnchorAddedEvent += FaceAdded;
		UnityARSessionNativeInterface.ARFaceAnchorUpdatedEvent += FaceUpdated;
		UnityARSessionNativeInterface.ARFaceAnchorRemovedEvent += FaceRemoved;

	}

	void OnGUI()
	{
		bool enableTongue = false;

		if (shapeEnabled) 
		{
			if (currentBlendShapes.ContainsKey (ARBlendShapeLocation.TongueOut)) 
			{
				enableTongue = (currentBlendShapes [ARBlendShapeLocation.TongueOut] > 0.5f);

			}

		}

		tongueImage.SetActive (enableTongue);
	}

	void FaceAdded (ARFaceAnchor anchorData)
	{
		shapeEnabled = true;
		currentBlendShapes = anchorData.blendShapes;
	}

	void FaceUpdated (ARFaceAnchor anchorData)
	{
		currentBlendShapes = anchorData.blendShapes;
	}

	void FaceRemoved (ARFaceAnchor anchorData)
	{
		shapeEnabled = false;
	}
	// Update is called once per frame
	void Update () {
		
	}
}
