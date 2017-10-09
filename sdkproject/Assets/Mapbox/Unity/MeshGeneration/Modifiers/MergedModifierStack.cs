using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Components;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	/// <summary>
	/// Merged Modifier Stack, just like regular Modifier stack, creates a game object from features. But the difference is, regular modifier stack creates a game object for each given faeture meanwhile Merged Modifier Stack merges meshes and creates one game object for all features (until the 65k vertex limit).
	/// It has extremely higher performance compared to regular modifier stack but since it merged all entities together, it also loses all individual entity data & makes it harder to interact with them.
	/// It pools and merges objects based on the tile contains them.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Merged Modifier Stack")]
	public class MergedModifierStack : ModifierStackBase
	{
		[NodeEditorElement("Mesh Modifiers")]
		public List<MeshModifier> MeshModifiers;
		[NodeEditorElement("Mesh Modifiers")]
		public List<GameObjectModifier> GoModifiers;

		[NonSerialized] private Dictionary<UnityTile, int> _cacheVertexCount = new Dictionary<UnityTile, int>();
		[NonSerialized] private Dictionary<UnityTile, List<MeshData>> _cached = new Dictionary<UnityTile, List<MeshData>>();
		[NonSerialized] private Dictionary<UnityTile, int> _buildingCount = new Dictionary<UnityTile, int>();

		[NonSerialized] private Dictionary<UnityTile, List<VectorEntity>> _activeObjects = new Dictionary<UnityTile, List<VectorEntity>>();
		[NonSerialized] private MeshData _tempMeshData;
		[NonSerialized] private MeshFilter _tempMeshFilter;
		[NonSerialized] private GameObject _tempGameObject;
		[NonSerialized] private VectorEntity _tempVectorEntity;
		[NonSerialized] private MeshData _temp2MeshData;
		[NonSerialized] private ObjectPool<VectorEntity> _pool;
		[NonSerialized] private ObjectPool<List<VectorEntity>> _listPool;
		[NonSerialized] private ObjectPool<List<MeshData>> _meshDataPool;

		private void OnEnable()
		{
			//we'll use this to concat building data until it reaches 65000 verts
			_pool = new ObjectPool<VectorEntity>(() =>
			{
				_tempGameObject = new GameObject();
				_tempMeshFilter = _tempGameObject.AddComponent<MeshFilter>();
				_tempMeshFilter.mesh.MarkDynamic();
				_tempVectorEntity = new VectorEntity()
				{
					GameObject = _tempGameObject,
					Transform = _tempGameObject.transform,
					MeshFilter = _tempMeshFilter,
					MeshRenderer = _tempGameObject.AddComponent<MeshRenderer>(),
					Mesh = _tempMeshFilter.mesh
				};
				return _tempVectorEntity;
			});
			_listPool = new ObjectPool<List<VectorEntity>>(() => { return new List<VectorEntity>(); });
			_meshDataPool = new ObjectPool<List<MeshData>>(() => { return new List<MeshData>(); });
			_tempMeshData = new MeshData();
		}

		public override void OnUnregisterTile(UnityTile tile)
		{
			//removing all caches 
			if (_activeObjects.ContainsKey(tile))
			{
				foreach (var ve in _activeObjects[tile])
				{
					ve.GameObject.SetActive(false);
					_pool.Put(ve);
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

		public override void Initialize()
		{
			base.Initialize();
			//init is also used for reloading map/ location change, so reseting everything here
			_cacheVertexCount.Clear();
			_cached.Clear();
			_buildingCount.Clear();

			foreach (var mmod in MeshModifiers)
			{
				mmod.Initialize();
			}

			foreach (var gmod in GoModifiers)
			{
				gmod.Initialize();
			}
		}

		public override GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
		{
			base.Execute(tile, feature, meshData, parent, type);

			if (!_cacheVertexCount.ContainsKey(tile))
			{
				_cacheVertexCount.Add(tile, 0);
				_cached.Add(tile, _meshDataPool.GetObject());
				_buildingCount.Add(tile, 0);
			}

			_buildingCount[tile]++;
			foreach (MeshModifier mod in MeshModifiers.Where(x => x.Active))
			{
				mod.Run(feature, meshData, tile);
			}

			GameObject go = null;
			if (_cacheVertexCount[tile] + meshData.Vertices.Count() < 64000)
			{
				_cacheVertexCount[tile] += meshData.Vertices.Count();
				_cached[tile].Add(meshData);
			}
			else
			{
				go = End(tile, parent, type);
			}

			return go;
		}


		public GameObject End(UnityTile tile, GameObject parent, string name = "")
		{
			if (_cached.ContainsKey(tile))
			{
				_tempMeshData.Clear();

				for (int i = 0; i < _cached[tile].Count; i++)
				{
					_temp2MeshData = _cached[tile][i];
					if (_temp2MeshData.Vertices.Count <= 3)
						continue;

					var st = _tempMeshData.Vertices.Count;
					_tempMeshData.Vertices.AddRange(_temp2MeshData.Vertices);
					_tempMeshData.Normals.AddRange(_temp2MeshData.Normals);

					for (int j = 0; j < _temp2MeshData.UV.Count; j++)
					{
						if (_tempMeshData.UV.Count <= j)
							_tempMeshData.UV.Add(new List<Vector2>(_temp2MeshData.UV[j].Count));
					}

					for (int j = 0; j < _temp2MeshData.UV.Count; j++)
					{
						_tempMeshData.UV[j].AddRange(_temp2MeshData.UV[j]);
					}

					for (int j = 0; j < _temp2MeshData.Triangles.Count; j++)
					{
						if (_tempMeshData.Triangles.Count <= j)
							_tempMeshData.Triangles.Add(new List<int>(_temp2MeshData.Triangles[j].Count));
					}

					for (int j = 0; j < _temp2MeshData.Triangles.Count; j++)
					{
						_tempMeshData.Triangles[j].AddRange(_temp2MeshData.Triangles[j].Select(x => x + st));
					}
				}

				if (_tempMeshData.Vertices.Count > 3)
				{
					_tempVectorEntity = null;
					_tempVectorEntity = _pool.GetObject();
					_tempVectorEntity.GameObject.SetActive(true);
					_tempVectorEntity.Mesh.Clear();

					_tempVectorEntity.GameObject.name = name;
					_tempVectorEntity.Mesh.subMeshCount = _tempMeshData.Triangles.Count;
					_tempVectorEntity.Mesh.SetVertices(_tempMeshData.Vertices);
					_tempVectorEntity.Mesh.SetNormals(_tempMeshData.Normals);
					for (int i = 0; i < _tempMeshData.Triangles.Count; i++)
					{
						_tempVectorEntity.Mesh.SetTriangles(_tempMeshData.Triangles[i], i);
					}

					for (int i = 0; i < _tempMeshData.UV.Count; i++)
					{
						_tempVectorEntity.Mesh.SetUVs(i, _tempMeshData.UV[i]);
					}

					_tempVectorEntity.GameObject.transform.SetParent(tile.transform, false);

					if (!_activeObjects.ContainsKey(tile))
					{
						_activeObjects.Add(tile, _listPool.GetObject());
					}
					_activeObjects[tile].Add(_tempVectorEntity);

					for (int i = 0; i < GoModifiers.Count; i++)
					{
						if (GoModifiers[i].Active)
						{
							GoModifiers[i].Run(_tempVectorEntity, tile);
						}
					}

					return _tempVectorEntity.GameObject;
				}
			}
			return null;
		}
	}
}