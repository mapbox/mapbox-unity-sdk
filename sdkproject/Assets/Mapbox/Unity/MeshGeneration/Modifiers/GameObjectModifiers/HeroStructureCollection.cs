using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KDTree;
using Mapbox.Unity.MeshGeneration;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;

[CreateAssetMenu(menuName = "Mapbox/HeroStructureCollection")]
public class HeroStructureCollection : ScriptableObject {

	public List<HeroStructureDataBundle> heroStructures = new List<HeroStructureDataBundle>();

	private void CacheStructureRadius()
	{
		for (int i = 0; i < heroStructures.Count; i++)
		{
			HeroStructureDataBundle structure = heroStructures[i];
			Vector2d latLon = Conversions.StringToLatLon(structure.latLon);
			structure.latLon_vector2d = latLon;
			MeshRenderer meshRenderer = structure.prefab.GetComponent<MeshRenderer>();
			Vector3 size = meshRenderer.bounds.size;
			float radius = Mathf.Max(size.x, size.z);
			//Debug.Log(meshRenderer.gameObject.name + " " + size.y);
			structure.radius = (double)System.Math.Pow(radius, 2f);
		}
	}

	private void OnValidate()
	{
		CacheStructureRadius();
	}
}
