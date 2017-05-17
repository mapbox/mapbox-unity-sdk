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

		Dictionary<UnityTile, Tile> _tiles;

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
			_tiles = new Dictionary<UnityTile, Tile>();
		}

		internal override void OnRegistered(UnityTile tile)
		{
			Run(tile);
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			if (_tiles.ContainsKey(tile))
			{
				_tiles[tile].Cancel();
				_tiles.Remove(tile);
			}
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

				tile.RasterDataState = TilePropertyState.Loading;

				RasterTile rasterTile;
				if (parameters.MapId.StartsWith("mapbox://", StringComparison.Ordinal))
				{
					rasterTile = _useRetina ? new RetinaRasterTile() : new RasterTile();
				}
				else
				{
					rasterTile = _useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
				}

				_tiles.Add(tile, rasterTile);
				rasterTile.Initialize(parameters, (Action)(() =>
				{
					// HACK: we need to check state because a cancel could have happened immediately following a response.
					if (rasterTile.HasError || rasterTile.CurrentState == Tile.State.Canceled)
					{
						tile.RasterDataState = TilePropertyState.Error;
						return;
					}

					_tiles.Remove(tile);

					if (tile.ImageData == null)
					{
						tile.ImageData = new Texture2D(0, 0, TextureFormat.RGB24, _useMipMap);
						tile.ImageData.wrapMode = TextureWrapMode.Clamp;
						tile.MeshRenderer.material = _baseMaterial;
						tile.MeshRenderer.material.mainTexture = tile.ImageData;
					}

					tile.ImageData.LoadImage(rasterTile.Data);
					if (_useCompression)
					{
						// High quality = true seems to decrease image quality?
						tile.ImageData.Compress(false);
					}

					tile.RasterDataState = TilePropertyState.Loaded;
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
