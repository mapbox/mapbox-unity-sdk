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
	/// Modifier Stacks
	/// Modifier Stack can be thought as styles as as they contain all the data/settings for how the feature will be visualized.
	/// They also create the game objects in default implementations in the sdk.
	/// Currently there's two implementations of this; Modifier Stack and Merged Modifier Stack.They work almost exactly same 
	/// (logically) with one difference; modifier stacks creates a game object for each feature while merged modifier stack, 
	/// merges them up as the name suggest and create one game object for multiple(as many as possible) features.Both have 
	/// their advantages but the main factor here is the performance.Regular modifier stack creates individual game object so 
	/// it's easier to interact, move, animate etc features.But if you want to visualize whole San Francisco, that would mean 
	/// just 200k-300k buildings which would hit performance really hard. In such a case, especially if you don't need 
	/// individual interaction or something, you can use merged modifier stack, which will probably be able to create whole 
	/// SF around a few hundred game objects.
	/// They contain two lists; mesh modifier list and game object modifier list.These modifiers are used to create and 
	/// decorate game objects.
	/// Mesh modifiers generate data required for the game objects mesh. I.e.polygon mesh modifier triangulates the polygn, 
	/// height modifier extrudes the polygon and adds volume etc, uv modifier changes UV mapping etc.
	/// Game object modifiers decorate created game objects, like settings material, interaction scripts, animations etc. 
	/// i.e.Material modifier sets materials to mesh and submeshes, highlight modifier adds mouse highlight to features, 
	/// feature behaviour adds a script to keep feature data on game objects etc.
	/// So the idea here is; run all mesh modifiers first, generate all the data required for mesh.Create game object 
	/// using that mesh data.Run all game object modifiers to decorate that game object.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Modifier Stack")]
	public class ModifierStack : ModifierStackBase
	{
		[SerializeField] public PositionTargetType moveFeaturePositionTo;


		[NonSerialized] private int vertexIndex = 1;
		[NonSerialized] private Dictionary<UnityTile, List<VectorEntity>> _activeObjects;
		[NonSerialized] private ObjectPool<VectorEntity> _pool;

		[NonSerialized] private Vector3 _tempPoint;
		[NonSerialized] private VectorEntity _tempVectorEntity;
		[NonSerialized] private ObjectPool<List<VectorEntity>> _listPool;

		[NonSerialized] private int _counter;
		[NonSerialized] private int _secondCounter;
		protected virtual void OnEnable()
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

		public override void Initialize()
		{
			base.Initialize();

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

		public override GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent = null, string type = "")
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
					MeshModifiers[i].Run(feature, meshData, tile);
				}
			}

			_tempVectorEntity = _pool.GetObject();

			// It is possible that we changed scenes in the middle of map generation.
			// This object can be null as a result of Unity cleaning up game objects in the scene.
			// Let's bail if we don't have our object.
			if (_tempVectorEntity.GameObject == null)
			{
				return null;
			}

			_tempVectorEntity.GameObject.SetActive(true);
			_tempVectorEntity.Mesh.Clear();
			_tempVectorEntity.Feature = feature;

#if UNITY_EDITOR
			if (feature.Data != null)
			{
				_tempVectorEntity.GameObject.name = type + " - " + feature.Data.Id;
			}
			else
			{
				_tempVectorEntity.GameObject.name = type;
			}
#endif
			_tempVectorEntity.Mesh.subMeshCount = meshData.Triangles.Count;
			_tempVectorEntity.Mesh.SetVertices(meshData.Vertices);
			_tempVectorEntity.Mesh.SetNormals(meshData.Normals);
			if (meshData.Tangents.Count > 0)
			{
				_tempVectorEntity.Mesh.SetTangents(meshData.Tangents);
			}

			_counter = meshData.Triangles.Count;
			for (int i = 0; i < _counter; i++)
			{
				_tempVectorEntity.Mesh.SetTriangles(meshData.Triangles[i], i);
			}
			_counter = meshData.UV.Count;
			for (int i = 0; i < _counter; i++)
			{
				_tempVectorEntity.Mesh.SetUVs(i, meshData.UV[i]);
			}

			_tempVectorEntity.Transform.SetParent(parent.transform, false);

			if (!_activeObjects.ContainsKey(tile))
			{
				_activeObjects.Add(tile, _listPool.GetObject());
			}
			_activeObjects[tile].Add(_tempVectorEntity);


			_tempVectorEntity.Transform.localPosition = meshData.PositionInTile;

			_counter = GoModifiers.Count;
			for (int i = 0; i < _counter; i++)
			{
				if (GoModifiers[i].Active)
				{
					GoModifiers[i].Run(_tempVectorEntity, tile);
				}
			}

			return _tempVectorEntity.GameObject;
		}

		public override void ClearCaches()
		{
			foreach (var modifier in GoModifiers)
			{
				modifier.ClearCaches();
			}
			foreach (var vectorEntity in _pool.GetQueue())
			{
				Destroy(vectorEntity.GameObject);
			}

			foreach (var tileTuple in _activeObjects)
			{
				foreach (var vectorEntity in tileTuple.Value)
				{
					Destroy(vectorEntity.GameObject);
				}
			}
			_pool.Clear();
			_activeObjects.Clear();
			_pool.Clear();
		}
	}
}