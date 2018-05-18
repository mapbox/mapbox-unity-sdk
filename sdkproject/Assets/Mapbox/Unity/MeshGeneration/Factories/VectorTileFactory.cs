namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Map;
	using Mapbox.Unity.Map;
	using System;

	/// <summary>
	///	Vector Tile Factory
	/// Vector data is much more detailed compared to terrain and image data so we have a different structure to process 
	/// vector data(compared to other factories). First of all, how does the vector data itself structured? Vector tile 
	/// data contains 'vector layers' as immediate children.And then each of these vector layers contains a number of  
	/// 'features' inside.I.e.vector data for a tile has 'building', 'road', 'landuse' etc layers. Then building layer 
	/// has a number of polygon features, road layer has line features etc.
	/// Similar to this, vector tile factory contains bunch of 'layer visualizers' and each one of them corresponds to 
	/// one (or more) vector layers in data.So when data is received, factory goes through all layers inside and passes 
	/// them to designated layer visualizers.We're using layer name as key here, to find the designated layer visualizer, 
	/// like 'building', 'road'. (vector tile factory visual would help here). If it can't find a layer visualizer for 
	/// that layer, it'll be skipped and not processed at all.If all you need is 1-2 layers, it's indeed a big waste to 
	/// pull whole vector data and you can use 'Style Optimized Vector Tile Factory' to pull only the layer you want to use.
	/// </summary>
	//[CreateAssetMenu(menuName = "Mapbox/Factories/Vector Tile Factory")]
	public class VectorTileFactory : AbstractTileFactory
	{
		private Dictionary<string, List<LayerVisualizerBase>> _layerBuilder;
		private Dictionary<UnityTile, VectorTile> _cachedData = new Dictionary<UnityTile, VectorTile>();
		private VectorLayerProperties _properties;
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
		protected VectorDataFetcher DataFetcher;

		#region AbstractFactoryOverrides
		/// <summary>
		/// Set up sublayers using VectorLayerVisualizers.
		/// </summary>
		protected override void OnInitialized()
		{
			_layerBuilder = new Dictionary<string, List<LayerVisualizerBase>>();
			_cachedData.Clear();

			DataFetcher = ScriptableObject.CreateInstance<VectorDataFetcher>();
			DataFetcher.DataRecieved += OnVectorDataRecieved;
			DataFetcher.FetchingError += OnDataError;

			foreach (var item in _properties.locationPrefabList)
			{
				LayerVisualizerBase visualizer = CreateInstance<LocationPrefabsLayerVisualizer>();
				((LocationPrefabsLayerVisualizer)visualizer).SetProperties((PrefabItemOptions)item, _properties.performanceOptions);

				visualizer.Initialize();
				if (visualizer == null)
				{
					continue;
				}

				if (_layerBuilder.ContainsKey(visualizer.Key))
				{
					_layerBuilder[visualizer.Key].Add(visualizer);
				}
				else
				{
					_layerBuilder.Add(visualizer.Key, new List<LayerVisualizerBase>() { visualizer });
				}
			}

			foreach (var sublayer in _properties.vectorSubLayers)
			{
				//if its of type prefabitemoptions then separate the visualizer type
				LayerVisualizerBase visualizer = CreateInstance<VectorLayerVisualizer>();
				((VectorLayerVisualizer)visualizer).SetProperties(sublayer, _properties.performanceOptions);

				visualizer.Initialize();
				if (visualizer == null)
				{
					continue;
				}

				if (_layerBuilder.ContainsKey(visualizer.Key))
				{
					_layerBuilder[visualizer.Key].Add(visualizer);
				}
				else
				{
					_layerBuilder.Add(visualizer.Key, new List<LayerVisualizerBase>() { visualizer });
				}
			}
		}

		public override void SetOptions(LayerProperties options)
		{
			_properties = (VectorLayerProperties)options;
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (string.IsNullOrEmpty(MapId) || _properties.sourceOptions.isActive == false || _properties.vectorSubLayers.Count == 0)
			{
				// Do nothing; 
				Progress++;
				Progress--;
			}
			else
			{
				tile.VectorDataState = TilePropertyState.Loading;
				Progress++;
				DataFetcher.FetchVector(tile.CanonicalTileId, MapId, tile, _properties.useOptimizedStyle, _properties.optimizedStyle);
			}
		}

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(TileErrorEventArgs e)
		{
			base.OnErrorOccurred(e);
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			// We are no longer interested in this tile's notifications.
			tile.OnHeightDataChanged -= DataChangedHandler;
			tile.OnRasterDataChanged -= DataChangedHandler;

			// clean up any pending request for this tile
			if (_cachedData.ContainsKey(tile))
			{
				Progress--;
				_cachedData.Remove(tile);
			}
			if (_layerBuilder != null)
			{
				foreach (var layer in _layerBuilder.Values)
				{
					foreach (var visualizer in layer)
					{
						visualizer.UnregisterTile(tile);
					}
				}
			}
		}
		#endregion


		#region DataFetcherEvents
		private void OnVectorDataRecieved(UnityTile tile, VectorTile vectorTile)
		{
			Progress--;
			if (_cachedData.ContainsKey(tile))
			{
				_cachedData[tile] = vectorTile;
			}
			else
			{
				_cachedData.Add(tile, vectorTile);
			}

			// FIXME: we can make the request BEFORE getting a response from these!
			if (tile.HeightDataState == TilePropertyState.Loading ||
					tile.RasterDataState == TilePropertyState.Loading)
			{
				tile.OnHeightDataChanged += DataChangedHandler;
				tile.OnRasterDataChanged += DataChangedHandler;
			}
			else
			{
				CreateMeshes(tile);
			}
		}

		private void OnDataError(UnityTile tile, TileErrorEventArgs e)
		{
			if (tile != null)
			{
				Progress--;
				tile.VectorDataState = TilePropertyState.Error;
			}
		}

		#endregion


		/// <summary>
		/// Vector Factory runs after Raster&Height data recieved and processed
		/// </summary>
		/// <param name="t"></param>
		private void DataChangedHandler(UnityTile t)
		{
			if (t.RasterDataState != TilePropertyState.Loading &&
				t.HeightDataState != TilePropertyState.Loading)
			{
				CreateMeshes(t);
			}
		}

		/// <summary>
		/// Fetches the vector data and passes each layer to relevant layer visualizers
		/// </summary>
		/// <param name="tile"></param>
		private void CreateMeshes(UnityTile tile)
		{
			tile.OnHeightDataChanged -= DataChangedHandler;
			tile.OnRasterDataChanged -= DataChangedHandler;

			tile.VectorDataState = TilePropertyState.Loading;

			// TODO: move unitytile state registrations to layer visualizers. Not everyone is interested in this data
			// and we should not wait for it here!
			foreach (var layerName in _cachedData[tile].Data.LayerNames())
			{
				if (_layerBuilder.ContainsKey(layerName))
				{
					foreach (var builder in _layerBuilder[layerName])
					{
						if (builder.Active)
						{
							Progress++;
							builder.Create(_cachedData[tile].Data.GetLayer(layerName), tile, DecreaseProgressCounter);
						}
					}
				}
			}

			//emptylayer for visualizers that don't depend on outside data sources
			string emptyLayer = "";
			if (_layerBuilder.ContainsKey(emptyLayer))
			{
				foreach (var builder in _layerBuilder[emptyLayer])
				{
					if (builder.Active)
					{
						Progress++;
						//just pass the first available layer - we should create a static null layer for this
						builder.Create(_cachedData[tile].Data.GetLayer(_cachedData[tile].Data.LayerNames()[0]), tile, DecreaseProgressCounter);
					}
				}
			}

			tile.VectorDataState = TilePropertyState.Loaded;
			_cachedData.Remove(tile);
		}

		private void DecreaseProgressCounter()
		{
			Progress--;
		}
	}
}
