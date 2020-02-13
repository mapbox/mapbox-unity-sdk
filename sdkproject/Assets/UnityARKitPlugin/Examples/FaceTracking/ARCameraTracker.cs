using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class ARCameraTracker : MonoBehaviour {

	[SerializeField]
	private Camera trackedCamera;

	private bool sessionStarted = false;

	// Use this for initialization
	void Start () {
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += FirstFrameUpdate;
	}

	void OnDestroy()
	{
	}

	void FirstFrameUpdate(UnityARCamera cam)
	{
		sessionStarted = true;
		UnityARSessionNativeInterface.ARFrameUpdatedEvent -= FirstFrameUpdate;
	}

	// Update is called once per frame
	void Update () {
		if (trackedCamera != null && sessionStarted) {
			Matrix4x4 cameraPose = UnityARSessionNativeInterface.GetARSessionNativeInterface ().GetCameraPose ();
			trackedCamera.transform.localPosition = UnityARMatrixOps.GetPosition (cameraPose);
			trackedCamera.transform.localRotation = UnityARMatrixOps.GetRotation (cameraPose);

			trackedCamera.projectionMatrix = UnityARSessionNativeInterface.GetARSessionNativeInterface ().GetCameraProjection ();
		}
	}
}
