using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Components;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	[Serializable]
	public class VectorModuleMergedModifierStack
	{
		public List<MeshModifier> MeshModifiers = new List<MeshModifier>();
		public List<GameObjectModifier> GoModifiers = new List<GameObjectModifier>();

		private Dictionary<CanonicalTileId, int> _cacheVertexCount = new Dictionary<CanonicalTileId, int>();
		private Dictionary<CanonicalTileId, List<MeshData>> _cached = new Dictionary<CanonicalTileId, List<MeshData>>();
		private Dictionary<CanonicalTileId, int> _buildingCount = new Dictionary<CanonicalTileId, int>();

		private Dictionary<CanonicalTileId, List<VectorEntity>> _activeObjects = new Dictionary<CanonicalTileId, List<VectorEntity>>();
		private MeshFilter _tempMeshFilter;
		private GameObject _tempGameObject;
		private VectorEntity _tempVectorEntity;
		private ObjectPool<VectorEntity> _pool;
		private ObjectPool<List<VectorEntity>> _listPool;
		private ObjectPool<List<MeshData>> _meshDataPool;

		public VectorModuleMergedModifierStack()
		{
			_pool = new ObjectPool<VectorEntity>(() =>
			{
				_tempGameObject = new GameObject();
				_tempMeshFilter = _tempGameObject.AddComponent<MeshFilter>();
				_tempMeshFilter.sharedMesh = new Mesh();
				_tempVectorEntity = new VectorEntity()
				{
					GameObject = _tempGameObject,
					Transform = _tempGameObject.transform,
					MeshFilter = _tempMeshFilter,
					MeshRenderer = _tempGameObject.AddComponent<MeshRenderer>(),
					Mesh = _tempMeshFilter.sharedMesh
				};
				return _tempVectorEntity;
			});
			_listPool = new ObjectPool<List<VectorEntity>>(() => { return new List<VectorEntity>(); });
			_meshDataPool = new ObjectPool<List<MeshData>>(() => { return new List<MeshData>(); });

			//init is also used for reloading map/ location change, so reseting everything here
			_cacheVertexCount.Clear();
			_cached.Clear();
			_buildingCount.Clear();
			_pool.Clear();

			for (int i = 0; i < MeshModifiers.Count; i++)
			{
				MeshModifiers[i].Initialize();
			}

			for (int i = 0; i < GoModifiers.Count; i++)
			{
				GoModifiers[i].Initialize();
			}
		}

		public void OnUnregisterTile(CanonicalTileId tile)
		{
			//removing all caches
			if (_activeObjects.ContainsKey(tile))
			{
				for (int i = 0; i < _activeObjects[tile].Count; i++)
				{
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

			//reset all counters
			if (_cacheVertexCount.ContainsKey(tile))
			{
				_cacheVertexCount.Remove(tile);
			}

			if (_cached.ContainsKey(tile))
			{
				_cached[tile].Clear();
				_meshDataPool.Put(_cached[tile]);
				_cached.Remove(tile);
			}

			if (_buildingCount.ContainsKey(tile))
			{
				_buildingCount.Remove(tile);
			}
		}

		public void Execute(CanonicalTileId tileId, VectorFeatureUnity feature, MeshData meshData)
		{
			lock (_cacheVertexCount)
			{
				if (!_cacheVertexCount.ContainsKey(tileId))
				{

					_cacheVertexCount.Add(tileId, 0);


					lock (_cached)
					{
						_cached.Add(tileId, _meshDataPool.GetObject());
					}

					lock (_buildingCount)
					{
						_buildingCount.Add(tileId, 0);
					}
				}
			}

			_buildingCount[tileId]++;
			for (int i = 0; i < MeshModifiers.Count; i++)
			{
				if (MeshModifiers[i] != null && MeshModifiers[i].Active)
				{
					MeshModifiers[i].Run(feature, meshData, null);
				}
			}

			_cacheVertexCount[tileId] += meshData.Vertices.Count;
			_cached[tileId].Add(meshData);
		}

		public MeshData End(CanonicalTileId tile)
		{
			var c2 = 0;
			if (_cached.ContainsKey(tile))
			{
				var tempMeshData = new MeshData();
				tempMeshData.Clear();

				//concat mesh data into _tempMeshData
				for (int i = 0; i < _cached[tile].Count; i++)
				{
					var temp2MeshData = _cached[tile][i];
					if (temp2MeshData == null || temp2MeshData.Vertices.Count <= 3)
						continue;

					var st = tempMeshData.Vertices.Count;
					tempMeshData.Vertices.AddRange(temp2MeshData.Vertices);
					tempMeshData.Normals.AddRange(temp2MeshData.Normals);

					c2 = temp2MeshData.UV.Count;
					for (int j = 0; j < c2; j++)
					{
						if (tempMeshData.UV.Count <= j)
						{
							tempMeshData.UV.Add(new List<Vector2>(temp2MeshData.UV[j].Count));
						}
					}

					c2 = temp2MeshData.UV.Count;
					for (int j = 0; j < c2; j++)
					{
						tempMeshData.UV[j].AddRange(temp2MeshData.UV[j]);
					}

					c2 = temp2MeshData.Triangles.Count;
					for (int j = 0; j < c2; j++)
					{
						if (tempMeshData.Triangles.Count <= j)
						{
							tempMeshData.Triangles.Add(new List<int>(temp2MeshData.Triangles[j].Count));
						}
					}

					for (int j = 0; j < c2; j++)
					{
						for (int k = 0; k < temp2MeshData.Triangles[j].Count; k++)
						{
							tempMeshData.Triangles[j].Add(temp2MeshData.Triangles[j][k] + st);
						}
					}
				}

				_cached[tile].Clear();
				_cacheVertexCount[tile] = 0;

				return tempMeshData;

//				//update pooled vector entity with new data
//				if (_tempMeshData.Vertices.Count > 3)
//				{
//					_cached[tile].Clear();
//					_cacheVertexCount[tile] = 0;
//					_tempVectorEntity = null;
//					_tempVectorEntity = _pool.GetObject();
//					_tempVectorEntity.GameObject.SetActive(true);
//					_tempVectorEntity.Mesh.Clear();
//
//					_tempVectorEntity.GameObject.name = name;
//					_tempVectorEntity.Mesh.subMeshCount = _tempMeshData.Triangles.Count;
//					_tempVectorEntity.Mesh.SetVertices(_tempMeshData.Vertices);
//					_tempVectorEntity.Mesh.SetNormals(_tempMeshData.Normals);
//
//					_counter = _tempMeshData.Triangles.Count;
//					for (int i = 0; i < _counter; i++)
//					{
//						_tempVectorEntity.Mesh.SetTriangles(_tempMeshData.Triangles[i], i);
//					}
//
//					_counter = _tempMeshData.UV.Count;
//					for (int i = 0; i < _counter; i++)
//					{
//						_tempVectorEntity.Mesh.SetUVs(i, _tempMeshData.UV[i]);
//					}
//
//					_tempVectorEntity.GameObject.transform.SetParent(tile.transform, false);
//
//					if (!_activeObjects.ContainsKey(tile))
//					{
//						_activeObjects.Add(tile, _listPool.GetObject());
//					}
//					_activeObjects[tile].Add(_tempVectorEntity);
//
//					_counter = GoModifiers.Count;
//					for (int i = 0; i < _counter; i++)
//					{
//						if (GoModifiers[i].Active)
//						{
//							GoModifiers[i].Run(_tempVectorEntity, tile);
//						}
//					}
//
//					return _tempVectorEntity.GameObject;
//				}
			}

			return null;
		}

		public void Clear()
		{
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
	}
}
