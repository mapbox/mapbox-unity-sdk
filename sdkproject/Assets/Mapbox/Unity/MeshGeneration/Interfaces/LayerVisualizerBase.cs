namespace Mapbox.Unity.MeshGeneration.Interfaces
{
    using Mapbox.VectorTile;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;

    /// <summary>
    /// Layer visualizers contains sytling logic and processes features
    /// </summary>
    public abstract class LayerVisualizerBase : ScriptableObject
    {
        public bool Active = true;
        public abstract string Key { get; set; }
        public abstract void Create(VectorTileLayer layer, UnityTile tile);
    }
}
