namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System;
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;

	public enum MapImageType
	{
		BasicMapboxStyle,
		Custom,
		None
	}

	/// <summary>
	/// Uses raster image services to create materials & textures for terrain
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Image Factory")]
	public class MapImageFactory : AbstractTileFactory
	{
		[SerializeField]
		private MapImageType _mapIdType;

		[SerializeField]
		[StyleSearch]
		Style _customStyle;

		[SerializeField]
		private string _mapId = "";

		[SerializeField]
		bool _useCompression = true;

		[SerializeField]
		bool _useMipMap = false;

		[SerializeField]
		bool _useRetina;

		public string MapId
		{
			get
			{
				return _mapId;
			}

			set
			{
				_mapId = value;
			}
		}

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
			if (_mapIdType == MapImageType.None)
				return;

			RasterTile rasterTile;
			if (_mapId.StartsWith("mapbox://", StringComparison.Ordinal))
			{
				rasterTile = _useRetina ? new RetinaRasterTile() : new RasterTile();
			}
			else
			{
				rasterTile = _useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
			}

			tile.RasterDataState = TilePropertyState.Loading;

			tile.AddTile(rasterTile);
			Progress++;
			rasterTile.Initialize(_fileSource, tile.CanonicalTileId, _mapId, () =>
			{
				if (tile == null)
				{
					return;
				}

				if (rasterTile.HasError)
				{
					OnErrorOccurred(new TileErrorEventArgs(tile.CanonicalTileId,rasterTile.GetType(),tile, rasterTile.Exceptions));
					tile.RasterDataState = TilePropertyState.Error;
					Progress--;
					return;
				}

				tile.SetRasterData(rasterTile.Data, _useMipMap, _useCompression);
				tile.RasterDataState = TilePropertyState.Loaded;
				Progress--;
			});
		}

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(TileErrorEventArgs e)
		{
			base.OnErrorOccurred(e);
		}

		internal override void OnUnregistered(UnityTile tile)
		{

		}
	}
}