namespace Mapbox.Unity.MeshGeneration.Filters
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;

	public class FilterBase : ScriptableObject
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