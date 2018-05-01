namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using Mapbox.VectorTile;
	using Mapbox.Unity.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;

	/// <summary>
	/// Layer visualizers contains sytling logic and processes features
	/// </summary>
	public abstract class LayerVisualizerBase : ScriptableObject
    {
        public bool Active = true;
        public abstract string Key { get; set; }
        public abstract void Create(VectorTileLayer layer, UnityTile tile, Action callback = null);

		protected IMapReadable _map;

		public virtual void Initialize( IMapReadable map)
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
