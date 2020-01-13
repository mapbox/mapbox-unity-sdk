using System;
using System.Collections.Generic;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;

namespace Mapbox.Core.VectorModule
{
	[Serializable]
	public class VectorProcessorModifierStack
	{
		public List<MeshModifier> MeshModifiers = new List<MeshModifier>();
		public List<GameObjectModifier> GoModifiers = new List<GameObjectModifier>();

		public PositionTargetType moveFeaturePositionTo;

		private int vertexIndex = 1;
		private Dictionary<UnityTile, List<VectorEntity>> _activeObjects;
		private ObjectPool<VectorEntity> _pool;

		private Vector3 _tempPoint;
		private VectorEntity _tempVectorEntity;
		private ObjectPool<List<VectorEntity>> _listPool;

		private int _counter;
		private int _secondCounter;

		public VectorProcessorModifierStack()
		{
			_pool = new ObjectPool<VectorEntity>(() =>
			{
				var go = new GameObject();
				var mf = go.AddComponent<MeshFilter>();
				mf.sharedMesh = new Mesh();
				mf.sharedMesh.name = "feature";
				var mr = go.AddComponent<MeshRenderer>();
				_tempVectorEntity = new VectorEntity()
				{
					GameObject = go,
					Transform = go.transform,
					MeshFilter = mf,
					MeshRenderer = mr,
					Mesh = mf.sharedMesh
				};
				return _tempVectorEntity;
			});
			_listPool = new ObjectPool<List<VectorEntity>>(() => { return new List<VectorEntity>(); });
			_activeObjects = new Dictionary<UnityTile, List<VectorEntity>>();

			_counter = MeshModifiers.Count;
			for (int i = 0; i < _counter; i++)
			{
				MeshModifiers[i].Initialize();
			}

			_counter = GoModifiers.Count;
			for (int i = 0; i < _counter; i++)
			{
				GoModifiers[i].Initialize();
			}
		}

		public void OnUnregisterTile(UnityTile tile)
		{
			if (_activeObjects.ContainsKey(tile))
			{
				_counter = _activeObjects[tile].Count;
				for (int i = 0; i < _counter; i++)
				{
					foreach (var item in GoModifiers)
					{
						item.OnPoolItem(_activeObjects[tile][i]);
					}
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

		public MeshData Execute(VectorFeatureUnity feature, MeshData meshData)
		{
			_counter = feature.Points.Count;
			_secondCounter = 0;

			if (moveFeaturePositionTo != PositionTargetType.TileCenter)
			{
				_tempPoint = Constants.Math.Vector3Zero;
				if (moveFeaturePositionTo == PositionTargetType.FirstVertex)
				{
					_tempPoint = feature.Points[0][0];
				}
				else if (moveFeaturePositionTo == PositionTargetType.CenterOfVertices)
				{
					//this is not precisely the center because of the duplicates  (first/last vertex) but close to center
					_tempPoint = feature.Points[0][0];
					vertexIndex = 1;

					for (int i = 0; i < _counter; i++)
					{
						_secondCounter = feature.Points[i].Count;
						for (int j = 0; j < _secondCounter; j++)
						{
							_tempPoint += feature.Points[i][j];
							vertexIndex++;
						}
					}
					_tempPoint /= vertexIndex;
				}

				for (int i = 0; i < _counter; i++)
				{
					_secondCounter = feature.Points[i].Count;
					for (int j = 0; j < _secondCounter; j++)
					{
						feature.Points[i][j] = new Vector3(feature.Points[i][j].x - _tempPoint.x, 0, feature.Points[i][j].z - _tempPoint.z);
					}
				}
				meshData.PositionInTile = _tempPoint;
			}

			meshData.PositionInTile = _tempPoint;
			_counter = MeshModifiers.Count;
			for (int i = 0; i < _counter; i++)
			{
				if (MeshModifiers[i] != null && MeshModifiers[i].Active)
				{
					MeshModifiers[i].Run(feature, meshData, null);
				}
			}

			return meshData;

//			_tempVectorEntity = _pool.GetObject();
//
//			// It is possible that we changed scenes in the middle of map generation.
//			// This object can be null as a result of Unity cleaning up game objects in the scene.
//			// Let's bail if we don't have our object.
//			if (_tempVectorEntity.GameObject == null)
//			{
//				return null;
//			}
//
//			_tempVectorEntity.GameObject.SetActive(true);
//			_tempVectorEntity.Mesh.Clear();
//			_tempVectorEntity.Feature = feature;
//
//#if UNITY_EDITOR
//			if (feature.Data != null)
//			{
//				_tempVectorEntity.GameObject.name = feature.Data.Id.ToString();
//			}
//#endif
//			_tempVectorEntity.Mesh.subMeshCount = meshData.Triangles.Count;
//			_tempVectorEntity.Mesh.SetVertices(meshData.Vertices);
//			_tempVectorEntity.Mesh.SetNormals(meshData.Normals);
//			if (meshData.Tangents.Count > 0)
//			{
//				_tempVectorEntity.Mesh.SetTangents(meshData.Tangents);
//			}
//
//			_counter = meshData.Triangles.Count;
//			for (int i = 0; i < _counter; i++)
//			{
//				_tempVectorEntity.Mesh.SetTriangles(meshData.Triangles[i], i);
//			}
//			_counter = meshData.UV.Count;
//			for (int i = 0; i < _counter; i++)
//			{
//				_tempVectorEntity.Mesh.SetUVs(i, meshData.UV[i]);
//			}

//			_tempVectorEntity.Transform.SetParent(parent.transform, false);
//
//			if (!_activeObjects.ContainsKey(tile))
//			{
//				_activeObjects.Add(tile, _listPool.GetObject());
//			}
//			_activeObjects[tile].Add(_tempVectorEntity);


//			_tempVectorEntity.Transform.localPosition = meshData.PositionInTile;
//
//			_counter = GoModifiers.Count;
//			for (int i = 0; i < _counter; i++)
//			{
//				if (GoModifiers[i].Active)
//				{
//					GoModifiers[i].Run(_tempVectorEntity, null);
//				}
//			}

			// return _tempVectorEntity.GameObject;
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
