namespace Mapbox.Unity.MeshGeneration.Factories
{
    using System.Collections.Generic;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Enums;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Interfaces;
    using Mapbox.Platform;

    [CreateAssetMenu(menuName = "Mapbox/Factories/Mesh Factory")]
    public class MeshFactory : Factory
    {
        [SerializeField]
        private string _mapId = "mapbox.mapbox-streets-v7";
        public List<LayerVisualizerBase> Visualizers;

        private Dictionary<Vector2, UnityTile> _tiles;
        private Dictionary<string, List<LayerVisualizerBase>> _layerBuilder;

        public override void Initialize(IFileSource fs)
        {
            base.Initialize(fs);
            _tiles = new Dictionary<Vector2, UnityTile>();
            _layerBuilder = new Dictionary<string, List<LayerVisualizerBase>>();
            foreach (LayerVisualizerBase factory in Visualizers)
            {
                if (_layerBuilder.ContainsKey(factory.Key))
                {
                    _layerBuilder[factory.Key].Add(factory);
                }
                else
                {
                    _layerBuilder.Add(factory.Key, new List<LayerVisualizerBase>() { factory });
                }
            }
        }

        public override void Register(UnityTile tile)
        {
            base.Register(tile);
            _tiles.Add(tile.TileCoordinate, tile);
            Run(tile);
        }

        private void Run(UnityTile tile)
        {
            if (tile.HeightDataState == TilePropertyState.Loading ||
                tile.ImageDataState == TilePropertyState.Loading)
            {
                tile.HeightDataChanged += HeightDataChangedHandler;
                tile.ImageDataChanged += ImageDataChangedHandler;
            }
            else
            {
                CreateMeshes(tile, null);
            }
        }

        private void HeightDataChangedHandler(UnityTile t, object e)
        {
            if (t.ImageDataState != TilePropertyState.Loading)
                CreateMeshes(t, e);
        }

        private void ImageDataChangedHandler(UnityTile t, object e)
        {
            if (t.HeightDataState != TilePropertyState.Loading)
                CreateMeshes(t, e);
        }

        private void CreateMeshes(UnityTile tile, object e)
        {
            tile.HeightDataChanged -= HeightDataChangedHandler;
            tile.ImageDataChanged -= ImageDataChangedHandler;

            var parameters = new Mapbox.Map.Tile.Parameters
            {
                Fs = this.FileSource,
                Id = new Mapbox.Map.CanonicalTileId(tile.Zoom, (int)tile.TileCoordinate.x, (int)tile.TileCoordinate.y),
                MapId = _mapId
            };

            var vectorTile = new Mapbox.Map.VectorTile();
            vectorTile.Initialize(parameters, () =>
            {
                if (vectorTile.Error != null)
                {
                    Debug.Log(vectorTile.Error);
                    return;
                }

                foreach (var layerName in vectorTile.Data.LayerNames())
                {
                    if (_layerBuilder.ContainsKey(layerName))
                    {
                        foreach (var builder in _layerBuilder[layerName])
                        {
                            if (builder.Active)
                                builder.Create(vectorTile.Data.GetLayer(layerName), tile);
                        }
                    }
                }
            });
        }
    }
}
