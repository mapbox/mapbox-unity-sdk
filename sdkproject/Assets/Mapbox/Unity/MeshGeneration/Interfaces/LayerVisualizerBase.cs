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

		public abstract void Create(VectorTileLayer layer, UnityTile tile, Action<UnityTile, LayerVisualizerBase> callback = null);

		public event System.EventHandler LayerVisualizerHasChanged;

		public virtual void Initialize()
		{

		}
		public virtual void InitializeStack()
		{

		}
		public virtual void SetProperties(VectorSubLayerProperties properties)
		{

		}

		public virtual void Clear()
		{

		}

		public void UnregisterTile(UnityTile tile)
		{
			OnUnregisterTile(tile);
		}

		public virtual void OnUnregisterTile(UnityTile tile)
		{

		}

		public virtual void UnbindSubLayerEvents()
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
