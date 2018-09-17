namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using Mapbox.VectorTile;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;

	/// <summary>
	/// Layer visualizers contains sytling logic and processes features
	/// </summary>
	public abstract class LayerVisualizerBase : ScriptableObject
	{
		public abstract bool Active { get; }
		public abstract string Key { get; set; }
		//public event Action FeaturePreProcessEvent;
		//public event Action FeaturePostProcessEvent;
		public abstract void Create(VectorTileLayer layer, UnityTile tile, Action<UnityTile, LayerVisualizerBase> callback = null);

		public event System.EventHandler LayerVisualizerHasChanged;

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
