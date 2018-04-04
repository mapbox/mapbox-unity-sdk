namespace Mapbox.Unity.MeshGeneration
{
	using UnityEngine;
	using KDTree;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;

	/// <summary>
	/// KdTree Collection is a feature collection using KdTree mainly for distance based searchs like "find all buildings 100m around
	/// player" or "find 10 closest buildings to this point".
	/// KdTree structures focus on search performance so querying for features will be very fast using this collection. On the other
	/// hand it's not good for dynamic/moving entities but we don't have such features on map so it's one of the best options for maps.
	/// </summary>

	[CreateAssetMenu(menuName = "Mapbox/Feature Collections/Kd Tree Collection")]
	public class KdTreeCollection : FeatureCollectionBase
	{
		private KDTree<VectorEntity> _entities;
		public int Count;

		public override void Initialize()
		{
			base.Initialize();
			_entities = new KDTree.KDTree<VectorEntity>(2);
		}

		public override void AddFeature(double[] position, VectorEntity ve)
		{
			_entities.AddPoint(position, ve);
			Count = _entities.Size;
		}

		public NearestNeighbour<VectorEntity> NearestNeighbors(double[] v, int maxCount, float range)
		{
			return _entities.NearestNeighbors(v, maxCount, range);
		}
	}
}
