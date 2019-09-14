using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

[CreateAssetMenu(fileName = "ARReferenceObjectAsset" , menuName = "UnityARKitPlugin/ARReferenceObjectAsset", order = 4)]
public class ARReferenceObjectAsset : ScriptableObject {
	public string objectName;
	public Object referenceObject;

}
