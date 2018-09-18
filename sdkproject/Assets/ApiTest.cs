using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using UnityEngine;

public class ApiTest : MonoBehaviour
{
	private AbstractMap _abstractMap;
	public ImagerySourceType imagerySource = ImagerySourceType.MapboxStreets;
	readonly StyleTypes[] testStyles = new StyleTypes[3] { StyleTypes.Fantasy, StyleTypes.Realistic, StyleTypes.Simple };
	int styleId = -1;
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
	[ContextMenu("ChangeImagery")]
	public void ChangeImagery()
	{
		imagerySource = (imagerySource == ImagerySourceType.MapboxSatelliteStreet) ? ImagerySourceType.MapboxStreets : imagerySource + 1;
		_abstractMap.ImageLayer.SetLayerSource(imagerySource);
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

	[ContextMenu("ChangeBuildingMaterial")]
	public void ChangeBuildingMaterial()
	{
		styleId = (styleId == 2) ? 0 : styleId + 1;
		var layer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("ExtrudedBuildings");
		if (layer != null)
		{
			layer.SetTexturingType(testStyles[styleId]);
		}
		else
		{
			Debug.Log("Layer not found");
		}
	}

}