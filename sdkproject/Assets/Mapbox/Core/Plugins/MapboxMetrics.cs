using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Mapbox.Unity;

/*
  Manually set Xcode deployment target to 8.0 (needs to be 8+)
 */

public class MapboxMetrics : MonoBehaviour 
{
	#if UNITY_IOS && !UNITY_EDITOR

	[DllImport("__Internal")]
	private static extern void initialize(string accessToken, string userAgentBase);

	[DllImport("__Internal")]
	private static extern void sendTurnstyleEvent();

	#endif

	// Use this for initialization
	void Start () {
		Debug.Log("starting mapboxmetrics..");
		Debug.Log (MapboxAccess.Instance.AccessToken);
		#if UNITY_IOS && !UNITY_EDITOR
		Debug.Log("starting mapboxmetrics.. ios");
		// This gives the telem library enough information to operate. It must be called before any other methods
		// In a real app, the game developers public key should be used and we still need to figure out what the
		// actual value for "MapboxEventsUnityiOS" should be
		initialize( MapboxAccess.Instance.AccessToken , "MapboxEventsUnityiOS");

		// This tells the telem library to send a turnstile event. It can be called as often as you want but ideally only when 
		// there is a relevant event that our Unity SDK knows about (i.e. a map load or game session start)
		sendTurnstyleEvent();

		#endif
	}

}
