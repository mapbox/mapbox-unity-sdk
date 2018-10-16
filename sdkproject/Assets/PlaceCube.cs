using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Utils;
public class PlaceCube : MonoBehaviour
{

	AbstractMap abstractMap;

	private void Start()
	{
		abstractMap = UnityEngine.Object.FindObjectOfType<AbstractMap>();

		Debug.Log("Awake");
		if (abstractMap == null)
		{

			Debug.LogWarning("Could not find AbstractMap...");

			enabled = false;

			return;

		}
		abstractMap.MapVisualizer.OnMapVisualizerStateChanged += OnMapReady;

	}

	private void OnMapReady(ModuleState moduleState)
	{
		Debug.Log("OnMapReady");
		if (moduleState == ModuleState.Finished)
		{
			abstractMap.MapVisualizer.OnMapVisualizerStateChanged -= OnMapReady;

			enabled = true;

			StartCoroutine(InitializePlayer());
		}

	}

	private IEnumerator InitializePlayer()
	{
		Debug.Log("InitPlayer");
		Vector2d playerLatLon = new Vector2d(36.0966, -112.0985);  //new Vector2d(37.7873886618601, -122.397240448021);

		//gameObject.transform.localScale = new Vector3(1f, 200f, 1f);
		Debug.Log("Height ->" + abstractMap.QueryElevationInMetersAt(playerLatLon));

		gameObject.transform.position = abstractMap.GeoToWorldPosition(playerLatLon, true);

		yield return null;

	}
}