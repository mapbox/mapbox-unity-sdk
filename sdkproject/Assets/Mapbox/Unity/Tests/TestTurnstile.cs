using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity;
using Mapbox.Unity.Telemetry;
using UnityEngine;

public class TestTurnstile : MonoBehaviour
{
	[SerializeField]
	public string accessToken;

	[SerializeField]
	public bool _sendEvent = false;
	ITelemetryLibrary _telemetryLibrary;

	public void SendTurnstileEvent()
	{
		try
		{
			_telemetryLibrary = TelemetryFactory.GetTelemetryInstance();
			_telemetryLibrary.Initialize(accessToken);
			//_telemetryLibrary.SetLocationCollectionState(GetTelemetryCollectionState());
			_telemetryLibrary.SendTurnstile();
		}
		catch (Exception ex)
		{
			Debug.LogErrorFormat("Error initializing telemetry: {0}", ex);
		}
	}

	bool GetTelemetryCollectionState()
	{
		if (!PlayerPrefs.HasKey(Constants.Path.SHOULD_COLLECT_LOCATION_KEY))
		{
			PlayerPrefs.SetInt(Constants.Path.SHOULD_COLLECT_LOCATION_KEY, 1);
		}
		return PlayerPrefs.GetInt(Constants.Path.SHOULD_COLLECT_LOCATION_KEY) != 0;
	}
	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if (_sendEvent == true)
		{
			SendTurnstileEvent();
			_sendEvent = false;
		}
	}
}
