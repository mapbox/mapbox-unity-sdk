using UnityEngine;
using System.Collections;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;
using System;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    public class ModifierStackBase : ScriptableObject
    {
        public virtual GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
        {
            return null;
        }

		public virtual void Initialize()
		{
			
		}

		public void UnregisterTile(UnityTile tile)
		{
			OnUnregisterTile(tile);
		}

		public virtual void OnUnregisterTile(UnityTile tile)
		{
			
		}
	}
}