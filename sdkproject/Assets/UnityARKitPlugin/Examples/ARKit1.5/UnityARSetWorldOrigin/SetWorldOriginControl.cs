using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;

public class SetWorldOriginControl : MonoBehaviour {

	public Camera arCamera;
	public Text positionText;
	public Text rotationText;


	// Update is called once per frame
	void Update () {
		positionText.text = "Camera position=" + arCamera.transform.position.ToString ();
		rotationText.text = "Camera rotation=" + arCamera.transform.rotation.ToString ();
	}

	public void SetWorldOrigin()
	{
		UnityARSessionNativeInterface.GetARSessionNativeInterface().SetWorldOrigin (arCamera.transform);
	}
}
