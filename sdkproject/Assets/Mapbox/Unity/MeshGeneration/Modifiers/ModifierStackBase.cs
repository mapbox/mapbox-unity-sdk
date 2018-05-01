using UnityEngine;
using System.Collections;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;
using System;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	public class ModifierStackBase : ScriptableObject
	{
		[NodeEditorElement("Mesh Modifiers")] public List<MeshModifier> MeshModifiers;
		[NodeEditorElement("Game Object Modifiers")] public List<GameObjectModifier> GoModifiers;

		protected IMapReadable _map;

		public virtual GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
		{
			return null;
		}

		public virtual void Initialize( IMapReadable map )
		{
			_map = map;
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