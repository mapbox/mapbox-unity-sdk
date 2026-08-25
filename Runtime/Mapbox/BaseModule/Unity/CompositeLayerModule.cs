using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;

public class CompositeLayerModule : ILayerModule
{
    public List<ILayerModule> LayerModules;

    public CompositeLayerModule(List<ILayerModule> layerModules)
    {
        LayerModules = layerModules;
    }
    
    /// <summary>
    /// Initializes each child in turn, passing the tile-lifecycle source straight through so a
    /// nested module can observe tiles exactly as a top-level one does.
    /// </summary>
    public virtual IEnumerator Initialize(ITileLifecycleSource tileLifecycle)
    {
        foreach (var module in LayerModules)
        {
            yield return module.Initialize(tileLifecycle);
        }
    }

    public virtual void LoadTempTile(UnityMapTile tile)
    {
        foreach (var module in LayerModules)
        {
            module.LoadTempTile(tile);
        }
    }

    public virtual bool LoadInstant(UnityMapTile unityTile)
    {
        foreach (var module in LayerModules)
        {
            var moduleDone = module.LoadInstant(unityTile);
            if (!moduleDone) return false;
        }

        return true;
    }

    /// <summary>
    /// Loads the given tiles in every child module, concurrently, and completes when they all have.
    ///
    /// The returned enumerators have to be yielded, not merely obtained. Every implementation of
    /// LoadTiles is an iterator method, so calling one builds its state machine and executes none
    /// of its body - this used to call each child and drop the result on the floor, then yield once,
    /// so no child module ever loaded anything and the composite reported itself done immediately.
    /// </summary>
    public IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles)
    {
        // Materialized because the sequence is handed to several modules that each enumerate it on
        // their own schedule, and callers pass reused scratch collections (TerrainLayerModule's
        // GetDataId returns one) that can be cleared out from under a lazy consumer.
        var materialized = new List<CanonicalTileId>(tiles);
        yield return LayerModules.Select(x => x.LoadTiles(materialized)).Where(x => x != null).WaitForAll();
    }

    public IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles)
    {
        foreach (var module in LayerModules)
        {
            foreach (var routine in module.GetTileCoverCoroutines(tiles))
            {
                if (routine != null)
                {
                    yield return routine;
                }

            }
        }
    }

    /// <summary>
    /// Retains the given tiles in every child module, reporting true only if all of them are ready.
    /// It used to discard each child's answer and hardcode true, so a composite claimed readiness
    /// even while a child said otherwise - LandmarksLayerModule, for one, computes a real value.
    /// </summary>
    public virtual bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
    {
        var allReady = true;
        foreach (var module in LayerModules)
        {
            // Deliberately not short-circuiting. Every module has to be told what to retain, or the
            // ones sitting after an unready module would stop retaining and have their tiles
            // recycled out from under them - so evaluate all of them, then combine.
            if (!module.RetainTiles(retainedTiles))
            {
                allReady = false;
            }
        }

        return allReady;
    }
    
    public virtual void UpdatePositioning(IMapInformation mapInfo)
    {
        foreach (var module in LayerModules)
        {
            module.UpdatePositioning(mapInfo);
        }
    }

    public virtual void OnDestroy()
    {
        foreach (var module in LayerModules)
        {
            module.OnDestroy();
        }
    }
}