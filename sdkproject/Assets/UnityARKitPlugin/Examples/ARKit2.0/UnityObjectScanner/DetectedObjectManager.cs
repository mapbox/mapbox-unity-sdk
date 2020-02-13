using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Collections.Hybrid.Generic;
using UnityEngine.XR.iOS;

public class DetectedObjectManager : MonoBehaviour {

	public GameObject m_ObjectPrefab;

	private LinkedListDictionary<string, GameObject> objectAnchorMap;

	// Use this for initialization
	void Start () {
		objectAnchorMap = new LinkedListDictionary<string, GameObject> ();
		UnityARSessionNativeInterface.ARObjectAnchorAddedEvent += ObjectAnchorAdded;
		UnityARSessionNativeInterface.ARObjectAnchorRemovedEvent +=  ObjectAnchorRemoved;
		UnityARSessionNativeInterface.ARObjectAnchorUpdatedEvent +=  ObjectAnchorUpdated;
	}

	void ObjectAnchorUpdated (ARObjectAnchor anchorData)
	{
		Debug.Log ("ObjectAnchorUpdated");
		if (objectAnchorMap.ContainsKey (anchorData.referenceObjectName)) {
			GameObject go = objectAnchorMap [anchorData.referenceObjectName];
			//do coordinate conversion from ARKit to Unity
			go.transform.position = UnityARMatrixOps.GetPosition (anchorData.transform);
			go.transform.rotation = UnityARMatrixOps.GetRotation (anchorData.transform);

		}

	}

	void ObjectAnchorRemoved (ARObjectAnchor anchorData)
	{
		Debug.Log ("ObjectAnchorRemoved");
		if (objectAnchorMap.ContainsKey (anchorData.referenceObjectName)) {
			GameObject rpgo = objectAnchorMap [anchorData.referenceObjectName];
			GameObject.Destroy (rpgo.gameObject);
			objectAnchorMap.Remove (anchorData.identifier);
		}
	}

	void ObjectAnchorAdded (ARObjectAnchor anchorData)
	{
		Debug.Log ("ObjectAnchorAdded");
		GameObject go = GameObject.Instantiate<GameObject> (m_ObjectPrefab);
		if (go != null) 
		{
			//do coordinate conversion from ARKit to Unity
			go.transform.position = UnityARMatrixOps.GetPosition (anchorData.transform);
			go.transform.rotation = UnityARMatrixOps.GetRotation (anchorData.transform);

			objectAnchorMap [anchorData.referenceObjectName] = go;
			go.name = anchorData.referenceObjectName;
			ObjectText objText = go.GetComponent<ObjectText> ();
			if (objText) 
			{
				objText.UpdateTextMesh (anchorData.referenceObjectName);
			}

		}

	}

	void OnDestroy()
	{
		UnityARSessionNativeInterface.ARObjectAnchorAddedEvent -= ObjectAnchorAdded;
		UnityARSessionNativeInterface.ARObjectAnchorRemovedEvent -=  ObjectAnchorRemoved;
		UnityARSessionNativeInterface.ARObjectAnchorUpdatedEvent -=  ObjectAnchorUpdated;

		foreach (GameObject rpgo in objectAnchorMap.Values) 
		{
			GameObject.Destroy (rpgo);
		}

		objectAnchorMap.Clear ();

	}


	// Update is called once per frame
	void Update () {
		
	}
}
