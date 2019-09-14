using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

[RequireComponent(typeof(ReflectionProbe))]
public class ReflectionProbeGameObject : MonoBehaviour {

	ReflectionProbe reflectionProbe;
	bool latchUpdate = false;
	Cubemap latchedTexture = null;

	[SerializeField]
	GameObject debugExtentGO;

	// Use this for initialization
	void Start()
	{
		reflectionProbe = GetComponent<ReflectionProbe> ();
	}


	public void UpdateEnvironmentProbe(AREnvironmentProbeAnchor environmentProbeAnchor)
	{
		transform.position = UnityARMatrixOps.GetPosition (environmentProbeAnchor.transform);

		Quaternion rot = UnityARMatrixOps.GetRotation (environmentProbeAnchor.transform);

		//rot.z = -rot.z;
		//rot.w = -rot.w;

		transform.rotation = rot;

		if (reflectionProbe != null) 
		{
			reflectionProbe.size = environmentProbeAnchor.Extent;
		}

		if (debugExtentGO != null) 
		{
			debugExtentGO.transform.localScale = environmentProbeAnchor.Extent;
		}

		latchedTexture = environmentProbeAnchor.Cubemap;
		latchUpdate = true;
	}

	void Update()
	{
		//always make sure to update texture in next update
		if (latchUpdate && reflectionProbe != null) 
		{
			if (reflectionProbe.customBakedTexture != null)
			{
				Object.Destroy(reflectionProbe.customBakedTexture);
			}
			reflectionProbe.customBakedTexture = latchedTexture;
			latchUpdate = false;
		}
	}
}
