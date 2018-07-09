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
	public KDTree<HeroStructureDataBundle> kdTree;

	private void BuildKDTree()
	{
		kdTree = new KDTree.KDTree<HeroStructureDataBundle>(2);
		for (int i = 0; i < heroStructures.Count; i++)
		{
			HeroStructureDataBundle structure = heroStructures[i];
			string latLonString = structure.latLon;
			Vector2d latLon = Conversions.StringToLatLon(latLonString);
			kdTree.AddPoint(new double[] { latLon.x, latLon.y }, structure);
		}
	}

	private void CacheStructureRadius()
	{
		for (int i = 0; i < heroStructures.Count; i++)
		{
			HeroStructureDataBundle structure = heroStructures[i];
			MeshRenderer meshRenderer = structure.prefab.GetComponent<MeshRenderer>();
			Vector3 size = meshRenderer.bounds.size;

			structure.radius = Mathf.Max(size.x, size.z);
		}
	}

	public List<HeroStructureDataBundle> GetListOfHeroStructuresInRange(MapOptions mapOptions)
	{
		Vector2d latLon = Conversions.StringToLatLon(mapOptions.locationOptions.latitudeLongitude);
		NearestNeighbour<HeroStructureDataBundle> pIter = kdTree.NearestNeighbors(new double[] { latLon.x, latLon.y}, 10, 10);
		List<HeroStructureDataBundle> list = new List<HeroStructureDataBundle>();
		do
		{
			HeroStructureDataBundle ve = pIter.Current;
			if(ve != null)
			{
				list.Add(ve);
				Debug.Log(ve.prefab.name + " is in range...");
			}
		}
		while (pIter.MoveNext());
		return list;
	}

	private void OnValidate()
	{
		CacheStructureRadius();
		BuildKDTree();
	}
}
