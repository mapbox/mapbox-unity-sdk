using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ARReferenceImagesSet" , menuName = "UnityARKitPlugin/ARReferenceImagesSet", order = 3)]
public class ARReferenceImagesSet : ScriptableObject {

	public string resourceGroupName;
	public ARReferenceImage [] referenceImages;

}