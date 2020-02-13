using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
using Collections.Hybrid.Generic;

public class GenerateEnvironmentProbeAnchors : MonoBehaviour {

	[SerializeField]
	ReflectionProbeGameObject m_ReflectionProbePrefab;

	private LinkedListDictionary<string, ReflectionProbeGameObject> probeAnchorMap;



	void Start () 
	{
		probeAnchorMap = new LinkedListDictionary<string, ReflectionProbeGameObject> ();
		UnityARSessionNativeInterface.AREnvironmentProbeAnchorAddedEvent += EnvironmentProbeAnchorAdded;
		UnityARSessionNativeInterface.AREnvironmentProbeAnchorRemovedEvent += EnvironmentProbeAnchorRemoved;
		UnityARSessionNativeInterface.AREnvironmentProbeAnchorUpdatedEvent += EnvironmentProbeAnchorUpdated;
	}

	void EnvironmentProbeAnchorUpdated (AREnvironmentProbeAnchor anchorData)
	{
		if (probeAnchorMap.ContainsKey (anchorData.identifier)) {
			probeAnchorMap [anchorData.identifier].UpdateEnvironmentProbe(anchorData);
		}

	}

	void EnvironmentProbeAnchorRemoved (AREnvironmentProbeAnchor anchorData)
	{
		if (probeAnchorMap.ContainsKey (anchorData.identifier)) {
			ReflectionProbeGameObject rpgo = probeAnchorMap [anchorData.identifier];
			GameObject.Destroy (rpgo.gameObject);
			probeAnchorMap.Remove (anchorData.identifier);
		}
	}

	void EnvironmentProbeAnchorAdded (AREnvironmentProbeAnchor anchorData)
	{
		ReflectionProbeGameObject go = GameObject.Instantiate<ReflectionProbeGameObject> (m_ReflectionProbePrefab);
		if (go != null) 
		{
			//do coordinate conversion from ARKit to Unity
			go.transform.position = UnityARMatrixOps.GetPosition (anchorData.transform);
			go.transform.rotation = UnityARMatrixOps.GetRotation (anchorData.transform);

			probeAnchorMap [anchorData.identifier] = go;
			go.UpdateEnvironmentProbe (anchorData);
		}

	}

	void OnDestroy()
	{
		UnityARSessionNativeInterface.AREnvironmentProbeAnchorAddedEvent -= EnvironmentProbeAnchorAdded;
		UnityARSessionNativeInterface.AREnvironmentProbeAnchorRemovedEvent -= EnvironmentProbeAnchorRemoved;
		UnityARSessionNativeInterface.AREnvironmentProbeAnchorUpdatedEvent -= EnvironmentProbeAnchorUpdated;

		foreach (ReflectionProbeGameObject rpgo in probeAnchorMap.Values) 
		{
			GameObject.Destroy (rpgo);
		}

		probeAnchorMap.Clear ();

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
