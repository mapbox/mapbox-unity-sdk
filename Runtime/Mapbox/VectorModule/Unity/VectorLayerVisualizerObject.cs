using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.Unity
{
	[CreateAssetMenu(menuName = "Mapbox/Layer Visualizers/Generic Vector Layer Visualizer")]
	public class VectorLayerVisualizerObject : LayerVisualizerConstructor
	{
		[SerializeField] private string _vectorLayerName;
		public string VectorLayerName => _vectorLayerName;

		[SerializeField] private VectorLayerVisualizerSettings _settings;
		[SerializeField] private List<ModifierStackObject> _modifierStackObjects;
		private VectorLayerVisualizer _layerVisualizer;
		
		public virtual IVectorLayerVisualizer GetLayerVisualizer()
		{
			return _layerVisualizer;
		}
		
		public override IVectorLayerVisualizer ConstructLayerVisualizer(IMapInformation mapInformation, UnityContext unityContext)
		{
			_layerVisualizer = new VectorLayerVisualizer(VectorLayerName, mapInformation, unityContext, _settings);
			_layerVisualizer.Active = true;
			
			foreach (var modifierStackObject in _modifierStackObjects.Where(x => x != null))
			{
				modifierStackObject.Initialize(unityContext);
			}
			_layerVisualizer.AddModifierStacks(_modifierStackObjects.Select((x, i) => x?.GetModifierStack 
				?? throw new NullReferenceException($"{name} ({nameof(VectorLayerVisualizerObject)}) is missing {nameof(ModifierStackObject)} reference @ element {i}")));

			_layerVisualizer.OnVectorMeshCreated += OnVectorMeshCreated;
			_layerVisualizer.OnVectorMeshDestroyed += OnVectorMeshDestroyed;
			
			return _layerVisualizer;
		}

		public Action<GameObject> OnVectorMeshCreated = list => { };
		public Action<GameObject> OnVectorMeshDestroyed = go => { };
	}
	
	public abstract class LayerVisualizerConstructor : ScriptableObject
	{
		public abstract IVectorLayerVisualizer ConstructLayerVisualizer(IMapInformation mapInformation, UnityContext unityContext);
	}
}