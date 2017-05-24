#if UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadAndroidAAR : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
		Debug.Log("=======================");
		Debug.Log("=======================");
		Debug.Log("=======================");
		Debug.Log("======================= BEFORE AndroidJavaClass");
		var MapboxAndroidTelem = new AndroidJavaClass("com.mapbox.services.android.telemetry.MapboxTelemetry");

	}

	// Update is called once per frame
	void Update()
	{

	}
}

#endif
