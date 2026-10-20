using System;
using System.Threading;
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

    /// <summary>
    /// A mesh modifier whose per-feature work lives in a separate <typeparamref name="TCore"/>.
    ///
    /// The cores exist so a modifier instance - shared by every tile being generated concurrently -
    /// holds no mutable state of its own. Allocating a core per feature achieved that at the cost of
    /// an object per feature; one core per thread achieves the same thing, because a tile is one task
    /// on one thread and so a thread's core is never touched by two tiles at once.
    ///
    /// The contract that makes this safe: a core must reset its own per-feature state rather than
    /// relying on being freshly constructed. Every shipped core already does.
    /// </summary>
    public abstract class MeshModifier<TCore> : MeshModifier where TCore : class
    {
        [NonSerialized] private ThreadLocal<TCore> _cores;

        /// <summary>This thread's core, built on first use.</summary>
        protected TCore Core => (_cores ??= new ThreadLocal<TCore>(CreateCore)).Value;

        protected abstract TCore CreateCore();

        public override void Initialize()
        {
            // A core captures the modifier's settings when it is built, so drop the built ones and
            // let each thread rebuild from whatever the stack was just configured with.
            _cores?.Dispose();
            _cores = null;
        }
    }
}
