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
		public VectorSubLayerProperties SubLayerProperties
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

		public void SetProperties(VectorSubLayerProperties properties, LayerPerformanceOptions performanceOptions)
		{
			List<MeshModifier> defaultMeshModifierStack = new List<MeshModifier>();
			List<GameObjectModifier> defaultGOModifierStack = new List<GameObjectModifier>();
			_layerProperties = properties;
			_performanceOptions = performanceOptions;

			Active = _layerProperties.coreOptions.isActive;

			if (properties.coreOptions.groupFeatures)
			{
				_defaultStack = ScriptableObject.CreateInstance<MergedModifierStack>();
			}
			else
			{
				_defaultStack = ScriptableObject.CreateInstance<ModifierStack>();
				((ModifierStack)_defaultStack).moveFeaturePositionTo = _layerProperties.moveFeaturePositionTo;
			}

			_defaultStack.MeshModifiers = new List<MeshModifier>();
			_defaultStack.GoModifiers = new List<GameObjectModifier>();

			switch (properties.coreOptions.geometryType)
			{
				case VectorPrimitiveType.Point:
				case VectorPrimitiveType.Custom:
					// Let the user add anything that they want
					if (_layerProperties.coreOptions.snapToTerrain == true)
					{
						defaultMeshModifierStack.Add(CreateInstance<SnapTerrainModifier>());
					}
					break;
				case VectorPrimitiveType.Line:

					var lineMeshMod = CreateInstance<LineMeshModifier>();
					lineMeshMod.Width = _layerProperties.coreOptions.lineWidth;
					defaultMeshModifierStack.Add(lineMeshMod);

					if (_layerProperties.extrusionOptions.extrusionType != Map.ExtrusionType.None)
					{
						var heightMod = CreateInstance<HeightModifier>();
						heightMod.SetProperties(_layerProperties.extrusionOptions);
						defaultMeshModifierStack.Add(heightMod);
					}
					if (_layerProperties.coreOptions.snapToTerrain == true)
					{
						defaultMeshModifierStack.Add(CreateInstance<SnapTerrainModifier>());
					}

					//collider modifier options
					if (_layerProperties.colliderOptions.colliderType != ColliderType.None)
					{
						var lineColliderMod = CreateInstance<ColliderModifier>();
						lineColliderMod.SetProperties(_layerProperties.colliderOptions);
						defaultGOModifierStack.Add(lineColliderMod);
					}

					var lineStyleMod = CreateInstance<MaterialModifier>();
					lineStyleMod.SetProperties(MapboxDefaultStyles.GetGeometryMaterialOptions(_layerProperties.materialOptions));
					defaultGOModifierStack.Add(lineStyleMod);

					break;
				case VectorPrimitiveType.Polygon:
					if (_layerProperties.coreOptions.snapToTerrain == true)
					{
						defaultMeshModifierStack.Add(CreateInstance<SnapTerrainModifier>());
					}
					defaultMeshModifierStack.Add(CreateInstance<PolygonMeshModifier>());

					GeometryMaterialOptions geometryMaterialOptions = MapboxDefaultStyles.GetGeometryMaterialOptions(_layerProperties.materialOptions);

					UVModifierOptions uvModOptions = new UVModifierOptions();
					uvModOptions.texturingType = geometryMaterialOptions.texturingType;
					uvModOptions.atlasInfo = geometryMaterialOptions.atlasInfo;
					uvModOptions.style = geometryMaterialOptions.style;

					var uvMod = CreateInstance<UvModifier>();
					uvMod.SetProperties(uvModOptions);
					defaultMeshModifierStack.Add(uvMod);

					if (_layerProperties.extrusionOptions.extrusionType != Map.ExtrusionType.None)
					{
						//replace materialOptions with styleOptions
						if (geometryMaterialOptions.texturingType == UvMapType.Atlas || geometryMaterialOptions.texturingType == UvMapType.AtlasWithColorPalette)
						{
							var atlasMod = CreateInstance<TextureSideWallModifier>();
							GeometryExtrusionWithAtlasOptions atlasOptions = new GeometryExtrusionWithAtlasOptions(_layerProperties.extrusionOptions, uvModOptions);
							atlasMod.SetProperties(atlasOptions);
							defaultMeshModifierStack.Add(atlasMod);
						}
						else
						{
							var heightMod = CreateInstance<HeightModifier>();
							heightMod.SetProperties(_layerProperties.extrusionOptions);
							defaultMeshModifierStack.Add(heightMod);
						}
					}

					//collider modifier options
					if (_layerProperties.colliderOptions.colliderType != ColliderType.None)
					{
						var polyColliderMod = CreateInstance<ColliderModifier>();
						polyColliderMod.SetProperties(_layerProperties.colliderOptions);
						defaultGOModifierStack.Add(polyColliderMod);
					}

					var styleMod = CreateInstance<MaterialModifier>();

					styleMod.SetProperties(geometryMaterialOptions);
					defaultGOModifierStack.Add(styleMod);

					if (geometryMaterialOptions.texturingType == UvMapType.AtlasWithColorPalette)
					{
						var colorPaletteMod = CreateInstance<MapboxStylesColorModifier>();
						colorPaletteMod.m_scriptablePalette = geometryMaterialOptions.colorPalette;

						defaultGOModifierStack.Add(colorPaletteMod);
					}
					break;
				default:
					break;
			}

			_defaultStack.MeshModifiers.AddRange(defaultMeshModifierStack);
			_defaultStack.GoModifiers.AddRange(defaultGOModifierStack);

			//Add any additional modifiers that were added.
			_defaultStack.MeshModifiers.AddRange(_layerProperties.MeshModifiers);
			_defaultStack.GoModifiers.AddRange(_layerProperties.GoModifiers);

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

		public override void Create(VectorTileLayer layer, UnityTile tile, Action callback)
		{
			if (!_activeCoroutines.ContainsKey(tile))
				_activeCoroutines.Add(tile, new List<int>());
			_activeCoroutines[tile].Add(Runnable.Run(ProcessLayer(layer, tile, callback)));
		}

		protected IEnumerator ProcessLayer(VectorTileLayer layer, UnityTile tile, Action callback = null)
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

			var featureCount = tempLayerProperties.vectorTileLayer.FeatureCount();
			do
			{
				for (int i = 0; i < featureCount; i++)
				{

					ProcessFeature(i, tile, tempLayerProperties);

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
				callback();
		}

		private bool ProcessFeature(int index, UnityTile tile, VectorLayerVisualizerProperties layerProperties)
		{
			var feature = GetFeatureinTileAtIndex(index, tile, layerProperties);

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
			tile.VectorDataState = Enums.TilePropertyState.Cancelled;
			if (_activeCoroutines.ContainsKey(tile))
			{
				foreach (var cor in _activeCoroutines[tile])
				{
					Runnable.Stop(cor);
				}
			}

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
		}
	}
}
