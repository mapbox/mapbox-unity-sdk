using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereClipSetter : MonoBehaviour {

	public float distance = 5;

	void Start()
	{
		Set();
	}


	void Update()
	{
		Shader.SetGlobalVector("_ClipPoint", transform.position);
	}

	void OnValidate()
	{
		Set();
	}

	public void Set()
	{
		Shader.SetGlobalFloat("_ClipDistance", distance);
	}
}
