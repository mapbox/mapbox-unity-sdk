using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ballz : MonoBehaviour {

	public float yDistanceThreshold;

	private float startingY;

	// Use this for initialization
	void Start () {
		startingY = transform.position.y;
	}
	
	// Update is called once per frame
	void Update () {

		if (Mathf.Abs (startingY - transform.position.y) > yDistanceThreshold) {
			Destroy (gameObject);
		}
	}
}
