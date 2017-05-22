using UnityEngine;
using System.Collections;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    public class ModifierStackBase : ScriptableObject
    {
        public virtual GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
        {
            return null;
        }
    }
}