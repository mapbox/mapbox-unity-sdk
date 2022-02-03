using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.Utilities;
using Mapbox.VectorTile.Geometry;
using UnityEngine.Rendering;

namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Map;

	public class VectorLayerVisualizerProperties
	{
		public bool buildingsWithUniqueIds = false;
		public ILayerFeatureFilterComparer[] layerFeatureFilters;
		public ILayerFeatureFilterComparer layerFeatureFilterCombiner;
	}

	public class VectorLayerVisualizer : LayerVisualizerBase
	{
		public override VectorSubLayerProperties SubLayerProperties
		{
			get
			{
				return _sublayerProperties;
			}
			set
			{
				_sublayerProperties = value;
			}
		}
		public override bool Active => _sublayerProperties.coreOptions.isActive;
		private ObjectPool<VectorEntity> _pool;
		private ObjectPool<List<VectorEntity>> _listPool;
		private Dictionary<UnityTile, List<VectorEntity>> _activeObjects;
		private VectorLayerVisualizerProperties _tempLayerProperties;

		//id tracking stuff, only necessary for unique id layers like `buildings with ids`
		private HashSet<ulong> _activeIds;
		private Dictionary<CanonicalTileId, List<ulong>> _idPool; //necessary to keep _activeIds list up to date when unloading tiles

		public override string Key
		{
			get { return _sublayerProperties.coreOptions.layerName; }
			set { _sublayerProperties.coreOptions.layerName = value; }
		}

		public override void Initialize()
		{
			base.Initialize();
			_activeIds = new HashSet<ulong>();
			_idPool = new Dictionary<CanonicalTileId, List<ulong>>();
			_tempLayerProperties = GetLayerTempProperties();

			foreach (var modifierStack in _modifierStacks)
			{
				modifierStack.Initialize();
			}

			_pool = new ObjectPool<VectorEntity>(() =>
			{
				var go = new GameObject();
				var mf = go.AddComponent<MeshFilter>();
				mf.sharedMesh = new Mesh();
				mf.sharedMesh.name = "feature";
				var mr = go.AddComponent<MeshRenderer>();
				var tempVectorEntity = new VectorEntity()
				{
					GameObject = go,
					Transform = go.transform,
					MeshFilter = mf,
					MeshRenderer = mr,
					Mesh = mf.sharedMesh
				};
				return tempVectorEntity;
			});
			_activeObjects = new Dictionary<UnityTile, List<VectorEntity>>();
			_listPool = new ObjectPool<List<VectorEntity>>(() => { return new List<VectorEntity>(); });
		}

		public override void Disable()
		{
			base.Disable();
			foreach (var tilePair in _activeObjects)
			{
				foreach (var entity in tilePair.Value)
				{
					entity.GameObject.SetActive(false);
					_pool.Put(entity);
				}
				tilePair.Value.Clear();
			}
			_activeObjects.Clear();
		}

		protected override void OnUnregisterTile(UnityTile tile)
		{
			ClearTasksOnUnregister(tile);
			ClearIdsOnUnregister(tile);

			//MapboxAccess.Instance.TaskManager.CancelTask(tile.CanonicalTileId.GenerateKey(Key));
		}

		protected override void OnClearTile(UnityTile tile)
		{
			ClearObjectOnUnregister(tile);
		}

		public override void Clear()
		{
			_idPool.Clear();

			foreach (var vectorEntity in _pool.GetQueue())
			{
				if (vectorEntity.Mesh != null)
				{
					vectorEntity.Mesh.Destroy(true);
				}

				vectorEntity.GameObject.Destroy();
			}

			foreach (var tileTuple in _activeObjects)
			{
				foreach (var vectorEntity in tileTuple.Value)
				{
					if (vectorEntity.Mesh != null)
					{
						vectorEntity.Mesh.Destroy(true);
					}
					vectorEntity.GameObject.Destroy();
				}
			}
			_pool.Clear();
			_activeObjects.Clear();
			_pool.Clear();
		}

		public override void Create(UnityTile tile, Action<UnityTile, LayerVisualizerBase> callback)
		{
			ProcessLayer(tile, tile.UnwrappedTileId, callback);
		}

		private void ProcessLayer(UnityTile tile, UnwrappedTileId tileId, Action<UnityTile, LayerVisualizerBase> callback = null)
		{
			if (tile == null || tile.IsStopped || tile.IsRecycled)
			{
				return;
			}

			tile.Logs.Add("ProcessLayer");
			var cachedTileId = tile.CanonicalTileId;
			var cachedCallback = callback;

			var source = new CancellationTokenSource();
			var token = source.Token;
			var meshDataList = new Dictionary<ModifierStack, List<Tuple<VectorFeatureUnity, MeshData>>>();

			void Action()
			{
				if (tile.IsRecycled || tile.IsStopped)
				{
					return;
				}

				var capturedToken = token;
				var layer = tile.VectorData.Data.GetLayer(Key);
				if (layer == null)
				{
					return;
				}

				var layerExtent = layer.Extent;
				for (int i = 0; i < layer.FeatureCount(); i++)
				{
					var feature = layer.GetFeature(i);
					var featureResult = new VectorFeatureUnity();
					featureResult.Properties = feature.GetProperties();

					if (capturedToken.IsCancellationRequested) break;
					if (IsFeatureInvalid(cachedTileId, featureResult, _tempLayerProperties)) continue;

					var geometry = feature.Geometry<float>(0);
					var points = new List<List<Vector3>>();
					foreach (var t in geometry)
					{
						var pointCount = t.Count;
						var newPoints = new List<Vector3>(pointCount);
						for (int k = 0; k < pointCount; k++)
						{
							var point = t[k];
							newPoints.Add(new Vector3(
								((point.X - layerExtent/2) / layerExtent) * tile.TileSize,
								0,
								(((layerExtent - point.Y)- layerExtent/2) / layerExtent) * tile.TileSize));
						}
						points.Add(newPoints);
					}

					featureResult.Points = points;
					if (featureResult.Points.Count < 1)
					{
						continue;
					}

					featureResult.Data = feature;

					foreach (var modifierStack in _modifierStacks)
					{
						if (modifierStack.FeatureFilterCombiner.Try(featureResult))
						{
							var meshData = new MeshData();
							meshData = modifierStack.RunMeshModifiers(tile, featureResult, meshData, tile.TileSize);
							if (!meshDataList.ContainsKey(modifierStack))
							{
								meshDataList.Add(modifierStack, new List<Tuple<VectorFeatureUnity, MeshData>>());
							}

							meshDataList[modifierStack].Add(new Tuple<VectorFeatureUnity, MeshData>(featureResult, meshData));
						}
					}
				}

				if (!capturedToken.IsCancellationRequested)
				{
					foreach (var pairs in meshDataList)
					{
						if (pairs.Key.combineMeshes)
						{
							var mergedData = CombineMeshData(pairs.Value);
							pairs.Value.Clear();
							pairs.Value.Add(new Tuple<VectorFeatureUnity, MeshData>(null, mergedData));
						}
					}
				}
			}
			void ContinueWith(Task t)
			{
				tile.Logs.Add("ContinueWith");
				if (_activeObjects.ContainsKey(tile))
				{
					ClearObjectOnUnregister(tile);
				}

				if (t.IsCanceled || tile.IsRecycled || tile.IsStopped)
				{
					meshDataList.Clear();
				}
				else
				{
					//is there a better way to check this?
					if (tile.CanonicalTileId == cachedTileId && !tile.IsRecycled)
					{
						tile.Logs.Add("creating objects");
						foreach (var pair in meshDataList)
						{
							foreach (var meshTuples in pair.Value)
							{
								var entity = CreateObject(tile, meshTuples.Item2, tile.gameObject, Key);
								entity.Feature = meshTuples.Item1;
#if UNITY_EDITOR
								if (meshTuples.Item1 != null && meshTuples.Item1.Data != null) entity.GameObject.name = Key;
#endif

								pair.Key.RunGoModifiers(entity, tile);
							}
						}
					}
				}

				cachedCallback?.Invoke(tile, this);
			}

			var taskWrapper = new TaskWrapper(tile.CanonicalTileId.GenerateKey(Key))
			{
				TileId = tile.CanonicalTileId,
				TilesetId = Key,
				Action = Action,
				Token = source,
				ContinueWith = ContinueWith,
				OnCancelled = () => { cachedCallback(tile, this);},
				#if UNITY_EDITOR
				Info = "VectorLayerVisualizer.ProcessLayer"
				#endif
			};



			MapboxAccess.Instance.TaskManager.AddTask(taskWrapper);
		}

		private static MeshData CombineMeshData(List<Tuple<VectorFeatureUnity, MeshData>> meshDataList)
		{
			var mergedData = new MeshData();
			var _counter = meshDataList.Count;
			for (int i = 0; i < _counter; i++)
			{
				var currentData = meshDataList[i].Item2;
				if (currentData.Vertices.Count <= 3)
					continue;

				var st = mergedData.Vertices.Count;
				mergedData.Vertices.AddRange(currentData.Vertices);
				mergedData.Normals.AddRange(currentData.Normals);
				mergedData.Tangents.AddRange(currentData.Tangents);

				var c2 = currentData.UV.Count;
				for (int j = 0; j < c2; j++)
				{
					if (mergedData.UV.Count <= j)
					{
						mergedData.UV.Add(new List<Vector2>(currentData.UV[j].Count));
					}
				}

				c2 = currentData.UV.Count;
				for (int j = 0; j < c2; j++)
				{
					mergedData.UV[j].AddRange(currentData.UV[j]);
				}

				c2 = currentData.Triangles.Count;
				for (int j = 0; j < c2; j++)
				{
					if (mergedData.Triangles.Count <= j)
					{
						mergedData.Triangles.Add(new List<int>(currentData.Triangles[j].Count));
					}
				}

				for (int j = 0; j < c2; j++)
				{
					for (int k = 0; k < currentData.Triangles[j].Count; k++)
					{
						mergedData.Triangles[j].Add(currentData.Triangles[j][k] + st);
					}
				}
			}

			return mergedData;
		}

		private VectorEntity CreateObject(UnityTile tile, MeshData meshData, GameObject parent, string type)
		{
			var tempVectorEntity = _pool.GetObject();

			// It is possible that we changed scenes in the middle of map generation.
			// This object can be null as a result of Unity cleaning up game objects in the scene.
			// Let's bail if we don't have our object.
			if (tempVectorEntity.GameObject == null)
			{
				return null;
			}

			tempVectorEntity.GameObject.SetActive(true);
			tempVectorEntity.Mesh.Clear();


			tempVectorEntity.Mesh.subMeshCount = meshData.Triangles.Count;
			tempVectorEntity.Mesh.indexFormat = IndexFormat.UInt32;
			tempVectorEntity.Mesh.SetVertices(meshData.Vertices);
			tempVectorEntity.Mesh.SetNormals(meshData.Normals);
			if (meshData.Tangents.Count > 0)
			{
				tempVectorEntity.Mesh.SetTangents(meshData.Tangents);
			}

			var counter = meshData.Triangles.Count;
			tempVectorEntity.Mesh.subMeshCount = counter;
			for (int i = 0; i < counter; i++)
			{
				tempVectorEntity.Mesh.SetTriangles(meshData.Triangles[i], i);
			}

			counter = meshData.UV.Count;
			for (int i = 0; i < counter; i++)
			{
				tempVectorEntity.Mesh.SetUVs(i, meshData.UV[i]);
			}

			tempVectorEntity.Transform.SetParent(parent.transform, false);

			if (!_activeObjects.ContainsKey(tile))
			{
				_activeObjects.Add(tile, _listPool.GetObject());
			}

			_activeObjects[tile].Add(tempVectorEntity);


			tempVectorEntity.Transform.localPosition = meshData.PositionInTile;

			return tempVectorEntity;
		}

		protected override void UpdateVector(object sender, System.EventArgs eventArgs)
		{
			var layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;
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

		#region Private Helper Methods
		private bool IsFeatureInvalid(CanonicalTileId tileId, VectorFeatureUnity feature, VectorLayerVisualizerProperties tempLayerProperties)
		{
			// if (!IsFeatureEligibleAfterFiltering(feature, tempLayerProperties))
			// 	return true;

			//this part is necessary for unique id layers (buildings)
			//it keeps track of processed ids and doesn't recreate them
			// if (tempLayerProperties.buildingsWithUniqueIds)
			// {
			// 	if (ShouldSkipProcessingFeatureWithId(feature.Data.Id, tempLayerProperties))
			// 		return true;
			//
			// 	AddFeatureToTileObjectPool(feature, tileId);
			// }

			if (feature.Properties.ContainsKey("extrude") && !bool.Parse(feature.Properties["extrude"].ToString()))
				return true;

			return false;
		}

		private VectorLayerVisualizerProperties GetLayerTempProperties()
		{
			var tempLayerProperties = new VectorLayerVisualizerProperties
			{
				buildingsWithUniqueIds = _sublayerProperties.buildingsWithUniqueIds,
				layerFeatureFilters = _sublayerProperties.filterOptions.filters.Select(m => m.GetFilterComparer()).ToArray(),
				layerFeatureFilterCombiner = new LayerFilterComparer()
			};

			//Get all filters in the array.

			// Pass them to the combiner
			switch (_sublayerProperties.filterOptions.combinerType)
			{
				case Filters.LayerFilterCombinerOperationType.Any:
					tempLayerProperties.layerFeatureFilterCombiner = LayerFilterComparer.AnyOf(tempLayerProperties.layerFeatureFilters);
					break;
				case Filters.LayerFilterCombinerOperationType.All:
					tempLayerProperties.layerFeatureFilterCombiner = LayerFilterComparer.AllOf(tempLayerProperties.layerFeatureFilters);
					break;
				case Filters.LayerFilterCombinerOperationType.None:
					tempLayerProperties.layerFeatureFilterCombiner = LayerFilterComparer.NoneOf(tempLayerProperties.layerFeatureFilters);
					break;
				default:
					break;
			}

			tempLayerProperties.buildingsWithUniqueIds = (_sublayerProperties.honorBuildingIdSetting) && _sublayerProperties.buildingsWithUniqueIds;
			return tempLayerProperties;
		}

		private void ClearObjectOnUnregister(UnityTile tile)
		{
			if (_activeObjects.ContainsKey(tile))
			{
				var counter = _activeObjects[tile].Count;
				for (int i = 0; i < counter; i++)
				{
					// foreach (var item in GoModifiers)
					// {
					// 	item.OnPoolItem(_activeObjects[tile][i]);
					// }
					if (null != _activeObjects[tile][i].GameObject)
					{
						_activeObjects[tile][i].GameObject.SetActive(false);
					}

					_pool.Put(_activeObjects[tile][i]);
				}

				_activeObjects[tile].Clear();

				//pooling these lists as they'll reused anyway, saving hundreds of list instantiations
				_listPool.Put(_activeObjects[tile]);
				_activeObjects.Remove(tile);
			}
		}

		private void ClearIdsOnUnregister(UnityTile tile)
		{
			//ids = unique ids in some certain layers (building-id layer)
			//removing ids from activeIds list so they'll be recreated next time tile loads (necessary when you're unloading/loading tiles)
			if (_idPool.ContainsKey(tile.CanonicalTileId))
			{
				foreach (var item in _idPool[tile.CanonicalTileId])
				{
					_activeIds.Remove(item);
				}

				_idPool[tile.CanonicalTileId].Clear();
			}
		}

		private void ClearTasksOnUnregister(UnityTile tile)
		{
			// if (_tasks.ContainsKey(tile.CanonicalTileId))
			// {
			// 	foreach (var task in _tasks[tile.CanonicalTileId])
			// 	{
			// 		task.Cancelled(task);
			// 	}
			//
			// 	_tasks.Remove(tile.CanonicalTileId);
			// }
		}

		/// <summary>
		/// Convenience function to add feature to Tile object pool.
		/// </summary>
		/// <param name="feature">Feature to be added to the pool.</param>
		/// <param name="tile">Tile currently being processed.</param>
		private void AddFeatureToTileObjectPool(VectorFeatureUnity feature, CanonicalTileId tileId)
		{
			_activeIds.Add(feature.Data.Id);
			if (!_idPool.ContainsKey(tileId))
			{
				_idPool.Add(tileId, new List<ulong>() { feature.Data.Id });
			}
			else
			{
				_idPool[tileId].Add(feature.Data.Id);
			}
		}

		/// <summary>
		/// Apply filters to the layer and check if the current feature is eleigible for rendering.
		/// </summary>
		/// <returns><c>true</c>, if feature eligible after filtering was applied, <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		private bool IsFeatureEligibleAfterFiltering(VectorFeatureUnity feature, VectorLayerVisualizerProperties layerProperties)
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
		/// Function to check if the feature is already in the active Id pool, features already in active Id pool should be skipped from processing.
		/// </summary>
		/// <returns><c>true</c>, if feature is already in activeId pool or if the layer has buildingsWithUniqueId flag set to <see langword="true"/>, <c>false</c> otherwise.</returns>
		/// <param name="featureId">Feature identifier.</param>
		private bool ShouldSkipProcessingFeatureWithId(ulong featureId, VectorLayerVisualizerProperties layerProperties)
		{
			return (layerProperties.buildingsWithUniqueIds && _activeIds.Contains(featureId));
		}



		#endregion
	}
}
