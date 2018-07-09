using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Mapbox.Editor;

using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using Mapbox.VectorTile.Geometry;
using Mapbox.Unity.MeshGeneration.Interfaces;

[System.Serializable]
public class HeroStructureDataBundle
{
	public bool active = true;
	public GameObject prefab;
	[Geocode]
	public string latLon;
	public float radius;
}
