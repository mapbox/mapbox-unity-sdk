using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.MeshGeneration.Modifiers;

namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using Mapbox.VectorTile;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;

	/// <summary>
	/// Layer visualizers contains sytling logic and processes features
	/// </summary>
	public abstract class LayerVisualizerBase : ScriptableObject
	{
		public abstract bool Active { get; }
		public abstract string Key { get; set; }
		public abstract VectorSubLayerProperties SubLayerProperties { get; set; }

		protected LayerPerformanceOptions _performanceOptions;
		protected VectorSubLayerProperties _sublayerProperties;
		protected List<ModifierStack> _modifierStacks;
		//protected HashSet<ModifierBase> _coreModifiers = new HashSet<ModifierBase>();

		public abstract void Create(UnityTile tile, Action<UnityTile, LayerVisualizerBase> callback = null);

		public event System.EventHandler LayerVisualizerHasChanged;

		public virtual void Initialize()
		{

		}

		public virtual void Enable()
		{
			_sublayerProperties.coreOptions.isActive = true;
		}

		public virtual void Disable()
		{
			_sublayerProperties.coreOptions.isActive = false;
		}

		public virtual void SetProperties(VectorSubLayerProperties properties)
		{
			//_coreModifiers = new HashSet<ModifierBase>();

			if (_sublayerProperties == null && properties != null)
			{
				_sublayerProperties = properties;
				if (_performanceOptions == null && properties.performanceOptions != null)
				{
					_performanceOptions = properties.performanceOptions;
				}
			}

			_modifierStacks = new List<ModifierStack>();
			foreach (var modifierStack in _sublayerProperties.ModifierStacks)
			{
				_modifierStacks.Add(modifierStack);
			}

			// Setup material options.
			_sublayerProperties.materialOptions.SetDefaultMaterialOptions();

			_sublayerProperties.coreOptions.PropertyHasChanged += UpdateVector;
			_sublayerProperties.filterOptions.PropertyHasChanged += UpdateVector;

			_sublayerProperties.filterOptions.RegisterFilters();

			_sublayerProperties.PropertyHasChanged += UpdateVector;
		}

		public virtual void Clear()
		{

		}

		public void UnregisterTile(UnityTile tile)
		{
			OnUnregisterTile(tile);
		}

		public void ClearTile(UnityTile tile)
		{
			OnClearTile(tile);
		}

		protected abstract void OnUnregisterTile(UnityTile tile);

		protected abstract void OnClearTile(UnityTile tile);

		public virtual void UnbindSubLayerEvents()
		{
			_sublayerProperties.extrusionOptions.PropertyHasChanged -= UpdateVector;
			_sublayerProperties.coreOptions.PropertyHasChanged -= UpdateVector;
			_sublayerProperties.filterOptions.PropertyHasChanged -= UpdateVector;
			_sublayerProperties.filterOptions.UnRegisterFilters();
			_sublayerProperties.materialOptions.PropertyHasChanged -= UpdateVector;

			_sublayerProperties.PropertyHasChanged -= UpdateVector;
		}

		protected virtual void UpdateVector(object sender, EventArgs e)
		{

		}

		protected virtual void OnUpdateLayerVisualizer(System.EventArgs e)
		{
			System.EventHandler handler = LayerVisualizerHasChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}
	}
}
