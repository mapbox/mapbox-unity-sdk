using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;



public class VideoFormatsExample : MonoBehaviour {

	public Transform formatsParent;
	public GameObject videoFormatButtonPrefab;

	// Use this for initialization
	void Start () {
		VideoFormatButton.FormatButtonPressedEvent += ExampletButtonPressed;
		PopulateVideoFormatButtons ();
	}
	
	void OnDestroy () {
		VideoFormatButton.FormatButtonPressedEvent -= ExampletButtonPressed;
	}

	void PopulateVideoFormatButtons()
	{
		foreach (UnityARVideoFormat vf in UnityARVideoFormat.SupportedVideoFormats()) 
		{
			GameObject go = Instantiate<GameObject> (videoFormatButtonPrefab, formatsParent);
			VideoFormatButton vfb = go.GetComponent<VideoFormatButton> ();
			if (vfb != null) {
				vfb.Populate (vf);
			}
		}
	}

	public void ExampletButtonPressed(UnityARVideoFormat videoFormat)
	{
		//Restart session with new video format in config

		UnityARSessionNativeInterface session = UnityARSessionNativeInterface.GetARSessionNativeInterface();

		ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration();

		if (config.IsSupported) {
			config.planeDetection = UnityARPlaneDetection.HorizontalAndVertical;
			config.alignment = UnityARAlignment.UnityARAlignmentGravity;
			config.getPointCloudData = true;
			config.enableLightEstimation = true;
			config.enableAutoFocus = true;
			config.videoFormat = videoFormat.videoFormatPtr;
			Application.targetFrameRate = videoFormat.framesPerSecond;

			UnityARSessionRunOption runOption = UnityARSessionRunOption.ARSessionRunOptionRemoveExistingAnchors | UnityARSessionRunOption.ARSessionRunOptionResetTracking;
			session.RunWithConfigAndOptions (config, runOption);
		}
				
	}
}
