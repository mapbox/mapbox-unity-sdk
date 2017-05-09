using Mapbox.Unity.Utilities;
namespace Mapbox.Unity.MeshGeneration.Factories {
	using System;
	using System.Collections.Generic;
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Platform;

	public enum MapImageType {
		BasicMapboxStyle,
		Custom,
		None
	}

	/// <summary>
	/// Uses raster image services to create materials & textures for terrain
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Map Image Factory")]
	public class MapImageFactory : Factory {
		[SerializeField]
		private MapImageType _mapIdType;
		[SerializeField]
		private string _customMapId = "";
		[SerializeField]
		private string _mapId = "";
		[SerializeField]
		public Material _baseMaterial;

		[SerializeField]
		TextureFormat _textureFormat = TextureFormat.DXT1;

		[SerializeField]
		bool _useMipMap = false;

		[SerializeField]
		bool _useRetina;

		private Dictionary<Vector2, UnityTile> _tiles;

		public override void Initialize(IFileSource fs) {
			base.Initialize(fs);
			_tiles = new Dictionary<Vector2, UnityTile>();
		}

		public override void Register(UnityTile tile) {
			base.Register(tile);
			_tiles.Add(tile.TileCoordinate, tile);
			Run(tile);
		}

		public override void Update() {
			base.Update();
			foreach (var tile in _tiles.Values) {
				Run(tile);
			}
		}

		/// <summary>
		/// Fetches the image and applies it to tile material.
		/// MapImage factory currently supports both new (RasterTile) and classic (ClassicRasterTile) Mapbox styles.
		/// </summary>
		/// <param name="tile"></param>
		private void Run(UnityTile tile) {
			if (!string.IsNullOrEmpty(_mapId)) {
				var parameters = new Tile.Parameters();
				parameters.Fs = this.FileSource;
				parameters.Id = new CanonicalTileId(tile.Zoom, (int)tile.TileCoordinate.x, (int)tile.TileCoordinate.y);
				parameters.MapId = _mapId;

				tile.ImageDataState = TilePropertyState.Loading;

				RasterTile rasterTile;
				if (parameters.MapId.StartsWith("mapbox://", StringComparison.Ordinal)) {
					rasterTile = _useRetina ? new RetinaRasterTile() : new RasterTile();
				} else {
					rasterTile = _useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
				}

				rasterTile.Initialize(parameters, (Action)(() => {
					if (rasterTile.HasError) {
						tile.ImageDataState = TilePropertyState.Error;
						return;
					}

                    var rend = tile.GetComponent<MeshRenderer>();
                    rend.material = _baseMaterial;
                    tile.ImageData = new Texture2D(0, 0, _textureFormat, _useMipMap);
                    tile.ImageData.wrapMode = TextureWrapMode.Clamp;
                    tile.ImageData.LoadImage(rasterTile.Data);
                    rend.material.mainTexture = tile.ImageData;
                    tile.ImageDataState = TilePropertyState.Loaded;

				}));
			} else {
				var rend = tile.GetComponent<MeshRenderer>();
				rend.material = _baseMaterial;
			}
		}
	}
}
