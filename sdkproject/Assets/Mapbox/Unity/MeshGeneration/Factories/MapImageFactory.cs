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

		}

		internal override void OnRegistered(UnityTile tile)
		{
			var parameters = new Tile.Parameters();
			parameters.Fs = _fileSource;
			parameters.Id = tile.CanonicalTileId;
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

				tile.SetRasterData(rasterTile.Data, _useMipMap, _useCompression);
				tile.RasterDataState = TilePropertyState.Loaded;
			});
		}

		internal override void OnUnregistered(UnityTile tile)
		{

		}
	}
}