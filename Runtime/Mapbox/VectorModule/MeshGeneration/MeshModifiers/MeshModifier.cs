using System;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers
{
    public interface IMeshModifier
    {
        void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo);
        void Initialize();

        /// <summary>
        /// Called once per tile before any <see cref="Run"/> for that tile, on the worker thread that
        /// will run them. A modifier that wants state spanning the tile's features acquires it here.
        /// </summary>
        void BeginTile(CanonicalTileId tileId);

        /// <summary>
        /// Called once per tile after the last <see cref="Run"/> for it, on the same thread. Release
        /// whatever <see cref="BeginTile"/> acquired. Runs even when the tile produced no geometry.
        /// </summary>
        void EndTile(CanonicalTileId tileId);

        /// <summary>
        /// The tile's visuals were removed and it is no longer on the map. Drop anything still held
        /// for it. Unlike the two above this arrives on the main thread, from
        /// <c>ModifierStack.UnregisterTile</c>, and may arrive for a tile this modifier never saw.
        /// </summary>
        void UnregisterTile(CanonicalTileId tileId);
    }

    [Serializable]
    public class MeshModifier : ModifierBase, IMeshModifier
    {

        public virtual void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
        {

        }

        public virtual void Initialize()
        {

        }

        public virtual void BeginTile(CanonicalTileId tileId)
        {

        }

        public virtual void EndTile(CanonicalTileId tileId)
        {

        }

        public virtual void UnregisterTile(CanonicalTileId tileId)
        {

        }
    }
}