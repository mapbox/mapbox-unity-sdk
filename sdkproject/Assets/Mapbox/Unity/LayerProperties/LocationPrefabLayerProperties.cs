using UnityEngine;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using System;

[Serializable]
public class LocationPrefabsLayerProperties : LayerProperties
{
	public List<PrefabItemOptions> locationPrefabList = new List<PrefabItemOptions>();
}