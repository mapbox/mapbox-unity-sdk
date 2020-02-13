using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ARReferenceImage" , menuName = "UnityARKitPlugin/ARReferenceImage", order = 2)]
public class ARReferenceImage : ScriptableObject {

	public string imageName;
	public Texture2D imageTexture;
	public float physicalSize;

}

