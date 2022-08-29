using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using AOT;


namespace UnityEngine.XR.iOS
{

	public struct UnityARVideoFormat  {
		public IntPtr videoFormatPtr;
		public float imageResolutionWidth;
		public float imageResolutionHeight;
		public int framesPerSecond;

		#if UNITY_EDITOR || !UNITY_IOS
		private static void EnumerateVideoFormats(VideoFormatEnumerator videoFormatEnumerator) 
		{
		}
		private static void EnumerateFaceTrackingVideoFormats(VideoFormatEnumerator videoFormatEnumerator) 
		{
		}
		#else
		[DllImport("__Internal")]
		private static extern void EnumerateVideoFormats(VideoFormatEnumerator videoFormatEnumerator);
		[DllImport("__Internal")]
		private static extern void EnumerateFaceTrackingVideoFormats(VideoFormatEnumerator videoFormatEnumerator);
		#endif

		static List<UnityARVideoFormat> videoFormatsList;

		public static List<UnityARVideoFormat> SupportedVideoFormats()
		{
			videoFormatsList = new List<UnityARVideoFormat> ();
			EnumerateVideoFormats (AddToVFList);

			return videoFormatsList;
		}

		public static List<UnityARVideoFormat> SupportedFaceTrackingVideoFormats()
		{
			videoFormatsList = new List<UnityARVideoFormat> ();
			EnumerateFaceTrackingVideoFormats(AddToVFList);

			return videoFormatsList;
		}

		[MonoPInvokeCallback(typeof(VideoFormatEnumerator))]
		private static void AddToVFList(UnityARVideoFormat newFormat)
		{
			Debug.Log ("New Format returned");
			videoFormatsList.Add (newFormat);
		}

	}

	public delegate void VideoFormatEnumerator(UnityARVideoFormat videoFormat);


}