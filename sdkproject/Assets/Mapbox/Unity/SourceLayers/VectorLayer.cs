using Mapbox.Utils;

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
				LayerProperty.AddPoiLayer(prefabItem);
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

			AddLocationPrefabItem(item);
		}

		/// <summary>
		/// Places a prefab at the specified LatLon on the Map.
		/// </summary>
		/// <param name="prefab"> A Game Object Prefab.</param>
		/// <param name="LatLon">A Vector2d(Latitude Longitude) object</param>
		public void SpawnPrefabAtGeoLocation(GameObject prefab,
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
		public void SpawnPrefabAtGeoLocation(GameObject prefab,
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
		public void SpawnPrefabByCategory(GameObject prefab,
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
		public void SpawnPrefabByName(GameObject prefab,
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
	}
}
