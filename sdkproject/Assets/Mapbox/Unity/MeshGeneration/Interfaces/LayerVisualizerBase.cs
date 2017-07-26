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
        public bool Active = true;
        public abstract string Key { get; set; }
        public abstract void Create(VectorTileLayer layer, UnityTile tile);
		protected WorldProperties _worldProperties;

		internal virtual void PreInitialize(WorldProperties wp)
		{
			
		}

		internal virtual void Initialize(WorldProperties wp)
		{
			_worldProperties = wp;
		}
	}
}
