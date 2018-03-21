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
		[NodeEditorElement("Mesh Modifiers")] public List<MeshModifier> MeshModifiers;
		[NodeEditorElement("Game Object Modifiers")] public List<GameObjectModifier> GoModifiers;

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