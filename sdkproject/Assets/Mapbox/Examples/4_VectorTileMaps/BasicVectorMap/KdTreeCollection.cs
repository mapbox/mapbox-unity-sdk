using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mapbox/Feature Collection (Kd Tree)")]
public class KdTreeCollection : FeatureCollectionBase
{
	public KDTree.KDTree<VectorEntity> Entities;
	public int Count;

	private void OnEnable()
	{
		Entities = new KDTree.KDTree<VectorEntity>(2);
	}

	public override void AddFeature(VectorEntity ve)
	{
		Entities.AddPoint(new double[] { ve.Transform.position.x, ve.Transform.position.z }, ve);
		Count = Entities.Size;
	}
}
