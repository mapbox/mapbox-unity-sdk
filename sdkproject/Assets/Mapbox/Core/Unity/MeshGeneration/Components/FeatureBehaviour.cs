namespace Mapbox.Unity.MeshGeneration.Components
{
    using UnityEngine;
    using System.Linq;
    using Mapbox.Unity.MeshGeneration.Data;

    public class FeatureBehaviour : MonoBehaviour
    {
        public Transform Transform { get; set; }
        public VectorFeatureUnity Data;

        [Multiline(10)]
        public string DataString;

        public void Init(VectorFeatureUnity feature)
        {
            Transform = transform;
            Data = feature;
            DataString = string.Join(" \r\n ", Data.Properties.Select(x => x.Key + " - " + x.Value.ToString()).ToArray());
        }
    }
}