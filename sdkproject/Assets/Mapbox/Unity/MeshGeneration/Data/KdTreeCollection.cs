namespace Mapbox.Unity.MeshGeneration
{
	using UnityEngine;
	using KDTree;
	using Mapbox.Unity.MeshGeneration.Data;

	/// <summary>
	/// KdTree Collection is a feature collection using KdTree mainly for distance based searchs like "find all buildings 100m around
	/// player" or "find 10 closest buildings to this point".
	/// KdTree structures focus on search performance so querying for features will be very fast using this collection. On the other
	/// hand it's not good for dynamic/moving entities but we don't have such features on map so it's one of the best options for maps.
	/// </summary>

	[CreateAssetMenu(menuName = "Mapbox/Feature Collections/Kd Tree Collection")]
	public class KdTreeCollection : FeatureCollectionBase
	{
		public KDTree<VectorEntity> Entities;
		public int Count;

		public override void Initialize()
		{
			base.Initialize();
			Entities = new KDTree.KDTree<VectorEntity>(2);
		}

		public override void AddFeature(VectorEntity ve)
		{
			Entities.AddPoint(new double[] { ve.Transform.position.x, ve.Transform.position.z }, ve);
			Count = Entities.Size;
		}
	}
}
