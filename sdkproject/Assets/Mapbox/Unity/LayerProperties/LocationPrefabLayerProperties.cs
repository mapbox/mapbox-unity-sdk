using UnityEngine;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using System;

public class LocationPrefabsLayerProperties : LayerProperties
{
	public LayerSourceOptions sourceOptions = new LayerSourceOptions()
	{
		layerSource = new Style()
		{
			Id = "mapbox.mapbox-streets-v7"
		},
		isActive = true
	};
	public GameObject prefab;
	public List<PrefabItem> locationPrefabList = new List<PrefabItem>();


}