namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Map;
	using Mapbox.Unity.Map;

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
		//[SerializeField]
		//private string _mapId = "mapbox.mapbox-streets-v7";

		//[NodeEditorElementAttribute("Layer Visalizers")]
		//public List<LayerVisualizerBase> Visualizers;

		private Dictionary<string, List<LayerVisualizerBase>> _layerBuilder;
		private Dictionary<UnityTile, VectorTile> _cachedData = new Dictionary<UnityTile, VectorTile>();

		VectorLayerProperties _properties;
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
			_properties = (VectorLayerProperties)options;
		}
		/// <summary>
		/// Set up sublayers using VectorLayerVisualizers.
		/// </summary>
		internal override void OnInitialized()
		{
			_layerBuilder = new Dictionary<string, List<LayerVisualizerBase>>();
			_cachedData.Clear();

			foreach (var sublayer in _properties.vectorSubLayers)
			{
				var visualizer = CreateInstance<VectorLayerVisualizer>();
				visualizer.SetProperties(sublayer, _properties.performanceOptions);

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

		internal override void OnRegistered(UnityTile tile)
		{
			var vectorTile = (_properties.useOptimizedStyle) ? new VectorTile(_properties.optimizedStyle.Id, _properties.optimizedStyle.Modified) : new VectorTile();
			tile.AddTile(vectorTile);

			if (string.IsNullOrEmpty(MapId) || _properties.sourceOptions.isActive == false || _properties.vectorSubLayers.Count == 0)
			{
				// Do nothing; 
				Progress++;
				Progress--;
			}
			else
			{
				vectorTile.Initialize(_fileSource, tile.CanonicalTileId, MapId, () =>
				{
					if (tile == null)
					{
						Progress++;
						Progress--;
						return;
					}

					if (vectorTile.HasError)
					{
						OnErrorOccurred(new TileErrorEventArgs(tile.CanonicalTileId, vectorTile.GetType(), tile, vectorTile.Exceptions));
						tile.VectorDataState = TilePropertyState.Error;
						Progress++;
						Progress--;
						return;
					}

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
				});
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

		internal override void OnUnregistered(UnityTile tile)
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

			tile.VectorDataState = TilePropertyState.Loaded;
			_cachedData.Remove(tile);
		}

		private void DecreaseProgressCounter()
		{
			Progress--;
		}
	}
}
