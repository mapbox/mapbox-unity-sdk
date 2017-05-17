namespace Mapbox.Unity.MeshGeneration.Factories
{
    using System;
    using System.Collections.Generic;
    using Mapbox.Map;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Enums;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Platform;

    public enum MapImageType
    {
        BasicMapboxStyle,
        Custom,
        None
    }

    /// <summary>
    /// Uses raster image services to create materials & textures for terrain
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Factories/Map Image Factory")]
	public class MapImageFactory : AbstractTileFactory
    {
        [SerializeField]
        private MapImageType _mapIdType;

		// TODO: fix or remove?
		[SerializeField]
        private string _customMapId = "";

        [SerializeField]
        private string _mapId = "";
        [SerializeField]
        public Material _baseMaterial;

        [SerializeField]
        bool _useCompression = true;

        [SerializeField]
        bool _useMipMap = false;

        [SerializeField]
        bool _useRetina;

		// TODO: come back to this
        //public override void Update()
        //{
        //    base.Update();
        //    foreach (var tile in _tiles.Values)
        //    {
        //        Run(tile);
        //    }
        //}

		internal override void OnInitialized()
		{
			// ? 
		}

		internal override void OnRegistered(UnityTile tile)
		{
			Run(tile);
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			// ?
		}

        /// <summary>
        /// Fetches the image and applies it to tile material.
        /// MapImage factory currently supports both new (RasterTile) and classic (ClassicRasterTile) Mapbox styles.
        /// </summary>
        /// <param name="tile"></param>
        private void Run(UnityTile tile)
        {
            if (!string.IsNullOrEmpty(_mapId))
            {
                var parameters = new Tile.Parameters();
                parameters.Fs = this.FileSource;
                parameters.Id = new CanonicalTileId(tile.Zoom, (int)tile.TileCoordinate.x, (int)tile.TileCoordinate.y);
                parameters.MapId = _mapId;

                tile.ImageDataState = TilePropertyState.Loading;

                RasterTile rasterTile;
                if (parameters.MapId.StartsWith("mapbox://", StringComparison.Ordinal))
                {
                    rasterTile = _useRetina ? new RetinaRasterTile() : new RasterTile();
                }
                else
                {
                    rasterTile = _useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
                }

                rasterTile.Initialize(parameters, (Action)(() =>
                {
					// FIXME: handle tile has been removed before response!
					// We can do this by cancelling the tile if we can get a reference to it.
                    if (rasterTile.HasError)
                    {
                        tile.ImageDataState = TilePropertyState.Error;
                        return;
                    }

					// TODO: Optimize--get from unitytile object?
                    var rend = tile.GetComponent<MeshRenderer>();
                    rend.material = _baseMaterial;
                    tile.ImageData = new Texture2D(0, 0, TextureFormat.RGB24, _useMipMap);
                    tile.ImageData.wrapMode = TextureWrapMode.Clamp;
                    tile.ImageData.LoadImage(rasterTile.Data);
                    if (_useCompression)
                    {
                        // High quality = true seems to decrease image quality?
                        tile.ImageData.Compress(false);
                    }
                    rend.material.mainTexture = tile.ImageData;
                    tile.ImageDataState = TilePropertyState.Loaded;

                }));
            }
            else
            {
                var rend = tile.GetComponent<MeshRenderer>();
                rend.material = _baseMaterial;
            }
        }
	}
}
