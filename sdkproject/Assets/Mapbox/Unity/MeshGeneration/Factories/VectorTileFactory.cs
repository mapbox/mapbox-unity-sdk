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

		public VectorDataFetcher GetFetcher()
		{
			return DataFetcher;
		}

		public VectorLayerProperties Properties
		{
			get
			{
				return _properties;
			}
		}
		private Dictionary<UnityTile, HashSet<LayerVisualizerBase>> _layerProgress;

		#region AbstractFactoryOverrides
		/// <summary>
		/// Set up sublayers using VectorLayerVisualizers.
		/// </summary>
		protected override void OnInitialized()
		{
			_layerProgress = new Dictionary<UnityTile, HashSet<LayerVisualizerBase>>();
			_layerBuilder = new Dictionary<string, List<LayerVisualizerBase>>();

			DataFetcher = ScriptableObject.CreateInstance<VectorDataFetcher>();
			DataFetcher.DataRecieved += OnVectorDataRecieved;
			DataFetcher.FetchingError += OnDataError;

			CreatePOILayerVisualizers();

			CreateLayerVisualizers();
		}

		private void CreatePOILayerVisualizers()
		{
			foreach (var item in _properties.locationPrefabList)
			{
				AddPOIVectorLayerVisualizer(item);
			}
		}

		private void CreateLayerVisualizers()
		{
			foreach (var sublayer in _properties.vectorSubLayers)
			{
				AddVectorLayerVisualizer(sublayer);
			}
		}

		public virtual LayerVisualizerBase AddVectorLayerVisualizer(VectorSubLayerProperties subLayer)
		{
			//if its of type prefabitemoptions then separate the visualizer type
			LayerVisualizerBase visualizer = CreateInstance<VectorLayerVisualizer>();

			//TODO : FIX THIS !!
			visualizer.LayerVisualizerHasChanged += UpdateTileFactory;

			// Set honorBuildingSettings - need to set here in addition to the UI. 
			// Not setting it here can lead to wrong filtering. 

			bool isPrimitiveTypeValidForBuidingIds = (subLayer.coreOptions.geometryType == VectorPrimitiveType.Polygon) || (subLayer.coreOptions.geometryType == VectorPrimitiveType.Custom);
			bool isSourceValidForBuildingIds = _properties.sourceType != VectorSourceType.MapboxStreets;

			subLayer.honorBuildingIdSetting = isPrimitiveTypeValidForBuidingIds && isSourceValidForBuildingIds;
			// Setup visualizer. 
			((VectorLayerVisualizer)visualizer).SetProperties(subLayer);

			visualizer.Initialize();
			if (visualizer == null)
			{
				return visualizer;
			}

			if (_layerBuilder.ContainsKey(visualizer.Key))
			{
				_layerBuilder[visualizer.Key].Add(visualizer);
			}
			else
			{
				_layerBuilder.Add(visualizer.Key, new List<LayerVisualizerBase> { visualizer });
			}
			return visualizer;
		}

		public virtual void AddPOIVectorLayerVisualizer(PrefabItemOptions poiSubLayer)
		{
			LayerVisualizerBase visualizer = CreateInstance<LocationPrefabsLayerVisualizer>();
			poiSubLayer.performanceOptions = _properties.performanceOptions;
			((LocationPrefabsLayerVisualizer)visualizer).SetProperties((PrefabItemOptions)poiSubLayer);

			visualizer.Initialize();
			if (visualizer == null)
			{
				return;
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

		public override void SetOptions(LayerProperties options)
		{
			_properties = (VectorLayerProperties)options;
			if (_layerBuilder != null)
			{
				_layerBuilder.Clear();
				// TODO : IS THIS CODE REALLY REQUIRED?
				//CreateLayerVisualizers();
				//foreach (var layer in _layerBuilder)
				//{
				//	foreach (var item in layer.Value)
				//	{
				//		(item as VectorLayerVisualizer).SetProperties(null);
				//		item.Initialize();
				//	}
				//}
			}
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (string.IsNullOrEmpty(MapId) || _properties.sourceOptions.isActive == false || (_properties.vectorSubLayers.Count + _properties.locationPrefabList.Count) == 0)
			{
				tile.VectorDataState = TilePropertyState.None;
				return;
			}
			tile.VectorDataState = TilePropertyState.Loading;
			_tilesWaitingResponse.Add(tile);
			VectorDataFetcherParameters parameters = new VectorDataFetcherParameters()
			{
				canonicalTileId = tile.CanonicalTileId,
				mapid = MapId,
				tile = tile,
				useOptimizedStyle = _properties.useOptimizedStyle,
				style = _properties.optimizedStyle
			};
			DataFetcher.FetchData(parameters);
		}

		protected override void UpdateTileFactory(object sender, EventArgs args)
		{
			Debug.Log("TileFactoryHasChanged Override ");
			VectorLayerUpdateArgs layerUpdateArgs = args as VectorLayerUpdateArgs;
			layerUpdateArgs.factory = this;
			base.UpdateTileFactory(sender, layerUpdateArgs);
		}


		public override void UpdateTileProperty(UnityTile tile, LayerUpdateArgs updateArgs)
		{
			updateArgs.property.UpdateProperty(tile);

			if (updateArgs.property.NeedsForceUpdate())
			{
				Unregister(tile);
			}
			Register(tile);
		}

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(UnityTile tile, TileErrorEventArgs e)
		{
			base.OnErrorOccurred(tile, e);
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			if (_layerProgress.ContainsKey(tile))
			{
				_layerProgress.Remove(tile);
			}
			if (_tilesWaitingProcessing.Contains(tile))
			{
				_tilesWaitingProcessing.Remove(tile);
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
		public void RedrawSubLayer(UnityTile tile, LayerVisualizerBase visualizer)
		{
			CreateFeatureWithBuilder(tile, visualizer.SubLayerProperties.coreOptions.layerName, visualizer);
		}
		public void UnregisterLayer(UnityTile tile, LayerVisualizerBase visualizer)
		{
			if (_layerProgress.ContainsKey(tile))
			{
				_layerProgress.Remove(tile);
			}
			if (_tilesWaitingProcessing.Contains(tile))
			{
				_tilesWaitingProcessing.Remove(tile);
			}

			if (visualizer != null)
			{
				//foreach (var layer in _layerBuilder.Values)
				{
					//foreach (var visualizer in layer)
					{
						visualizer.UnregisterTile(tile);
					}
				}
			}
		}

		protected override void OnPostProcess(UnityTile tile)
		{

		}

		#endregion

		#region DataFetcherEvents
		private void OnVectorDataRecieved(UnityTile tile, VectorTile vectorTile)
		{
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);
				if (tile.VectorDataState != TilePropertyState.Unregistered)
				{
					tile.SetVectorData(vectorTile);

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
			}
		}

		private void DataChangedHandler(UnityTile tile)
		{
			if (tile.VectorDataState != TilePropertyState.Unregistered &&
				tile.RasterDataState != TilePropertyState.Loading &&
				tile.HeightDataState != TilePropertyState.Loading)
			{
				CreateMeshes(tile);
			}
		}

		private void OnDataError(UnityTile tile, VectorTile vectorTile, TileErrorEventArgs e)
		{
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);
				if (tile.VectorDataState != TilePropertyState.Unregistered)
				{
					tile.SetVectorData(null);
					tile.VectorDataState = TilePropertyState.Error;
					OnErrorOccurred(e);
				}
			}

		}
		#endregion

		/// <summary>
		/// Fetches the vector data and passes each layer to relevant layer visualizers
		/// </summary>
		/// <param name="tile"></param>
		private void CreateMeshes(UnityTile tile)
		{
			foreach (var layerName in tile.VectorData.Data.LayerNames())
			{
				if (_layerBuilder.ContainsKey(layerName))
				{
					foreach (var builder in _layerBuilder[layerName])
					{
						CreateFeatureWithBuilder(tile, layerName, builder);
					}
				}
			}

			//emptylayer for visualizers that don't depend on outside data sources
			string emptyLayer = "";
			if (_layerBuilder.ContainsKey(emptyLayer))
			{
				foreach (var builder in _layerBuilder[emptyLayer])
				{
					CreateFeatureWithBuilder(tile, emptyLayer, builder);
					//if (builder.Active)
					//{
					//	if (_layerProgress.ContainsKey(tile))
					//	{
					//		_layerProgress[tile].Add(builder);
					//	}
					//	else
					//	{
					//		_layerProgress.Add(tile, new HashSet<LayerVisualizerBase> { builder });
					//		if (!_tilesWaitingProcessing.Contains(tile))
					//		{
					//			_tilesWaitingProcessing.Add(tile);
					//		}
					//	}
					//	//just pass the first available layer - we should create a static null layer for this
					//	builder.Create(tile.VectorData.Data.GetLayer(tile.VectorData.Data.LayerNames()[0]), tile, DecreaseProgressCounter);
					//}
				}
			}

			if (!_layerProgress.ContainsKey(tile))
			{
				tile.VectorDataState = TilePropertyState.Loaded;
			}
		}

		private void CreateFeatureWithBuilder(UnityTile tile, string layerName, LayerVisualizerBase builder)
		{
			if (builder.Active)
			{
				if (_layerProgress.ContainsKey(tile))
				{
					_layerProgress[tile].Add(builder);
				}
				else
				{
					_layerProgress.Add(tile, new HashSet<LayerVisualizerBase> { builder });
					if (!_tilesWaitingProcessing.Contains(tile))
					{
						_tilesWaitingProcessing.Add(tile);
					}
				}
				if (layerName != "")
				{
					builder.Create(tile.VectorData.Data.GetLayer(layerName), tile, DecreaseProgressCounter);
				}
				else
				{
					//just pass the first available layer - we should create a static null layer for this
					builder.Create(tile.VectorData.Data.GetLayer(tile.VectorData.Data.LayerNames()[0]), tile, DecreaseProgressCounter);
				}
			}
		}

		private void DecreaseProgressCounter(UnityTile tile, LayerVisualizerBase builder)
		{
			if (_layerProgress.ContainsKey(tile))
			{
				if (_layerProgress[tile].Contains(builder))
				{
					_layerProgress[tile].Remove(builder);

				}
				if (_layerProgress[tile].Count == 0)
				{
					_layerProgress.Remove(tile);
					_tilesWaitingProcessing.Remove(tile);
					tile.VectorDataState = TilePropertyState.Loaded;
				}
			}
		}
	}
}
