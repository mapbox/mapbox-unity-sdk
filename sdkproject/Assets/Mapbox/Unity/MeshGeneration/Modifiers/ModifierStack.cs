namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using System;

	public enum PositionTargetType
	{
		TileCenter,
		FirstVertex,
		CenterOfVertices
	}

	/// <summary>
	/// Modifier Stack creates a game object from a feature using given modifiers.
	/// It runs mesh modifiers, creates the game object and then run the game object modifiers.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Modifier Stack")]
	public class ModifierStack : ModifierStackBase
	{
		[SerializeField] private PositionTargetType _moveFeaturePositionTo;
		private Vector3 _center = Vector3.zero;
		[NodeEditorElement("Mesh Modifiers")] public List<MeshModifier> MeshModifiers;
		[NodeEditorElement("Game Object Modifiers")] public List<GameObjectModifier> GoModifiers;

		[NonSerialized] private int vertexIndex = 1;
		[NonSerialized] private Dictionary<UnityTile, List<VectorEntity>> _activeObjects;
		[NonSerialized] private ObjectPool<VectorEntity> _pool;

		[NonSerialized] private Vector3 _tempPoint;
		[NonSerialized] private VectorEntity _tempVectorEntity;
		[NonSerialized] private ObjectPool<List<VectorEntity>> _listPool;

		private void OnEnable()
		{
			_pool = new ObjectPool<VectorEntity>(() =>
			{
				var go = new GameObject();
				var mf = go.AddComponent<MeshFilter>();
				var mr = go.AddComponent<MeshRenderer>();
				_tempVectorEntity = new VectorEntity()
				{
					GameObject = go,
					Transform = go.transform,
					MeshFilter = mf,
					MeshRenderer = mr,
					Mesh = mf.mesh
				};
				return _tempVectorEntity;
			});
			_listPool = new ObjectPool<List<VectorEntity>>(() => { return new List<VectorEntity>(); });
			_activeObjects = new Dictionary<UnityTile, List<VectorEntity>>();
		}

		public override void OnUnregisterTile(UnityTile tile)
		{
			if (_activeObjects.ContainsKey(tile))
			{
				for (int i = 0; i < _activeObjects[tile].Count; i++)
				{
					_activeObjects[tile][i].GameObject.SetActive(false);
					_pool.Put(_activeObjects[tile][i]);
				}
				_activeObjects[tile].Clear();
				//pooling these lists as they'll reused anyway, saving hundreds of list instantiations
				_listPool.Put(_activeObjects[tile]);
				_activeObjects.Remove(tile);
			}
		}

		public override void Initialize()
		{
			base.Initialize();

			for (int i = 0; i < MeshModifiers.Count; i++)
			{
				MeshModifiers[i].Initialize();
			}

			for (int i = 0; i < GoModifiers.Count; i++)
			{
				GoModifiers[i].Initialize();
			}
		}


		public override GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
		{
			_center = Vector3.zero;
			if (_moveFeaturePositionTo != PositionTargetType.TileCenter)
			{
				_tempPoint = Constants.Math.Vector3Zero;
				if (_moveFeaturePositionTo == PositionTargetType.FirstVertex)
				{
					_tempPoint = feature.Points[0][0];
				}
				else if (_moveFeaturePositionTo == PositionTargetType.CenterOfVertices)
				{
					//this is not precisely the center because of the duplicates  (first/last vertex) but close to center
					_tempPoint = feature.Points[0][0];
					vertexIndex = 1;
					for (int i = 0; i < feature.Points.Count; i++)
					{
						for (int j = 0; j < feature.Points[i].Count; j++)
						{
							_tempPoint += feature.Points[i][j];
							vertexIndex++;
						}
					}
					_tempPoint /= vertexIndex;
				}

				for (int i = 0; i < feature.Points.Count; i++)
				{
					for (int j = 0; j < feature.Points[i].Count; j++)
					{
						feature.Points[i][j] = new Vector3(feature.Points[i][j].x - _tempPoint.x, 0, feature.Points[i][j].z - _tempPoint.z);
					}
				}
				_center = _tempPoint;
			}

			for (int i = 0; i < MeshModifiers.Count; i++)
			{
				if (MeshModifiers[i] != null && MeshModifiers[i].Active)
				{
					MeshModifiers[i].Run(feature, meshData, tile);
				}
			}


			_tempVectorEntity = _pool.GetObject();
			_tempVectorEntity.GameObject.SetActive(true);
			_tempVectorEntity.Mesh.Clear();

			_tempVectorEntity.GameObject.name = type + " - " + feature.Data.Id;
			_tempVectorEntity.Mesh.subMeshCount = meshData.Triangles.Count;
			_tempVectorEntity.Mesh.SetVertices(meshData.Vertices);
			_tempVectorEntity.Mesh.SetNormals(meshData.Normals);
			for (int i = 0; i < meshData.Triangles.Count; i++)
			{
				_tempVectorEntity.Mesh.SetTriangles(meshData.Triangles[i], i);
			}

			for (int i = 0; i < meshData.UV.Count; i++)
			{
				_tempVectorEntity.Mesh.SetUVs(i, meshData.UV[i]);
			}

			_tempVectorEntity.Transform.SetParent(parent.transform, false);

			if (!_activeObjects.ContainsKey(tile))
			{
				_activeObjects.Add(tile, _listPool.GetObject());
			}
			_activeObjects[tile].Add(_tempVectorEntity);


			_tempVectorEntity.Transform.localPosition = _center;

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
}