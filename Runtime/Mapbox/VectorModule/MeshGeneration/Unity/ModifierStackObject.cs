using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.MeshGeneration.GameObjectModifiers;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.Unity
{
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Modifier Stack")]
	public class ModifierStackObject : ScriptableObject, IModifierStack
	{
		private ModifierStack _modifierStack;
		public ModifierStack GetModifierStack => _modifierStack;

		public ModifierStackSettings Settings;
		public VectorFilterStackObject Filters;
		public List<ScriptableMeshModifierObject> MeshModifiers = new List<ScriptableMeshModifierObject>();
		public List<ScriptableGameObjectModifierObject> GoModifiers = new List<ScriptableGameObjectModifierObject>();

		public void Initialize(UnityContext unityContext = null)
		{
			var filterCombiner = Filters == null ? null : Filters.GetCombiner();

			// Empty slots and unticked modifiers are dropped here rather than checked per feature.
			// The Inspector's enable toggle was drawn and never read, so unticking a modifier left
			// it running; and an empty slot in GoModifiers reached RunGoModifiers, which - unlike
			// RunMeshModifiers - does not null-check.
			var activeMeshModifiers = MeshModifiers.Where(x => x != null && x.Active).ToList();
			var activeGoModifiers = GoModifiers.Where(x => x != null && x.Active).ToList();

			_modifierStack = new ModifierStack(Settings, filterCombiner)
			{
				MeshModifiers = activeMeshModifiers.Select(x => x as IMeshModifier).ToList(),
				GoModifiers = activeGoModifiers.Select(x => x as IGameObjectModifier).ToList()
			};
			foreach (var modifier in activeMeshModifiers)
			{
				modifier.ConstructModifier(unityContext);
			}

			foreach (var modifier in activeGoModifiers)
			{
				modifier.ConstructModifier(unityContext);
			}
		}

		public MeshData RunMeshModifiers(VectorFeatureUnity feature, MeshData meshData, IMapInformation mapInfo)
		{
			return _modifierStack.RunMeshModifiers(feature, meshData, mapInfo);
		}

		public void RunGoModifiers(VectorEntity entity, IMapInformation mapInformation)
		{
			_modifierStack.RunGoModifiers(entity, mapInformation);
		}

		public void Finalize(VectorEntity entity)
		{
			_modifierStack.Finalize(entity);
		}
	}
}