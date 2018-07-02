using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KDTree;
using Mapbox.Unity.MeshGeneration;

[CreateAssetMenu(menuName = "Mapbox/HeroStructureCollection")]
public class HeroStructureCollection : ScriptableObject {

	public List<HeroStructureDataBundle> heroStructures = new List<HeroStructureDataBundle>();
	public KdTreeCollection Collection;

	private void BuildKDTree()
	{
		
	}

	private void OnValidate()
	{
		Debug.Log("OnValidate");
		BuildKDTree();
	}
}
