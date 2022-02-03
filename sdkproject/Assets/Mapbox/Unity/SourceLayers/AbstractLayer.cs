using System;
using Mapbox.Map;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	public class LayerUpdateArgs : System.EventArgs
	{
		public AbstractTileFactory factory;
		public MapboxDataProperty property;
		public bool effectsVectorLayer;
	}

	public class VectorLayerUpdateArgs : LayerUpdateArgs
	{
		public LayerVisualizerBase visualizer;
		public ModifierBase modifier;
	}

	public abstract class AbstractLayer
	{
		protected bool IsEnabled = true;
		public bool IsLayerActive => IsEnabled;
		public event System.EventHandler UpdateLayer;
		protected virtual void NotifyUpdateLayer(LayerUpdateArgs layerUpdateArgs)
		{
			System.EventHandler handler = UpdateLayer;
			if (handler != null)
			{
				handler(this, layerUpdateArgs);
			}
		}
		protected virtual void NotifyUpdateLayer(AbstractTileFactory factory, MapboxDataProperty prop, bool effectsVectorLayer = false)
		{
			System.EventHandler handler = UpdateLayer;
			if (handler != null)
			{
				LayerUpdateArgs layerUpdateArgs =
					(factory is VectorTileFactory) ?
					new VectorLayerUpdateArgs
					{
						factory = factory,
						effectsVectorLayer = effectsVectorLayer,
						property = prop
					}
					:
					new LayerUpdateArgs
					{
						factory = factory,
						effectsVectorLayer = effectsVectorLayer,
						property = prop
					};
				handler(this, layerUpdateArgs);
			}
		}

		public virtual void Enable()
		{
			IsEnabled = true;
			OnEnabled(null);
		}

		public virtual void Disable()
		{
			IsEnabled = false;
			OnDisabled(null);
		}

		public virtual void Clear()
		{
			
		}

		public abstract void ClearTile(UnityTile tile);
		public abstract void Register(UnityTile tile);
		public abstract void Unregister(UnityTile tile);
		public abstract void Stop(UnityTile tile);

		public Action<AbstractLayer> OnDisabled = (f) => { };
		public Action<AbstractLayer> OnEnabled = (f) => { };
	}
}
