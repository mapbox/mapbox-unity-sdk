using UnityEngine;
using System.Collections;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    public class ModifierStackBase : ScriptableObject
    {
        [SerializeField]
        private List<MeshModifier> _baseModifiers;

        public virtual GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
        {
            foreach (var mod in _baseModifiers)
            {
                mod.Run(feature, meshData, tile);
            }
            return null;
        }
    }
}