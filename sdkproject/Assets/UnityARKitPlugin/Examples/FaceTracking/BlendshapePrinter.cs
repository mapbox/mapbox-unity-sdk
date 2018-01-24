using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class BlendshapePrinter : MonoBehaviour {

	bool shapeEnabled = false;
	Dictionary<string, float> currentBlendShapes;

	// Use this for initialization
	void Start () {
		UnityARSessionNativeInterface.ARFaceAnchorAddedEvent += FaceAdded;
		UnityARSessionNativeInterface.ARFaceAnchorUpdatedEvent += FaceUpdated;
		UnityARSessionNativeInterface.ARFaceAnchorRemovedEvent += FaceRemoved;

	}

	void OnGUI()
	{
		if (shapeEnabled) {

			string blendshapes = "";
			string shapeNames = "";
			string valueNames = "";
			foreach(KeyValuePair<string,float> kvp in currentBlendShapes) {
				blendshapes += " [";
				blendshapes += kvp.Key.ToString ();
				blendshapes += ":";
				blendshapes += kvp.Value.ToString ();
				blendshapes += "]\n";
				shapeNames += "\"";
				shapeNames += kvp.Key.ToString ();
				shapeNames += "\",\n";
				valueNames += kvp.Value.ToString ();
				valueNames += "\n";
			}

			GUILayout.BeginHorizontal (GUILayout.ExpandHeight(true));
			GUILayout.Box (blendshapes);
			GUILayout.EndHorizontal ();

			Debug.Log (shapeNames);
			Debug.Log (valueNames);

		}
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
