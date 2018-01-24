using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class ARCameraTracker : MonoBehaviour {

	[SerializeField]
	private Camera trackedCamera;

	// Use this for initialization
	void Start () {
	}

	void OnDestroy()
	{
	}


	// Update is called once per frame
	void Update () {
		if (trackedCamera != null) {
			Matrix4x4 cameraPose = UnityARSessionNativeInterface.GetARSessionNativeInterface ().GetCameraPose ();
			trackedCamera.transform.localPosition = UnityARMatrixOps.GetPosition (cameraPose);
			trackedCamera.transform.localRotation = UnityARMatrixOps.GetRotation (cameraPose);

			trackedCamera.projectionMatrix = UnityARSessionNativeInterface.GetARSessionNativeInterface ().GetCameraProjection ();
		}
	}
}
