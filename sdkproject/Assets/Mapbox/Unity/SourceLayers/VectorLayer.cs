using System.Linq;

namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;

	[Serializable]
	public class VectorLayer : AbstractLayer, IVectorDataLayer
	{
		#region Events
		public EventHandler SubLayerAdded;
		public EventHandler SubLayerRemoved;
		#endregion


		[SerializeField]
		VectorLayerProperties _layerProperty = new VectorLayerProperties();

		[NodeEditorElement(" Vector Layer ")]
		public VectorLayerProperties LayerProperty
		{
			get
			{
				return _layerProperty;
			}
		}

		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Vector;
			}
		}

		public bool IsLayerActive
		{
			get
			{
				return (_layerProperty.sourceType != VectorSourceType.None);
			}
		}

		public string LayerSource
		{
			get
			{
				return _layerProperty.sourceOptions.Id;
			}
		}

		public void SetLayerSource(VectorSourceType vectorSource)
		{
			if (vectorSource != VectorSourceType.Custom && vectorSource != VectorSourceType.None)
			{
				_layerProperty.sourceType = vectorSource;
				_layerProperty.sourceOptions.layerSource = MapboxDefaultVector.GetParameters(vectorSource);
			}
			else
			{
				Debug.LogWarning("Invalid style - trying to set " + vectorSource.ToString() + " as default style!");
			}
		}

		public void SetLayerSource(string vectorSource)
		{
			if (!string.IsNullOrEmpty(vectorSource))
			{
				_layerProperty.sourceType = VectorSourceType.Custom;
				_layerProperty.sourceOptions.Id = vectorSource;
			}
			else
			{
				_layerProperty.sourceType = VectorSourceType.None;
				Debug.LogWarning("Empty source - turning off vector data. ");
			}
		}

		public void AddLayerSource(string vectorSource)
		{
			if (!string.IsNullOrEmpty(vectorSource))
			{
				if (!_layerProperty.sourceOptions.Id.Contains(vectorSource))
				{
					if (string.IsNullOrEmpty(_layerProperty.sourceOptions.Id))
					{
						SetLayerSource(vectorSource);
						return;
					}
					var newLayerSource = _layerProperty.sourceOptions.Id + "," + vectorSource;
					SetLayerSource(newLayerSource);
				}
			}
			else
			{
				Debug.LogError("Empty source. Nothing was added to the list of data sources");
			}
		}

		private void AddVectorLayer(object sender, EventArgs args)
		{
			VectorLayerUpdateArgs layerUpdateArgs = args as VectorLayerUpdateArgs;
			if (layerUpdateArgs.property is PrefabItemOptions)
			{
				layerUpdateArgs.visualizer =
					_vectorTileFactory.AddPOIVectorLayerVisualizer((PrefabItemOptions) layerUpdateArgs.property);
			}
			else if (layerUpdateArgs.property is VectorSubLayerProperties)
			{
				layerUpdateArgs.visualizer =
					_vectorTileFactory.AddVectorLayerVisualizer((VectorSubLayerProperties) layerUpdateArgs.property);
			}

			layerUpdateArgs.factory = _vectorTileFactory;

			SubLayerAdded(this, layerUpdateArgs);
		}

		private void RemoveVectorLayer(object sender, EventArgs args)
		{
			VectorLayerUpdateArgs layerUpdateArgs = args as VectorLayerUpdateArgs;

			layerUpdateArgs.visualizer = _vectorTileFactory.FindVectorLayerVisualizer((VectorSubLayerProperties)layerUpdateArgs.property);
			layerUpdateArgs.factory = _vectorTileFactory;

			SubLayerRemoved(this, layerUpdateArgs);
		}
		public void AddLocationPrefabItem(PrefabItemOptions prefabItem)
		{
			//ensure that there is a list of prefabitems
			if (PointsOfInterestSublayerList == null)
			{
				PointsOfInterestSublayerList = new List<PrefabItemOptions>();
			}

			//add the prefab item if it doesn't already exist
			if (!PointsOfInterestSublayerList.Contains(prefabItem))
			{
				PointsOfInterestSublayerList.Add(prefabItem);
			}
		}

		public void RemoveVectorLayer(int index)
		{
			if (_layerProperty.vectorSubLayers != null)
			{
				_layerProperty.vectorSubLayers.RemoveAt(index);
			}
		}

		public void RemovePrefabItem(int index)
		{
			if (PointsOfInterestSublayerList != null)
			{
				PointsOfInterestSublayerList.RemoveAt(index);
			}
		}

		public void Initialize(LayerProperties properties)
		{
			_layerProperty = (VectorLayerProperties)properties;
			Initialize();
		}

		public void Initialize()
		{
			_vectorTileFactory = ScriptableObject.CreateInstance<VectorTileFactory>();
			UpdateFactorySettings();

			_layerProperty.PropertyHasChanged += RedrawVectorLayer;
			_layerProperty.SubLayerPropertyAdded += AddVectorLayer;
			_layerProperty.SubLayerPropertyRemoved += RemoveVectorLayer;
			_vectorTileFactory.TileFactoryHasChanged += (sender, args) =>
			{
				Debug.Log("VectorLayer Delegate");
				NotifyUpdateLayer(args as LayerUpdateArgs);
			};
		}

		public void UpdateFactorySettings()
		{
			_vectorTileFactory.SetOptions(_layerProperty);
		}

		public void Remove()
		{
			_layerProperty = new VectorLayerProperties
			{
				sourceType = VectorSourceType.None
			};
		}

		public void Update(LayerProperties properties)
		{
			Initialize(properties);
		}

		private void RedrawVectorLayer(object sender, System.EventArgs e)
		{
			NotifyUpdateLayer(_vectorTileFactory, sender as MapboxDataProperty, true);
		}

		public VectorTileFactory Factory
		{
			get
			{
				return _vectorTileFactory;
			}
		}
		private VectorTileFactory _vectorTileFactory;

		public List<PrefabItemOptions> PointsOfInterestSublayerList
		{
			get
			{
				return _layerProperty.locationPrefabList;
			}
			set
			{
				_layerProperty.locationPrefabList = value;
			}
		}


		#region LayerOperations

		// FEATURE LAYER OPERATIONS

		public void AddFeatureLayer(VectorSubLayerProperties subLayerProperties)
		{
			if (_layerProperty.vectorSubLayers == null)
			{
				_layerProperty.vectorSubLayers = new List<VectorSubLayerProperties>();
			}
			_layerProperty.vectorSubLayers.Add(subLayerProperties);
			_layerProperty.OnSubLayerPropertyAdded(new VectorLayerUpdateArgs { property = _layerProperty.vectorSubLayers.Last() });
		}

		public VectorSubLayerProperties FindFeatureLayerWithName(string featureLayerName)
		{
			int foundLayerIndex = -1;
			// Optimize for performance.
			for (int i = 0; i < _layerProperty.vectorSubLayers.Count; i++)
			{
				if (_layerProperty.vectorSubLayers[i].SubLayerNameMatchesExact(featureLayerName))
				{
					foundLayerIndex = i;
					break;
				}
			}
			return (foundLayerIndex != -1) ? _layerProperty.vectorSubLayers[foundLayerIndex] : null;
		}

		public void RemoveFeatureLayerWithName(string featureLayerName)
		{
			var layerToRemove = FindFeatureLayerWithName(featureLayerName);
			if (layerToRemove != null)
			{
				//vectorSubLayers.Remove(layerToRemove);
				_layerProperty.OnSubLayerPropertyRemoved(new VectorLayerUpdateArgs { property = layerToRemove });
			}
		}

		
		// POI LAYER OPERATIONS

		public void AddPoiLayer(PrefabItemOptions poiLayerProperties)
		{
			if (_layerProperty.locationPrefabList == null)
			{
				_layerProperty.locationPrefabList = new List<PrefabItemOptions>();
			}
			_layerProperty.locationPrefabList.Add(poiLayerProperties);
			_layerProperty.OnSubLayerPropertyAdded(new VectorLayerUpdateArgs { property = _layerProperty.locationPrefabList.Last() });
		}

		public PrefabItemOptions FindPoiLayerWithName(string poiLayerName)
		{
			int foundLayerIndex = -1;
			// Optimize for performance.
			for (int i = 0; i < _layerProperty.locationPrefabList.Count; i++)
			{
				if (_layerProperty.locationPrefabList[i].SubLayerNameMatchesExact(poiLayerName))
				{
					foundLayerIndex = i;
					break;
				}
			}
			return (foundLayerIndex != -1) ? _layerProperty.locationPrefabList[foundLayerIndex] : null;
		}

		public void RemovePoiLayerWithName(string poiLayerName)
		{
			var layerToRemove = FindPoiLayerWithName(poiLayerName);
			if (layerToRemove != null)
			{
				//vectorSubLayers.Remove(layerToRemove);
				_layerProperty.OnSubLayerPropertyRemoved(new VectorLayerUpdateArgs { property = layerToRemove });
			}
		}

		#endregion
	}
}
