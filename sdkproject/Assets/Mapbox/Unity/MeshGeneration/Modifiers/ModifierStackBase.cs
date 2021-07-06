using UnityEngine;
using System.Collections;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;
using System;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	public class ModifierStackBase : ScriptableObject
	{
		public List<MeshModifier> MeshModifiers = new List<MeshModifier>();
		public List<GameObjectModifier> GoModifiers = new List<GameObjectModifier>();

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

		public virtual MeshData RunMeshModifiers(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, float scaler)
		{
			return null;
		}

		public virtual void Clear()
		{

		}

		public virtual void RunGoModifiers(VectorEntity entity, UnityTile tile)
		{

		}
	}
}
