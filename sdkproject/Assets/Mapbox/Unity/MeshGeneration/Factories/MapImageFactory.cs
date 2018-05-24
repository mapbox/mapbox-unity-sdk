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
		protected ImageDataFetcher DataFetcher;
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

		#region UnityMethods
		protected virtual void OnDestroy()
		{
			if (DataFetcher != null)
			{
				DataFetcher.DataRecieved -= OnImageRecieved;
				DataFetcher.FetchingError -= OnDataError;
			}
		}
		#endregion

		#region DataFetcherEvents
		private void OnImageRecieved(UnityTile tile, RasterTile rasterTile)
		{
			if (tile != null)
			{
				Progress--;
				tile.SetRasterData(rasterTile.Data, _properties.rasterOptions.useMipMap, _properties.rasterOptions.useCompression);
				tile.RasterDataState = TilePropertyState.Loaded;
			}
		}

		//merge this with OnErrorOccurred?
		protected virtual void OnDataError(UnityTile tile, TileErrorEventArgs e)
		{
			if (tile != null)
			{
				Progress--;
				tile.RasterDataState = TilePropertyState.Error;
				OnErrorOccurred(e);
			}
		}
		#endregion

		#region AbstractFactoryOverrides
		protected override void OnInitialized()
		{
			DataFetcher = ScriptableObject.CreateInstance<ImageDataFetcher>();
			DataFetcher.DataRecieved += OnImageRecieved;
			DataFetcher.FetchingError += OnDataError;
		}

		public override void SetOptions(LayerProperties options)
		{
			_properties = (ImageryLayerProperties)options;
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (_properties.sourceType == ImagerySourceType.None)
			{
				Progress++;
				Progress--;
				return;
			}

			tile.RasterDataState = TilePropertyState.Loading;
			Progress++;
			DataFetcher.FetchImage(tile.CanonicalTileId, MapId, tile, _properties.rasterOptions.useRetina);
		}

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(TileErrorEventArgs e)
		{
		}

		protected override void OnUnregistered(UnityTile tile)
		{

		}


		#endregion
	}
}