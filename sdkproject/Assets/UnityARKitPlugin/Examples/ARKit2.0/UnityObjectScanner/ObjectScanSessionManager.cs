using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class ObjectScanSessionManager : MonoBehaviour {

	public Camera m_camera;
	private UnityARSessionNativeInterface m_session;

	[Header("AR Config Options")]
	public UnityARAlignment startAlignment = UnityARAlignment.UnityARAlignmentGravity;
	public UnityARPlaneDetection planeDetection = UnityARPlaneDetection.Horizontal;
	public bool getPointCloudData = true;
	public bool enableLightEstimation = true;
	public bool enableAutoFocus = true;


	private bool sessionStarted = false;

	public ARKitWorldTrackingSessionConfiguration sessionConfiguration
	{
		get
		{
			ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();
			config.planeDetection = planeDetection;
			config.alignment = startAlignment;
			config.getPointCloudData = getPointCloudData;
			config.enableLightEstimation = enableLightEstimation;
			config.enableAutoFocus = enableAutoFocus;

			return config;
		}
	}

	//Warning: using this configuration is expensive CPU and battery-wise - use in limited amounts!
	public ARKitObjectScanningSessionConfiguration objScanSessionConfiguration
	{
		get
		{
			ARKitObjectScanningSessionConfiguration config = new ARKitObjectScanningSessionConfiguration ();
			config.planeDetection = planeDetection;
			config.alignment = startAlignment;
			config.getPointCloudData = getPointCloudData;
			config.enableLightEstimation = enableLightEstimation;
			config.enableAutoFocus = enableAutoFocus;

			return config;
		}
	}

	// Use this for initialization
	void Start () {
		m_session = UnityARSessionNativeInterface.GetARSessionNativeInterface();
		if (m_camera == null) {
			m_camera = Camera.main;
		}
		Application.targetFrameRate = 60;

		StartObjectScanningSession ();
	}


	public void StartObjectScanningSession()
	{
		sessionStarted = false;
		var config =  objScanSessionConfiguration;
		if (config.IsSupported) {
			m_session.RunWithConfig (config);
			UnityARSessionNativeInterface.ARFrameUpdatedEvent += FirstFrameUpdate;
		}
	}


	void FirstFrameUpdate(UnityARCamera cam)
	{
		sessionStarted = true;
		UnityARSessionNativeInterface.ARFrameUpdatedEvent -= FirstFrameUpdate;
	}

	// Update is called once per frame

	void Update () {

		if (m_camera != null && sessionStarted)
		{
			// JUST WORKS!
			Matrix4x4 matrix = m_session.GetCameraPose();
			m_camera.transform.localPosition = UnityARMatrixOps.GetPosition(matrix);
			m_camera.transform.localRotation = UnityARMatrixOps.GetRotation (matrix);

			m_camera.projectionMatrix = m_session.GetCameraProjection ();
		}

	}

}
