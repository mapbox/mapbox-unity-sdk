using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;

public class MapTransform : MonoBehaviour {

	public Transform positionToTrack;

	// Update is called once per frame
	void Update () {

		transform.position = positionToTrack.position;

	}
}
