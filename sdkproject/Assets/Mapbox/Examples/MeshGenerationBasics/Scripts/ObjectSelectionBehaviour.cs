using UnityEngine;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Components;
using UnityEngine.UI;
using System;

public class ObjectSelectionBehaviour : MonoBehaviour
{
	private static FeatureUiMarker _marker;
	private FeatureBehaviour _feature;

	public void Start()
	{
		if (_marker == null)
		{
			var canv = FindObjectOfType<Canvas>();
			if (canv == null)
			{
				var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
				canv = go.GetComponent<Canvas>();
				canv.renderMode = RenderMode.ScreenSpaceOverlay;
			}

			var sel = Instantiate(Resources.Load<GameObject>("selector"));
			sel.transform.SetParent(canv.transform);
			_marker = sel.GetComponent<FeatureUiMarker>();
		}
	}

	public void OnMouseDown()
	{
		if (_feature == null)
			_feature = GetComponent<FeatureBehaviour>();
		_marker.Show(_feature);
	}
}
