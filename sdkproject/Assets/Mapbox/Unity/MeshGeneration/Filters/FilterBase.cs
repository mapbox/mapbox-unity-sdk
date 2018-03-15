namespace Mapbox.Unity.MeshGeneration.Filters
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;

	public interface ILayerFeatureFilterComparer
	{
		bool Try(VectorFeatureUnity feature);
	}

	public class FilterBase : ILayerFeatureFilterComparer
	{
		public virtual string Key { get { return ""; } }

		public virtual bool Try(VectorFeatureUnity feature)
		{
			return true;
		}

		public virtual void Initialize()
		{

		}
	}
}