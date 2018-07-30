using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;

public class ActivateLight : MonoBehaviour {
	
	private MeshRenderer _mr;
	// Use this for initialization
	void Awake()
	{
		_mr = GetComponent<MeshRenderer>();
		ToggleMr();
	}

	private void ToggleMr()
	{
		if (_mr != null)
		{
			_mr.enabled = !_mr.enabled;
		}
	}

	void Start () 
	{
		AbstractMap map = FindObjectOfType<AbstractMap>();
		if(map != null)
		{
			map.OnImageLayerRedrawn += ToggleMr;
		}
	}

	// Update is called once per frame
	void Update () {
		
	}
}
