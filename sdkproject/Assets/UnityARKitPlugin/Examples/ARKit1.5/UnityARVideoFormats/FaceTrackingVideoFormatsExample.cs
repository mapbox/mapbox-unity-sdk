using UnityEngine;
using UnityEngine.XR.iOS;



public class FaceTrackingVideoFormatsExample : MonoBehaviour {

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
		foreach (UnityARVideoFormat vf in UnityARVideoFormat.SupportedFaceTrackingVideoFormats()) 
		{
			GameObject go = Instantiate(videoFormatButtonPrefab, formatsParent);
			VideoFormatButton vfb = go.GetComponent<VideoFormatButton> ();
			if (vfb != null) {
				vfb.Populate (vf);
			}
		}
	}

	public void ExampletButtonPressed(UnityARVideoFormat videoFormat)
	{
		UnityARSessionNativeInterface session = UnityARSessionNativeInterface.GetARSessionNativeInterface();

		var config = new ARKitFaceTrackingConfiguration();

		if (config.IsSupported) {
			config.alignment = UnityARAlignment.UnityARAlignmentGravity;
			config.enableLightEstimation = true;
			config.videoFormat = videoFormat.videoFormatPtr;
			Application.targetFrameRate = videoFormat.framesPerSecond;

			UnityARSessionRunOption runOption = UnityARSessionRunOption.ARSessionRunOptionRemoveExistingAnchors | UnityARSessionRunOption.ARSessionRunOptionResetTracking;
			session.RunWithConfigAndOptions (config, runOption);
		}
				
	}
}
