using UnityEngine;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Components;
using UnityEngine.UI;
using System;

public class FeatureSelectionDetector : MonoBehaviour
{
	private FeatureUiMarker _marker;
	private FeatureBehaviour _feature;
	
	public void OnMouseDown()
	{
		_marker.Show(_feature);
	}

	internal void Initialize(FeatureUiMarker marker, FeatureBehaviour fb)
	{
		_marker = marker;
		_feature = fb;
	}
}
