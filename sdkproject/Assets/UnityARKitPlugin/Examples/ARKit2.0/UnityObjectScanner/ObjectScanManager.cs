using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
using System;
using UnityEngine.UI;
using System.IO;

public class ObjectScanManager : MonoBehaviour {

	[SerializeField]
	ObjectScanSessionManager m_ARSessionManager;

	[SerializeField]
	Text listOfObjects;

	int objIndex = 0;
	List<ARReferenceObject> scannedObjects;
	bool detectionMode = false;

	private PickBoundingBox pickBoundingBox;

	void Start()
	{
		scannedObjects = new List<ARReferenceObject> ();
		pickBoundingBox = GetComponent<PickBoundingBox> ();
	}

	void OnDestroy()
	{
		ClearScannedObjects ();
	}

	static UnityARSessionNativeInterface session
	{
		get { return UnityARSessionNativeInterface.GetARSessionNativeInterface(); }
	}

	public void CreateReferenceObject()
	{
		//this script should be placed on the bounding volume GameObject
		CreateReferenceObject (pickBoundingBox.transform, pickBoundingBox.bounds.center-pickBoundingBox.transform.position, pickBoundingBox.bounds.size);
	}

	public void CreateReferenceObject(Transform objectTransform, Vector3 center, Vector3 extent)
	{
		session.ExtractReferenceObjectAsync (objectTransform, center, extent, (ARReferenceObject referenceObject) => {
			if (referenceObject != null) {
				Debug.LogFormat ("ARReferenceObject created: center {0} extent {1}", referenceObject.center, referenceObject.extent);
				referenceObject.name = "objScan_" + objIndex++;
				Debug.LogFormat ("ARReferenceObject has name {0}", referenceObject.name);
				scannedObjects.Add(referenceObject);
				UpdateList();
			} else {
				Debug.Log ("Failed to create ARReferenceObject.");
			}
		});
	}

	void UpdateList()
	{
		string members = "";
		foreach (ARReferenceObject arro in scannedObjects) {
			members += arro.name + ",";
		}
		listOfObjects.text = members;
	}

	public void DetectScannedObjects(Text toChange)
	{
		detectionMode = !detectionMode;
		if (detectionMode) {
			StartDetecting ();
			toChange.text = "Stop Detecting";
		} else {
			m_ARSessionManager.StartObjectScanningSession ();
			toChange.text = "Detect Objects";
		}
	}

	private void StartDetecting()
	{
		//create a set out of the scanned objects
		IntPtr ptrReferenceObjectsSet = session.CreateNativeReferenceObjectsSet(scannedObjects);

		//restart session without resetting tracking 
		var config = m_ARSessionManager.sessionConfiguration;

		//use object set from above to detect objects
		config.dynamicReferenceObjectsPtr = ptrReferenceObjectsSet;

		//Debug.Log("Restarting session without resetting tracking");
		session.RunWithConfigAndOptions(config, UnityARSessionRunOption.ARSessionRunOptionRemoveExistingAnchors | UnityARSessionRunOption.ARSessionRunOptionResetTracking);

	}


	public void ClearScannedObjects()
	{
		detectionMode = false;
		scannedObjects.Clear ();
		UpdateList ();
		m_ARSessionManager.StartObjectScanningSession ();
	}

	public void SaveScannedObjects()
	{
		if (scannedObjects.Count == 0)
			return;

		string pathToSaveTo = Path.Combine(Application.persistentDataPath, "ARReferenceObjects");

		if (!Directory.Exists (pathToSaveTo)) 
		{
			Directory.CreateDirectory (pathToSaveTo);
		}

		foreach (ARReferenceObject arro in scannedObjects) 
		{
			string fullPath = Path.Combine (pathToSaveTo, arro.name + ".arobject");
			arro.Save (fullPath);
		}
	}
}
