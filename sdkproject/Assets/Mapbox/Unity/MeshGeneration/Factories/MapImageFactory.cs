namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;

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

		[SerializeField]
		private string _customMapId = "";

		[SerializeField]
		private string _mapId = "";

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
			// TODO: simplify this across tile factories? Cancel tile, but only if it needs to be cancelled.
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
			var parameters = new Tile.Parameters();
			parameters.Fs = this.FileSource;
			parameters.Id = new CanonicalTileId(tile.Zoom, (int)tile.TileCoordinate.x, (int)tile.TileCoordinate.y);
			parameters.MapId = _mapId;

			RasterTile rasterTile;
			if (parameters.MapId.StartsWith("mapbox://", StringComparison.Ordinal))
			{
				rasterTile = _useRetina ? new RetinaRasterTile() : new RasterTile();
			}
			else
			{
				rasterTile = _useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
			}

			tile.RasterDataState = TilePropertyState.Loading;

			_tiles.Add(tile, rasterTile);
			rasterTile.Initialize(parameters, () =>
			{
				// HACK: we need to check state because a cancel could have happened immediately following a response.
				if (rasterTile.HasError || rasterTile.CurrentState == Tile.State.Canceled)
				{
					tile.RasterDataState = TilePropertyState.Error;
					return;
				}

				_tiles.Remove(tile);

				//// Don't leak the texture, just reuse it.
				//if (tile.RasterData == null)
				//{
				//	tile.RasterData = new Texture2D(0, 0, TextureFormat.RGB24, _useMipMap);
				//	tile.RasterData.wrapMode = TextureWrapMode.Clamp;
				//	tile.MeshRenderer.material = _baseMaterial;
				//	tile.MeshRenderer.material.mainTexture = tile.RasterData;
				//}

				//tile.RasterData.LoadImage(rasterTile.Data);
				//if (_useCompression)
				//{
				//	// High quality = true seems to decrease image quality?
				//	tile.RasterData.Compress(false);
				//}

				tile.SetRasterData(rasterTile.Data, _useMipMap, _useCompression);
				tile.RasterDataState = TilePropertyState.Loaded;
			});
		}
	}
}