using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectText : MonoBehaviour {

	public TextMesh textMesh;

	// Use this for initialization
	void Start () {
		
	}

	public void UpdateTextMesh(string nameOfReferenceObject)
	{
		textMesh.text = nameOfReferenceObject;
	}

	// Update is called once per frame
	void Update () {
	
	}
}
