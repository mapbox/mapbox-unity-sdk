using Mapbox.Unity.MeshGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;

public class MoveMap : MonoBehaviour
{
	public AbstractMapVisualizer MapVisualizer;
	public Vector3 MoveDelta;

	void Start()
	{
		MapVisualizer.OnMapVisualizerStateChanged += (s) =>
		{
			if (s == ModuleState.Finished)
				transform.position += MoveDelta;
		};
	}
}