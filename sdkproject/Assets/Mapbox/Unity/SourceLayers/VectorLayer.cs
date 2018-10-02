using System.Linq;
using Mapbox.Utils;
using System;
using UnityEngine;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.Map
{
	[Serializable]
	public class VectorLayer : AbstractLayer, IVectorDataLayer
	{
		//Private Fields
		[SerializeField]
		private VectorLayerProperties _layerProperty = new VectorLayerProperties();
		private VectorTileFactory _vectorTileFactory;

		//Events
		public EventHandler SubLayerAdded;
		public EventHandler SubLayerRemoved;

		//Properties
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
		public string LayerSourceId
		{
			get
			{
				return _layerProperty.sourceOptions.Id;
			}
		}
		public VectorTileFactory Factory
		{
			get
			{
				return _vectorTileFactory;
			}
		}

		//Public Methods
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

		public void Update(LayerProperties properties)
		{
			Initialize(properties);
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

		//Private Methods
		private void AddVectorLayer(object sender, EventArgs args)
		{
			VectorLayerUpdateArgs layerUpdateArgs = args as VectorLayerUpdateArgs;
			if (layerUpdateArgs.property is PrefabItemOptions)
			{
				layerUpdateArgs.visualizer =
					_vectorTileFactory.AddPOIVectorLayerVisualizer((PrefabItemOptions)layerUpdateArgs.property);
			}
			else if (layerUpdateArgs.property is VectorSubLayerProperties)
			{
				layerUpdateArgs.visualizer =
					_vectorTileFactory.AddVectorLayerVisualizer((VectorSubLayerProperties)layerUpdateArgs.property);
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

		private void RedrawVectorLayer(object sender, System.EventArgs e)
		{
			NotifyUpdateLayer(_vectorTileFactory, sender as MapboxDataProperty, true);
		}

		#region Api Methods

		/// <summary>
		/// Add provided data source (mapid) to existing ones.
		/// Mapbox vector api supports comma separated mapids and this method
		/// adds the provided mapid at the end of the existing source.
		/// </summary>
		/// <param name="vectorSource">Data source (Mapid) to add to existing sources.</param>
		public virtual void AddLayerSource(string vectorSource)
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

		/// <summary>
		/// Change existing data source (mapid) with provided source.
		/// </summary>
		/// <param name="vectorSource">Data source (Mapid) to use.</param>
		public virtual void SetLayerSource(string vectorSource)
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

		/// <summary>
		/// Change existing data source (mapid) with provided source.
		/// </summary>
		/// <param name="vectorSource">Data source (Mapid) to use.</param>
		public virtual void SetLayerSource(VectorSourceType vectorSource)
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

		public virtual void EnableOptimizedStyle(bool isEnabled)
		{
			if (_layerProperty.useOptimizedStyle != isEnabled)
			{
				_layerProperty.useOptimizedStyle = isEnabled;
				_layerProperty.HasChanged = true;
			}
		}

		/// <summary>
		/// Enable coroutines for vector features, processing choosen amount
		/// of them each frame.
		/// </summary>
		/// <param name="isEnabled">Enable coroutine for vector layer.</param>
		/// <param name="entityPerCoroutine">Numbers of features to process each frame.</param>
		public virtual void EnableCoroutines(bool isEnabled, int entityPerCoroutine = 20)
		{
			if (_layerProperty.performanceOptions.isEnabled != isEnabled ||
				_layerProperty.performanceOptions.entityPerCoroutine != entityPerCoroutine)
			{
				_layerProperty.performanceOptions.isEnabled = isEnabled;
				_layerProperty.performanceOptions.entityPerCoroutine = entityPerCoroutine;
				_layerProperty.performanceOptions.HasChanged = true;
			}
		}

		#endregion

		#region Poi Api Methods

		/// <summary>
		/// Creates the prefab layer.
		/// </summary>
		/// <param name="item"> the options of the prefab layer.</param>
		private void CreatePrefabLayer(PrefabItemOptions item)
		{
			if (LayerProperty.sourceType == VectorSourceType.None
				|| !LayerProperty.sourceOptions.Id.Contains(MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets).Id))
			{
				Debug.LogError("In order to place location prefabs please add \"mapbox.mapbox-streets-v7\" to the list of vector data sources");
				return;
			}

			AddPoiLayer(item);
		}

		/// <summary>
		/// Places a prefab at the specified LatLon on the Map.
		/// </summary>
		/// <param name="prefab"> A Game Object Prefab.</param>
		/// <param name="LatLon">A Vector2d(Latitude Longitude) object</param>
		public virtual void SpawnPrefabAtGeoLocation(GameObject prefab,
											 Vector2d LatLon,
											 Action<List<GameObject>> callback = null,
											 bool scaleDownWithWorld = true,
											 string locationItemName = "New Location")
		{
			var latLonArray = new Vector2d[] { LatLon };
			SpawnPrefabAtGeoLocation(prefab, latLonArray, callback, scaleDownWithWorld, locationItemName);
		}

		/// <summary>
		/// Places a prefab at all locations specified by the LatLon array.
		/// </summary>
		/// <param name="prefab"> A Game Object Prefab.</param>
		/// <param name="LatLon">A Vector2d(Latitude Longitude) object</param>
		public virtual void SpawnPrefabAtGeoLocation(GameObject prefab,
											 Vector2d[] LatLon,
											 Action<List<GameObject>> callback = null,
											 bool scaleDownWithWorld = true,
											 string locationItemName = "New Location")
		{
			var coordinateArray = new string[LatLon.Length];
			for (int i = 0; i < LatLon.Length; i++)
			{
				coordinateArray[i] = LatLon[i].x + ", " + LatLon[i].y;
			}

			PrefabItemOptions item = new PrefabItemOptions()
			{
				findByType = LocationPrefabFindBy.AddressOrLatLon,
				prefabItemName = locationItemName,
				spawnPrefabOptions = new SpawnPrefabOptions()
				{
					prefab = prefab,
					scaleDownWithWorld = scaleDownWithWorld
				},

				coordinates = coordinateArray
			};

			if (callback != null)
			{
				item.OnAllPrefabsInstantiated += callback;
			}

			CreatePrefabLayer(item);
		}

		/// <summary>
		/// Places the prefab for supplied categories.
		/// </summary>
		/// <param name="prefab">GameObject Prefab</param>
		/// <param name="categories"><see cref="LocationPrefabCategories"/> For more than one category separate them by pipe
		/// (eg: LocationPrefabCategories.Food | LocationPrefabCategories.Nightlife)</param>
		/// <param name="density">Density controls the number of POIs on the map.(Integer value between 1 and 30)</param>
		/// <param name="locationItemName">Name of this location prefab item for future reference</param>
		/// <param name="scaleDownWithWorld">Should the prefab scale up/down along with the map game object?</param>
		public virtual void SpawnPrefabByCategory(GameObject prefab,
										  LocationPrefabCategories categories = LocationPrefabCategories.AnyCategory,
										  int density = 30, Action<List<GameObject>> callback = null,
										  bool scaleDownWithWorld = true,
										  string locationItemName = "New Location")
		{
			PrefabItemOptions item = new PrefabItemOptions()
			{
				findByType = LocationPrefabFindBy.MapboxCategory,
				categories = categories,
				density = density,
				prefabItemName = locationItemName,
				spawnPrefabOptions = new SpawnPrefabOptions()
				{
					prefab = prefab,
					scaleDownWithWorld = scaleDownWithWorld
				}
			};

			if (callback != null)
			{
				item.OnAllPrefabsInstantiated += callback;
			}

			CreatePrefabLayer(item);
		}

		/// <summary>
		/// Places the prefab at POI locations if its name contains the supplied string
		/// <param name="prefab">GameObject Prefab</param>
		/// <param name="nameString">This is the string that will be checked against the POI name to see if is contained in it, and ony those POIs will be spawned</param>
		/// <param name="density">Density (Integer value between 1 and 30)</param>
		/// <param name="locationItemName">Name of this location prefab item for future reference</param>
		/// <param name="scaleDownWithWorld">Should the prefab scale up/down along with the map game object?</param>
		/// </summary>
		public virtual void SpawnPrefabByName(GameObject prefab,
									  string nameString,
									  int density = 30,
									  Action<List<GameObject>> callback = null,
									  bool scaleDownWithWorld = true,
									  string locationItemName = "New Location")
		{
			PrefabItemOptions item = new PrefabItemOptions()
			{
				findByType = LocationPrefabFindBy.POIName,
				nameString = nameString,
				density = density,
				prefabItemName = locationItemName,
				spawnPrefabOptions = new SpawnPrefabOptions()
				{
					prefab = prefab,
					scaleDownWithWorld = scaleDownWithWorld
				}
			};

			CreatePrefabLayer(item);
		}



		#endregion

		#region LayerOperations

		// FEATURE LAYER OPERATIONS

		public virtual void AddFeatureLayer(VectorSubLayerProperties subLayerProperties)
		{
			if (_layerProperty.vectorSubLayers == null)
			{
				_layerProperty.vectorSubLayers = new List<VectorSubLayerProperties>();
			}

			_layerProperty.vectorSubLayers.Add(subLayerProperties);
			_layerProperty.OnSubLayerPropertyAdded(new VectorLayerUpdateArgs { property = _layerProperty.vectorSubLayers.Last() });
		}

		public virtual IEnumerable<VectorSubLayerProperties> GetAllFeatureLayers()
		{
			return _layerProperty.vectorSubLayers.AsEnumerable();
		}

		public virtual IEnumerable<VectorSubLayerProperties> GetAllPolygonFeatureLayers()
		{
			foreach (var featureLayer in _layerProperty.vectorSubLayers)
			{
				if (featureLayer.coreOptions.geometryType == VectorPrimitiveType.Polygon)
				{
					yield return featureLayer;
				}
			}
		}

		public virtual IEnumerable<VectorSubLayerProperties> GetAllLineFeatureLayers()
		{
			foreach (var featureLayer in _layerProperty.vectorSubLayers)
			{
				if (featureLayer.coreOptions.geometryType == VectorPrimitiveType.Line)
				{
					yield return featureLayer;
				}
			}
		}

		public virtual IEnumerable<VectorSubLayerProperties> GetAllPointFeatureLayers()
		{
			foreach (var featureLayer in _layerProperty.vectorSubLayers)
			{
				if (featureLayer.coreOptions.geometryType == VectorPrimitiveType.Point)
				{
					yield return featureLayer;
				}
			}
		}

		public virtual IEnumerable<VectorSubLayerProperties> GetFeatureLayerByQuery(Func<VectorSubLayerProperties, bool> query)
		{
			foreach (var featureLayer in _layerProperty.vectorSubLayers)
			{
				if (query(featureLayer))
				{
					yield return featureLayer;
				}
			}
		}

		public virtual VectorSubLayerProperties GetFeatureLayerAtIndex(int i)
		{
			if (i < _layerProperty.vectorSubLayers.Count)
			{
				return _layerProperty.vectorSubLayers[i];
			}
			else
			{
				return null;
			}
		}

		public virtual VectorSubLayerProperties FindFeatureLayerWithName(string featureLayerName)
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

		public virtual void RemoveFeatureLayerWithName(string featureLayerName)
		{
			var layerToRemove = FindFeatureLayerWithName(featureLayerName);
			if (layerToRemove != null)
			{
				//vectorSubLayers.Remove(layerToRemove);
				_layerProperty.OnSubLayerPropertyRemoved(new VectorLayerUpdateArgs { property = layerToRemove });
			}
		}

		public virtual void RemoveFeatureLayer(VectorSubLayerProperties layer)
		{
			_layerProperty.vectorSubLayers.Remove(layer);
			_layerProperty.OnSubLayerPropertyRemoved(new VectorLayerUpdateArgs { property = layer });
		}

		// POI LAYER OPERATIONS

		public virtual void AddPoiLayer(PrefabItemOptions poiLayerProperties)
		{
			if (_layerProperty.locationPrefabList == null)
			{
				_layerProperty.locationPrefabList = new List<PrefabItemOptions>();
			}

			_layerProperty.locationPrefabList.Add(poiLayerProperties);
			_layerProperty.OnSubLayerPropertyAdded(new VectorLayerUpdateArgs { property = _layerProperty.locationPrefabList.Last() });
		}

		public virtual IEnumerable<PrefabItemOptions> GetAllPoiLayers()
		{
			return _layerProperty.locationPrefabList.AsEnumerable();
		}

		public virtual PrefabItemOptions GetPoiLayerAtIndex(int i)
		{
			if (i < _layerProperty.vectorSubLayers.Count)
			{
				return _layerProperty.locationPrefabList[i];
			}
			else
			{
				return null;
			}
		}

		public virtual IEnumerable<PrefabItemOptions> GetPoiLayerByQuery(Func<PrefabItemOptions, bool> query)
		{
			foreach (var poiLayer in _layerProperty.locationPrefabList)
			{
				if (query(poiLayer))
				{
					yield return poiLayer;
				}
			}
		}

		public virtual PrefabItemOptions FindPoiLayerWithName(string poiLayerName)
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

		public virtual void RemovePoiLayerWithName(string poiLayerName)
		{
			var layerToRemove = FindPoiLayerWithName(poiLayerName);
			if (layerToRemove != null)
			{
				//vectorSubLayers.Remove(layerToRemove);
				_layerProperty.OnSubLayerPropertyRemoved(new VectorLayerUpdateArgs { property = layerToRemove });
			}
		}

		public virtual void RemovePoiLayer(PrefabItemOptions layer)
		{
			_layerProperty.locationPrefabList.Remove(layer);
			_layerProperty.OnSubLayerPropertyRemoved(new VectorLayerUpdateArgs { property = layer });
		}

		#endregion
	}
}
