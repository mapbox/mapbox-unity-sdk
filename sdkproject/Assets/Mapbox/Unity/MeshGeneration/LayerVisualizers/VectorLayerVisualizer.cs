using Mapbox.VectorTile.Geometry;

namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.VectorTile;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Map;

	public class VectorLayerVisualizerProperties
	{
		public FeatureProcessingStage featureProcessingStage;
		public bool buildingsWithUniqueIds = false;
		public VectorTileLayer vectorTileLayer;
		public ILayerFeatureFilterComparer[] layerFeatureFilters;
		public ILayerFeatureFilterComparer layerFeatureFilterCombiner;
	}


	public class VectorLayerVisualizer : LayerVisualizerBase
	{
		VectorSubLayerProperties _layerProperties;
		public override VectorSubLayerProperties SubLayerProperties
		{
			get
			{
				return _layerProperties;
			}
			set
			{
				_layerProperties = value;
			}
		}

		public ModifierStackBase DefaultModifierStack
		{
			get
			{
				return _defaultStack;
			}
			set
			{
				_defaultStack = value;
			}
		}

		//public event System.EventHandler VectorHasChanged;

		protected LayerPerformanceOptions _performanceOptions;
		protected Dictionary<UnityTile, List<int>> _activeCoroutines;
		int _entityInCurrentCoroutine = 0;

		protected ModifierStackBase _defaultStack;
		private HashSet<ulong> _activeIds;
		private Dictionary<UnityTile, List<ulong>> _idPool; //necessary to keep _activeIds list up to date when unloading tiles
		private string _key;

		public override string Key
		{
			get { return _layerProperties.coreOptions.layerName; }
			set { _layerProperties.coreOptions.layerName = value; }
		}

		public T FindMeshModifier<T>() where T : MeshModifier
		{
			MeshModifier mod = _defaultStack.MeshModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			return (T)mod;
		}

		public T FindGameObjectModifier<T>() where T : GameObjectModifier
		{
			GameObjectModifier mod = _defaultStack.GoModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			return (T)mod;
		}

		public T AddOrCreateMeshModifier<T>() where T : MeshModifier
		{
			MeshModifier mod = _defaultStack.MeshModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			if (mod == null)
			{
				mod = (MeshModifier)CreateInstance(typeof(T));
				_defaultStack.MeshModifiers.Add(mod);
			}
			return (T)mod;
		}

		public T AddOrCreateGameObjectModifier<T>() where T : GameObjectModifier
		{
			GameObjectModifier mod = _defaultStack.GoModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			if (mod == null)
			{
				mod = (GameObjectModifier)CreateInstance(typeof(T));
				_defaultStack.GoModifiers.Add(mod);
			}
			return (T)mod;
		}

		private void UpdateVector(object sender, System.EventArgs eventArgs)
		{
			VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

			layerUpdateArgs.visualizer = this;
			layerUpdateArgs.effectsVectorLayer = true;

			if (layerUpdateArgs.modifier != null)
			{
				layerUpdateArgs.property.PropertyHasChanged -= layerUpdateArgs.modifier.UpdateModifier;
				layerUpdateArgs.modifier.ModifierHasChanged -= UpdateVector;
			}
			else if (layerUpdateArgs.property != null)
			{
				layerUpdateArgs.property.PropertyHasChanged -= UpdateVector;
			}
			UnbindSubLayerEvents();

			OnUpdateLayerVisualizer(layerUpdateArgs);
		}

		public override void UnbindSubLayerEvents()
		{
			foreach (var modifier in _defaultStack.MeshModifiers)
			{
				modifier.UnbindProperties();
				modifier.ModifierHasChanged -= UpdateVector;
			}
			foreach (var modifier in _defaultStack.GoModifiers)
			{
				modifier.UnbindProperties();
				modifier.ModifierHasChanged -= UpdateVector;
			}

			_layerProperties.extrusionOptions.PropertyHasChanged -= UpdateVector;
			_layerProperties.coreOptions.PropertyHasChanged -= UpdateVector;
			_layerProperties.filterOptions.PropertyHasChanged -= UpdateVector;
			_layerProperties.filterOptions.UnRegisterFilters();
			_layerProperties.materialOptions.PropertyHasChanged -= UpdateVector;

			_layerProperties.PropertyHasChanged -= UpdateVector;
		}

		public override void SetProperties(VectorSubLayerProperties properties)
		{
			if (_layerProperties == null && properties != null)
			{
				_layerProperties = properties;
				if (_performanceOptions == null && properties.performanceOptions != null)
				{
					_performanceOptions = properties.performanceOptions;
				}
			}

			if (_layerProperties.coreOptions.combineMeshes)
			{
				if (_defaultStack == null || !(_defaultStack is MergedModifierStack))
				{
					_defaultStack = ScriptableObject.CreateInstance<MergedModifierStack>();
				}
				else
				{
					// HACK - to clean out the Modifiers.
					// Will this trigger GC that we could avoid ??
					_defaultStack.MeshModifiers.Clear();
					_defaultStack.GoModifiers.Clear();
				}
			}
			else
			{
				if (_defaultStack == null || !(_defaultStack is ModifierStack))
				{
					_defaultStack = ScriptableObject.CreateInstance<ModifierStack>();
					((ModifierStack)_defaultStack).moveFeaturePositionTo = _layerProperties.moveFeaturePositionTo;
				}
				else
				{
					// HACK - to clean out the Modifiers.
					// Will this trigger GC that we could avoid ??
					_defaultStack.MeshModifiers.Clear();
					_defaultStack.GoModifiers.Clear();
				}
			}

			//Add any additional modifiers that were added.
			if (_defaultStack.MeshModifiers == null)
			{
				_defaultStack.MeshModifiers = new List<MeshModifier>();
			}
			if (_defaultStack.GoModifiers == null)
			{
				_defaultStack.GoModifiers = new List<GameObjectModifier>();
			}

			// Setup material options.
			_layerProperties.materialOptions.SetDefaultMaterialOptions();

			switch (_layerProperties.coreOptions.geometryType)
			{
				case VectorPrimitiveType.Point:
				case VectorPrimitiveType.Custom:
					{
						// Let the user add anything that they want
						if (_layerProperties.coreOptions.snapToTerrain == true)
						{
							//defaultMeshModifierStack.Add(CreateInstance<SnapTerrainModifier>());
							AddOrCreateMeshModifier<SnapTerrainModifier>();
						}

						break;
					}
				case VectorPrimitiveType.Line:
					{
						if (_layerProperties.coreOptions.snapToTerrain == true)
						{
							AddOrCreateMeshModifier<SnapTerrainModifier>();
						}

						var lineMeshMod = AddOrCreateMeshModifier<LineMeshModifier>();
						lineMeshMod.SetProperties(_layerProperties.lineGeometryOptions);
						lineMeshMod.ModifierHasChanged += UpdateVector;

						if (_layerProperties.extrusionOptions.extrusionType != Map.ExtrusionType.None)
						{
							var heightMod = AddOrCreateMeshModifier<HeightModifier>();
							heightMod.SetProperties(_layerProperties.extrusionOptions);
							heightMod.ModifierHasChanged += UpdateVector;
						}
						else
						{
							_layerProperties.extrusionOptions.PropertyHasChanged += UpdateVector;
						}

						//collider modifier options
						var lineColliderMod = AddOrCreateGameObjectModifier<ColliderModifier>();
						lineColliderMod.SetProperties(_layerProperties.colliderOptions);
						lineColliderMod.ModifierHasChanged += UpdateVector;

						var lineStyleMod = AddOrCreateGameObjectModifier<MaterialModifier>();
						lineStyleMod.SetProperties(_layerProperties.materialOptions);
						lineStyleMod.ModifierHasChanged += UpdateVector;

						break;
					}
				case VectorPrimitiveType.Polygon:
					{
						if (_layerProperties.coreOptions.snapToTerrain == true)
						{
							AddOrCreateMeshModifier<SnapTerrainModifier>();
						}

						AddOrCreateMeshModifier<PolygonMeshModifier>();

						UVModifierOptions uvModOptions = new UVModifierOptions();
						uvModOptions.texturingType = (_layerProperties.materialOptions.style == StyleTypes.Custom) ? _layerProperties.materialOptions.customStyleOptions.texturingType : _layerProperties.materialOptions.texturingType;
						uvModOptions.atlasInfo = (_layerProperties.materialOptions.style == StyleTypes.Custom) ? _layerProperties.materialOptions.customStyleOptions.atlasInfo : _layerProperties.materialOptions.atlasInfo;
						uvModOptions.style = _layerProperties.materialOptions.style;

						var uvMod = AddOrCreateMeshModifier<UvModifier>();
						uvMod.SetProperties(uvModOptions);

						if (_layerProperties.extrusionOptions.extrusionType != Map.ExtrusionType.None)
						{
							//replace materialOptions with styleOptions
							bool useTextureSideWallModifier =
							(_layerProperties.materialOptions.style == StyleTypes.Custom) ?
								(_layerProperties.materialOptions.customStyleOptions.texturingType == UvMapType.Atlas || _layerProperties.materialOptions.customStyleOptions.texturingType == UvMapType.AtlasWithColorPalette)
								: (_layerProperties.materialOptions.texturingType == UvMapType.Atlas || _layerProperties.materialOptions.texturingType == UvMapType.AtlasWithColorPalette);

							if (useTextureSideWallModifier)
							{
								var atlasMod = AddOrCreateMeshModifier<TextureSideWallModifier>();
								GeometryExtrusionWithAtlasOptions atlasOptions = new GeometryExtrusionWithAtlasOptions(_layerProperties.extrusionOptions, uvModOptions);
								atlasMod.SetProperties(atlasOptions);
								_layerProperties.extrusionOptions.PropertyHasChanged += UpdateVector;
							}
							else
							{
								var heightMod = AddOrCreateMeshModifier<HeightModifier>();
								heightMod.SetProperties(_layerProperties.extrusionOptions);
								heightMod.ModifierHasChanged += UpdateVector;
							}
						}
						else
						{
							_layerProperties.extrusionOptions.PropertyHasChanged += UpdateVector;
						}

						//collider modifier options
						var polyColliderMod = AddOrCreateGameObjectModifier<ColliderModifier>();
						polyColliderMod.SetProperties(_layerProperties.colliderOptions);
						polyColliderMod.ModifierHasChanged += UpdateVector;

						var styleMod = AddOrCreateGameObjectModifier<MaterialModifier>();
						styleMod.SetProperties(_layerProperties.materialOptions);
						styleMod.ModifierHasChanged += UpdateVector;


						bool isCustomStyle = (_layerProperties.materialOptions.style == StyleTypes.Custom);
						if ((isCustomStyle) ? (_layerProperties.materialOptions.customStyleOptions.texturingType == UvMapType.AtlasWithColorPalette)
							: (_layerProperties.materialOptions.texturingType == UvMapType.AtlasWithColorPalette))
						{
							var colorPaletteMod = AddOrCreateGameObjectModifier<MapboxStylesColorModifier>();
							colorPaletteMod.m_scriptablePalette = (isCustomStyle) ? _layerProperties.materialOptions.customStyleOptions.colorPalette : _layerProperties.materialOptions.colorPalette;
							_layerProperties.materialOptions.PropertyHasChanged += UpdateVector;
							//TODO: Add SetProperties Method to MapboxStylesColorModifier
						}

						break;
					}
				default:
					break;
			}

			_layerProperties.coreOptions.PropertyHasChanged += UpdateVector;
			_layerProperties.filterOptions.PropertyHasChanged += UpdateVector;

			_layerProperties.filterOptions.RegisterFilters();
			if (_layerProperties.MeshModifiers != null)
			{
				_defaultStack.MeshModifiers.AddRange(_layerProperties.MeshModifiers);
			}
			if (_layerProperties.GoModifiers != null)
			{
				_defaultStack.GoModifiers.AddRange(_layerProperties.GoModifiers);
			}
			//Adding filters from the types dropdown

			//if ((MapboxSpecialLayerParameters.LayerNameTypeProperty.ContainsKey(properties.coreOptions.layerName)) && !string.IsNullOrEmpty(properties.selectedTypes))
			//{
			//	LayerFilter filter = new LayerFilter(LayerFilterOperationType.Contains);

			//	filter.Key = MapboxSpecialLayerParameters.LayerNameTypeProperty[properties.coreOptions.layerName];
			//	filter.PropertyValue = properties.selectedTypes;

			//	//if (properties.coreOptions.layerName == properties.roadLayer)
			//	//{
			//	//	filter.Key = properties.roadLayer_TypeProperty;
			//	//	filter.PropertyValue = properties.selectedTypes;
			//	//}
			//	//else if (properties.coreOptions.layerName == "landuse")
			//	//{
			//	//	filter.Key = properties.landuseLayer_TypeProperty;
			//	//	filter.PropertyValue = properties.selectedTypes;
			//	//}
			//	properties.filterOptions.filters.Add(filter);
			//}

			_layerProperties.PropertyHasChanged += UpdateVector;
		}

		/// <summary>
		/// Add the replacement criteria to any mesh modifiers implementing IReplaceable
		/// </summary>
		/// <param name="criteria">Criteria.</param>
		protected void SetReplacementCriteria(IReplacementCriteria criteria)
		{
			foreach (var meshMod in _defaultStack.MeshModifiers)
			{
				if (meshMod is IReplaceable)
				{
					((IReplaceable)meshMod).Criteria.Add(criteria);
				}
			}
		}

		#region Private Helper Methods
		/// <summary>
		/// Convenience function to add feature to Tile object pool.
		/// </summary>
		/// <param name="feature">Feature to be added to the pool.</param>
		/// <param name="tile">Tile currently being processed.</param>
		private void AddFeatureToTileObjectPool(VectorFeatureUnity feature, UnityTile tile)
		{
			_activeIds.Add(feature.Data.Id);
			if (!_idPool.ContainsKey(tile))
			{
				_idPool.Add(tile, new List<ulong>());
			}
			else
			{
				_idPool[tile].Add(feature.Data.Id);
			}
		}

		/// <summary>
		/// Apply filters to the layer and check if the current feature is eleigible for rendering.
		/// </summary>
		/// <returns><c>true</c>, if feature eligible after filtering was applied, <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		private bool IsFeatureEligibleAfterFiltering(VectorFeatureUnity feature, UnityTile tile, VectorLayerVisualizerProperties layerProperties)
		{
			if (layerProperties.layerFeatureFilters.Count() == 0)
			{
				return true;
			}
			else
			{
				// build features only if the filter returns true.
				if (layerProperties.layerFeatureFilterCombiner.Try(feature))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Function to fetch feature in vector tile at the index specified.
		/// </summary>
		/// <returns>The feature in tile at the index requested.</returns>
		/// <param name="tile">Unity Tile containing the feature.</param>
		/// <param name="index">Index of the vector feature being requested.</param>
		private VectorFeatureUnity GetFeatureinTileAtIndex(int index, UnityTile tile, VectorLayerVisualizerProperties layerProperties)
		{
			return new VectorFeatureUnity(layerProperties.vectorTileLayer.GetFeature(index),
													 tile,
										  layerProperties.vectorTileLayer.Extent,
										  layerProperties.buildingsWithUniqueIds);
		}

		/// <summary>
		/// Function to check if the feature is already in the active Id pool, features already in active Id pool should be skipped from processing.
		/// </summary>
		/// <returns><c>true</c>, if feature is already in activeId pool or if the layer has buildingsWithUniqueId flag set to <see langword="true"/>, <c>false</c> otherwise.</returns>
		/// <param name="featureId">Feature identifier.</param>
		private bool ShouldSkipProcessingFeatureWithId(ulong featureId, UnityTile tile, VectorLayerVisualizerProperties layerProperties)
		{
			return (layerProperties.buildingsWithUniqueIds && _activeIds.Contains(featureId));
		}

		/// <summary>
		/// Gets a value indicating whether this entity per coroutine bucket is full.
		/// </summary>
		/// <value><c>true</c> if coroutine bucket is full; otherwise, <c>false</c>.</value>
		private bool IsCoroutineBucketFull
		{
			get
			{
				return (_performanceOptions != null && _performanceOptions.isEnabled && _entityInCurrentCoroutine >= _performanceOptions.entityPerCoroutine);
			}
		}

		public override bool Active
		{
			get
			{
				return _layerProperties.coreOptions.isActive;
			}
		}

		#endregion
		public override void Initialize()
		{
			base.Initialize();
			_entityInCurrentCoroutine = 0;
			_activeCoroutines = new Dictionary<UnityTile, List<int>>();
			_activeIds = new HashSet<ulong>();
			_idPool = new Dictionary<UnityTile, List<ulong>>();

			if (_defaultStack != null)
			{
				_defaultStack.Initialize();
			}
		}

		public override void InitializeStack()
		{
			if (_defaultStack != null)
			{
				_defaultStack.Initialize();
			}
		}


		public override void Create(VectorTileLayer layer, UnityTile tile, Action<UnityTile, LayerVisualizerBase> callback)
		{
			if (!_activeCoroutines.ContainsKey(tile))
				_activeCoroutines.Add(tile, new List<int>());
			_activeCoroutines[tile].Add(Runnable.Run(ProcessLayer(layer, tile, tile.UnwrappedTileId, callback)));
		}

		protected IEnumerator ProcessLayer(VectorTileLayer layer, UnityTile tile, UnwrappedTileId tileId, Action<UnityTile, LayerVisualizerBase> callback = null)
		{
			//HACK to prevent request finishing on same frame which breaks modules started/finished events
			yield return null;

			if (tile == null)
			{
				yield break;
			}

			VectorLayerVisualizerProperties tempLayerProperties = new VectorLayerVisualizerProperties();
			tempLayerProperties.vectorTileLayer = layer;
			tempLayerProperties.featureProcessingStage = FeatureProcessingStage.PreProcess;

			//Get all filters in the array.
			tempLayerProperties.layerFeatureFilters = _layerProperties.filterOptions.filters.Select(m => m.GetFilterComparer()).ToArray();

			// Pass them to the combiner
			tempLayerProperties.layerFeatureFilterCombiner = new Filters.LayerFilterComparer();
			switch (_layerProperties.filterOptions.combinerType)
			{
				case Filters.LayerFilterCombinerOperationType.Any:
					tempLayerProperties.layerFeatureFilterCombiner = Filters.LayerFilterComparer.AnyOf(tempLayerProperties.layerFeatureFilters);
					break;
				case Filters.LayerFilterCombinerOperationType.All:
					tempLayerProperties.layerFeatureFilterCombiner = Filters.LayerFilterComparer.AllOf(tempLayerProperties.layerFeatureFilters);
					break;
				case Filters.LayerFilterCombinerOperationType.None:
					tempLayerProperties.layerFeatureFilterCombiner = Filters.LayerFilterComparer.NoneOf(tempLayerProperties.layerFeatureFilters);
					break;
				default:
					break;
			}

			tempLayerProperties.buildingsWithUniqueIds = (_layerProperties.honorBuildingIdSetting) && _layerProperties.buildingsWithUniqueIds;

			////find any replacement criteria and assign them
			foreach (var goModifier in _defaultStack.GoModifiers)
			{
				if (goModifier is IReplacementCriteria && goModifier.Active)
				{
					SetReplacementCriteria((IReplacementCriteria)goModifier);
				}
			}

			#region PreProcess & Process.

			var featureCount = (tempLayerProperties.vectorTileLayer == null) ? 0 : tempLayerProperties.vectorTileLayer.FeatureCount();
			do
			{
				for (int i = 0; i < featureCount; i++)
				{
					//checking if tile is recycled and changed
					if (tile.UnwrappedTileId != tileId || !_activeCoroutines.ContainsKey(tile) || tile.TileState == Enums.TilePropertyState.Unregistered)
					{
						yield break;
					}

					ProcessFeature(i, tile, tempLayerProperties, layer.Extent);

					if (IsCoroutineBucketFull)
					{
						//Reset bucket..
						_entityInCurrentCoroutine = 0;
						yield return null;
					}
				}
				// move processing to next stage.
				tempLayerProperties.featureProcessingStage++;
			} while (tempLayerProperties.featureProcessingStage == FeatureProcessingStage.PreProcess
			|| tempLayerProperties.featureProcessingStage == FeatureProcessingStage.Process);

			#endregion

			#region PostProcess
			// TODO : Clean this up to follow the same pattern.
			var mergedStack = _defaultStack as MergedModifierStack;
			if (mergedStack != null && tile != null)
			{
				mergedStack.End(tile, tile.gameObject, layer.Name);
			}
			#endregion

			if (callback != null)
				callback(tile, this);
		}

		private bool ProcessFeature(int index, UnityTile tile, VectorLayerVisualizerProperties layerProperties, float layerExtent)
		{
			var fe = layerProperties.vectorTileLayer.GetFeature(index);
			List<List<Point2d<float>>> geom;
			if (layerProperties.buildingsWithUniqueIds == true) //ids from building dataset is big ulongs
			{
				geom = fe.Geometry<float>(); //and we're not clipping by passing no parameters

				if (geom[0][0].X < 0 || geom[0][0].X > layerExtent || geom[0][0].Y < 0 || geom[0][0].Y > layerExtent)
				{
					return false;
				}
			}
			else //streets ids, will require clipping
			{
				geom = fe.Geometry<float>(0); //passing zero means clip at tile edge
			}

			var feature = new VectorFeatureUnity(layerProperties.vectorTileLayer.GetFeature(index),
				geom,
				tile,
				layerProperties.vectorTileLayer.Extent,
				layerProperties.buildingsWithUniqueIds);


			if (IsFeatureEligibleAfterFiltering(feature, tile, layerProperties))
			{
				if (tile != null && tile.gameObject != null && tile.VectorDataState != Enums.TilePropertyState.Cancelled)
				{
					switch (layerProperties.featureProcessingStage)
					{
						case FeatureProcessingStage.PreProcess:
							//pre process features.
							PreProcessFeatures(feature, tile, tile.gameObject);
							break;
						case FeatureProcessingStage.Process:
							//skip existing features, only works on tilesets with unique ids
							if (ShouldSkipProcessingFeatureWithId(feature.Data.Id, tile, layerProperties))
							{
								return false;
							}
							//feature not skipped. Add to pool only if features are in preprocess stage.
							AddFeatureToTileObjectPool(feature, tile);
							Build(feature, tile, tile.gameObject);
							break;
						case FeatureProcessingStage.PostProcess:
							break;
						default:
							break;
					}
					_entityInCurrentCoroutine++;
				}
			}
			return true;
		}

		/// <summary>
		/// Preprocess features, finds the relevant modifier stack and passes the feature to that stack
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="tile"></param>
		/// <param name="parent"></param>
		private bool IsFeatureValid(VectorFeatureUnity feature)
		{
			if (feature.Properties.ContainsKey("extrude") && !bool.Parse(feature.Properties["extrude"].ToString()))
				return false;

			if (feature.Points.Count < 1)
				return false;

			return true;
		}

		protected void PreProcessFeatures(VectorFeatureUnity feature, UnityTile tile, GameObject parent)
		{
			////find any replacement criteria and assign them
			foreach (var goModifier in _defaultStack.GoModifiers)
			{
				if (goModifier is IReplacementCriteria && goModifier.Active)
				{
					goModifier.FeaturePreProcess(feature);
				}
			}
		}

		protected void Build(VectorFeatureUnity feature, UnityTile tile, GameObject parent)
		{
			if (feature.Properties.ContainsKey("extrude") && !Convert.ToBoolean(feature.Properties["extrude"]))
				return;

			if (feature.Points.Count < 1)
				return;

			//this will be improved in next version and will probably be replaced by filters
			var styleSelectorKey = _layerProperties.coreOptions.sublayerName;

			var meshData = new MeshData();
			meshData.TileRect = tile.Rect;

			//and finally, running the modifier stack on the feature
			var processed = false;

			if (!processed)
			{
				if (_defaultStack != null)
				{
					_defaultStack.Execute(tile, feature, meshData, parent, styleSelectorKey);
				}
			}
		}

		protected void PostProcessFeatures(VectorFeatureUnity feature, UnityTile tile, GameObject parent)
		{
			//var mergedStack = _defaultStack as MergedModifierStack;
			//if (mergedStack != null && tile != null)
			//{
			//	mergedStack.End(tile, tile.gameObject, _vectorFeaturesPerTile[tile].vectorTileLayer.Name);
			//}
		}
		private string FindSelectorKey(VectorFeatureUnity feature)
		{
			// TODO: FIX THIS!!
			//if (string.IsNullOrEmpty(_classificationKey))
			//{
			//	if (feature.Properties.ContainsKey("type"))
			//	{
			//		return feature.Properties["type"].ToString().ToLowerInvariant();
			//	}
			//	else if (feature.Properties.ContainsKey("class"))
			//	{
			//		return feature.Properties["class"].ToString().ToLowerInvariant();
			//	}
			//}
			//else
			//TODO: Come back to this.
			//var size = _layerProperties.coreOptions.propertyValuePairs.Count;
			//for (int i = 0; i < size; i++)
			//{
			//	var key = _layerProperties.coreOptions.propertyValuePairs[i].featureKey;
			//	if (feature.Properties.ContainsKey(key))
			//	{
			//		if (feature.Properties.ContainsKey(key))
			//		{
			//			return feature.Properties[key].ToString().ToLowerInvariant();
			//		}
			//	}
			//}


			return Key;
		}

		/// <summary>
		/// Handle tile destruction event and propagate it to modifier stacks
		/// </summary>
		/// <param name="tile">Destroyed tile object</param>
		public override void OnUnregisterTile(UnityTile tile)
		{
			base.OnUnregisterTile(tile);
			if (_activeCoroutines.ContainsKey(tile))
			{
				foreach (var cor in _activeCoroutines[tile])
				{
					Runnable.Stop(cor);
				}
			}
			_activeCoroutines.Remove(tile);

			if (_defaultStack != null)
			{
				_defaultStack.UnregisterTile(tile);
			}

			//removing ids from activeIds list so they'll be recreated next time tile loads (necessary when you're unloading/loading tiles)
			if (_idPool.ContainsKey(tile))
			{
				foreach (var item in _idPool[tile])
				{
					_activeIds.Remove(item);
				}
				_idPool[tile].Clear();
			}
			//UnbindSubLayerEvents();
		}

		public override void ClearCaches()
		{
			_idPool.Clear();
			_defaultStack.ClearCaches();
		}
	}
}
