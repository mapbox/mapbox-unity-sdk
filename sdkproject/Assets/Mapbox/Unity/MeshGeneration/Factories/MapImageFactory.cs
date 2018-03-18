namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System;
	using Mapbox.Map;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;

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
		ImageryLayerProperties _properties;

		public string MapId
		{
			get
			{
				return _properties.sourceOptions.Id;
			}

			set
			{
				_properties.sourceOptions.Id = value;
			}
		}

		public override void SetOptions(LayerProperties options)
		{
			_properties = (ImageryLayerProperties)options;
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
			if (_properties.sourceType == ImagerySourceType.None)
			{
				Progress++;
				Progress--;
				return;
			}

			RasterTile rasterTile;
			if (MapId.StartsWith("mapbox://", StringComparison.Ordinal))
			{
				rasterTile = _properties.rasterOptions.useRetina ? new RetinaRasterTile() : new RasterTile();
			}
			else
			{
				rasterTile = _properties.rasterOptions.useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
			}

			tile.RasterDataState = TilePropertyState.Loading;

			tile.AddTile(rasterTile);
			Progress++;
			rasterTile.Initialize(_fileSource, tile.CanonicalTileId, MapId, () =>
			{
				if (tile == null)
				{
					Progress--;
					return;
				}

				if (rasterTile.HasError)
				{
					OnErrorOccurred(new TileErrorEventArgs(tile.CanonicalTileId, rasterTile.GetType(), tile, rasterTile.Exceptions));
					tile.RasterDataState = TilePropertyState.Error;
					Progress--;
					return;
				}

				tile.SetRasterData(rasterTile.Data, _properties.rasterOptions.useMipMap, _properties.rasterOptions.useCompression);
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