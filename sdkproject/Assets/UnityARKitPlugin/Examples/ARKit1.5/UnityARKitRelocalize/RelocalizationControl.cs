using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;

public class RelocalizationControl : MonoBehaviour {

	public Text buttonText;
	public Text trackingStateText;
	public Text trackingReasonText;

	// Use this for initialization
	void Start () {
		UnityARSessionNativeInterface.ARSessionShouldAttemptRelocalization = false;
		UpdateText ();

		UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += TrackingChanged;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void TrackingChanged(UnityARCamera cam)
	{
		trackingStateText.text = cam.trackingState.ToString ();
		trackingReasonText.text = cam.trackingReason.ToString ();
	}

	void OnDestroy()
	{
		UnityARSessionNativeInterface.ARSessionTrackingChangedEvent -= TrackingChanged;
	}

	void UpdateText()
	{
		buttonText.text = UnityARSessionNativeInterface.ARSessionShouldAttemptRelocalization ? "SHOULD RELOCALIZE" : "NO RELOCALIZE";
	}

	public void ToggleRelocalization()
	{
		UnityARSessionNativeInterface.ARSessionShouldAttemptRelocalization = !UnityARSessionNativeInterface.ARSessionShouldAttemptRelocalization;
		UpdateText ();
	}
}
