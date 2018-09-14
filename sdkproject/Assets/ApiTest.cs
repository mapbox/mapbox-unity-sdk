using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using UnityEngine;

public class ApiTest : MonoBehaviour
{
	private AbstractMap _abstractMap;

	void Start()
	{
		_abstractMap = FindObjectOfType<AbstractMap>();
	}

	[ContextMenu("EnableColliders")]
	public void EnableColliders()
	{
		_abstractMap.Terrain.LayerProperty.SetCollider(true);
	}

	[ContextMenu("DisableColliders")]
	public void DisableColliders()
	{
		_abstractMap.Terrain.LayerProperty.SetCollider(false);
	}


	[ContextMenu("DisableLayer")]
	public void DisableLayer()
	{
		var layer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("ExtrudedBuildings");
		if (layer != null)
		{
			layer.SetActive(false);
		}
		else
		{
			Debug.Log("Layer not found");
		}
	}

}