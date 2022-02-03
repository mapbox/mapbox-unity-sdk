using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Enums;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Map;
using Mapbox.Unity.Map;
using System;
using Mapbox.Platform;
using Mapbox.Unity.CustomLayer;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.DataFetching;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	public class VectorTileFactory : AbstractTileFactory
	{
		public VectorFactoryManager VectorFactoryManager;
		private Dictionary<string, List<LayerVisualizerBase>> _layerBuilder;
		private VectorLayerProperties _properties;
		public VectorLayerProperties Properties => _properties;
		private Dictionary<UnityTile, HashSet<LayerVisualizerBase>> _layerProgress;

		public VectorTileFactory(VectorLayerProperties properties)
		{
			_properties = properties;
			_layerProgress = new Dictionary<UnityTile, HashSet<LayerVisualizerBase>>();
			_layerBuilder = new Dictionary<string, List<LayerVisualizerBase>>();

			VectorFactoryManager = new VectorFactoryManager(_properties);
			VectorFactoryManager.DataReceived += OnFetcherDataReceived;
			VectorFactoryManager.FetchingError += OnFetchingError;

			CreateLayerVisualizers();
		}

		public override void SetOptions(LayerProperties options)
		{
			_properties = (VectorLayerProperties)options;
			if (_layerBuilder != null)
			{
				RemoveAllLayerVisualiers();

				CreateLayerVisualizers();
			}
		}

		public override void Clear()
		{
			if (_layerBuilder != null)
			{
				foreach (var layerList in _layerBuilder.Values)
				{
					foreach (var layerVisualizerBase in layerList)
					{
						layerVisualizerBase.Clear();
						GameObject.DestroyImmediate(layerVisualizerBase);
					}
				}

				_layerProgress.Clear();
				_tilesWaitingResponse.Clear();
				_tilesWaitingProcessing.Clear();
			}
		}

		protected override void OnRegistered(UnityTile tile)
		{
			_tilesWaitingResponse.Add(tile);
			VectorFactoryManager.RegisterTile(tile);
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			VectorFactoryManager.UnregisterTile(tile);
			if (_layerProgress != null && _layerProgress.ContainsKey(tile))
			{
				_layerProgress.Remove(tile);
			}
			if (_tilesWaitingResponse != null && _tilesWaitingProcessing.Contains(tile))
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

		protected override void OnStopped(UnityTile tile)
		{
			VectorFactoryManager.StopTile(tile);
		}

		protected override void OnClearTile(UnityTile tile)
		{
			//tile.SetVectorData(null);
			if (_layerBuilder != null)
			{
				foreach (var layer in _layerBuilder.Values)
				{
					foreach (var visualizer in layer)
					{
						visualizer.ClearTile(tile);
					}
				}
			}
		}

		private void CreateMeshes(UnityTile tile, Action callback)
		{
			foreach (var layerVisualizerTuple in _layerBuilder)
			{
				foreach (var visualizer in layerVisualizerTuple.Value)
				{
					CreateFeatureWithBuilder(tile, visualizer, callback);
				}
			}
		}

		private void OnFetcherDataReceived(UnityTile tile, Mapbox.Map.VectorTile vectorTile)
		{
			if (vectorTile.CurrentTileState != TileState.Canceled &&
			    tile.ContainsDataTile(vectorTile) &&
			    _tilesWaitingResponse.Contains(tile))
			{
				tile.SetVectorData(vectorTile, CreateMeshes);
			}
		}

		private void OnFetchingError(UnityTile tile, Mapbox.Map.VectorTile vectorTile, TileErrorEventArgs e)
		{
			if (tile != null)
			{
				tile.Logs.Add("vector OnFetchingError");
				_tilesWaitingResponse.Remove(tile);
				OnErrorOccurred(e);
			}
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

		protected override void UpdateTileFactory(object sender, EventArgs args)
		{
			var layerUpdateArgs = args as VectorLayerUpdateArgs;
			layerUpdateArgs.factory = this;
			base.UpdateTileFactory(sender, layerUpdateArgs);
		}

		public void RedrawSubLayer(UnityTile tile, LayerVisualizerBase visualizer)
		{
			CreateFeatureWithBuilder(tile, visualizer);
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
				visualizer.UnregisterTile(tile);
			}
		}

		protected override void OnUnbindEvents()
		{
			if (_layerBuilder != null)
			{
				foreach (var layer in _layerBuilder.Values)
				{
					foreach (var visualizer in layer)
					{
						visualizer.LayerVisualizerHasChanged -= UpdateTileFactory;
						visualizer.UnbindSubLayerEvents();
					}
				}
			}
		}

		#region Private Methods
		private void TrackFeatureWithBuilder(UnityTile tile, LayerVisualizerBase builder)
		{
			if (builder.Active)
			{
				if (_layerProgress.ContainsKey(tile))
				{
					_layerProgress[tile].Add(builder);
				}
				else
				{
					_layerProgress.Add(tile, new HashSet<LayerVisualizerBase> {builder});
					if (!_tilesWaitingProcessing.Contains(tile))
					{
						_tilesWaitingProcessing.Add(tile);
					}
				}
			}
		}

		private void CreateFeatureWithBuilder(UnityTile tile, LayerVisualizerBase builder, Action callback = null)
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
				builder.Create(tile, (t, b) =>
				{
					LayerFinishedCallback(t, b);
					if (callback != null)
					{
						callback();
					}
				});
			}
		}

		private void LayerFinishedCallback(UnityTile tile, LayerVisualizerBase builder)
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
				}
			}
		}

		private void CreateLayerVisualizers()
		{
			foreach (var sublayer in _properties.vectorSubLayers)
			{
				AddVectorLayerVisualizer(sublayer);
			}
		}

		private void RemoveAllLayerVisualiers()
		{
			//Clearing gameobjects pooled and managed by modifiers to prevent zombie gameobjects.
			foreach (var pairs in _layerBuilder)
			{
				foreach (var layerVisualizerBase in pairs.Value)
				{
					layerVisualizerBase.Clear();
				}
			}
			_layerBuilder.Clear();
		}
		#endregion

		#region Public Layer Operation Api Methods for
		public virtual LayerVisualizerBase AddVectorLayerVisualizer(VectorSubLayerProperties subLayer)
		{
			//if its of type prefabitemoptions then separate the visualizer type
			LayerVisualizerBase visualizer = ScriptableObject.CreateInstance<VectorLayerVisualizer>();

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

		public virtual LayerVisualizerBase FindVectorLayerVisualizer(VectorSubLayerProperties subLayer)
		{
			if (_layerBuilder.ContainsKey(subLayer.Key))
			{
				var visualizer = _layerBuilder[subLayer.Key].Find((obj) => obj.SubLayerProperties == subLayer);
				return visualizer;
			}
			return null;
		}

		public virtual void RemoveVectorLayerVisualizer(LayerVisualizerBase subLayer)
		{
			subLayer.Clear();
			if (_layerBuilder.ContainsKey(subLayer.Key))
			{
				if (Properties.vectorSubLayers.Contains(subLayer.SubLayerProperties))
				{
					Properties.vectorSubLayers.Remove(subLayer.SubLayerProperties);
				}
				else if (subLayer.SubLayerProperties is PrefabItemOptions && Properties.locationPrefabList.Contains(subLayer.SubLayerProperties as PrefabItemOptions))
				{
					Properties.locationPrefabList.Remove(subLayer.SubLayerProperties as PrefabItemOptions);
				}
				subLayer.LayerVisualizerHasChanged -= UpdateTileFactory;
				subLayer.UnbindSubLayerEvents();
				_layerBuilder[subLayer.Key].Remove(subLayer);
			}
		}
		#endregion

		public List<LayerVisualizerBase> GetVisualizersOfLayerType(string sublayerTypeName)
		{
			if (_layerBuilder.ContainsKey(sublayerTypeName))
			{
				return _layerBuilder[sublayerTypeName];
			}
			else
			{
				return null;
			}
		}
	}
}
