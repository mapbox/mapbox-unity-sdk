using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;

public class VideoFormatButton : MonoBehaviour {

	public Text videoFormatDescription;
	private UnityARVideoFormat arVideoFormat;

	public delegate void VideoFormatButtonPressed(UnityARVideoFormat videoFormat);
	public static event VideoFormatButtonPressed FormatButtonPressedEvent;

	public void Populate(UnityARVideoFormat videoFormat)
	{
		arVideoFormat = videoFormat;
		videoFormatDescription.text = "VideoFormat Resolution: " + videoFormat.imageResolutionWidth + "x" + videoFormat.imageResolutionHeight + " FPS: " + videoFormat.framesPerSecond;
	}

	public void ButtonPressed()
	{
		if (FormatButtonPressedEvent != null) {
			FormatButtonPressedEvent.Invoke (arVideoFormat);
		}
	}
}
