namespace Mapbox.Unity.MeshGeneration.Filters
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;

    public class FilterBase : ScriptableObject
    {
        public virtual string Key { get { return ""; } }

        public virtual bool Try(VectorFeatureUnity feature)
        {
            return true;
        }
    }
}