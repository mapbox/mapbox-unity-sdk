using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mapbox/Feature Collection (List)")]
public class ListFeatureCollection : FeatureCollectionBase
{
	public List<VectorEntity> Entities;

	private void OnEnable()
	{
		Entities = new List<VectorEntity>();
	}

	public override void AddFeature(VectorEntity ve)
	{
		Entities.Add(ve);
	}
}
