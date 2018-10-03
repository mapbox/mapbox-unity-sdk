using System;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Modifiers;

namespace Mapbox.Unity.Map
{
	public interface ISubLayerBehaviorModifiers
	{
		void IsBuildingIdsUnique(bool isUniqueIds);

		void AddMeshModifier(MeshModifier modifier);
		void AddMeshModifier(List<MeshModifier> modifiers);
		List<MeshModifier> GetMeshModifier(Func<MeshModifier, bool> act);
		void RemoveMeshModifier(MeshModifier modifier);

		void AddGameObjectModifier(GameObjectModifier modifier);
		void AddGameObjectModifier(List<GameObjectModifier> modifiers);
		List<GameObjectModifier> GetGameObjectModifier(Func<GameObjectModifier, bool> act);
		void RemoveGameObjectModifier(GameObjectModifier modifier);
	}
}