using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GravityWarpSetter : MonoBehaviour {


	int id;
	public float multiplier = 5, distance = 5;

	void Start () {
		id = Shader.PropertyToID("_GravityPoint");
		Set();
	}
	

	void Update () {
		Shader.SetGlobalVector("_GravityPoint", transform.position);
	}

	void OnValidate()
	{
		Set();
	}

	public void Set()
	{
		Shader.SetGlobalFloat("_GravityMulitplier", multiplier);
		Shader.SetGlobalFloat("_GravityDistance", 1 / distance);
	}
}