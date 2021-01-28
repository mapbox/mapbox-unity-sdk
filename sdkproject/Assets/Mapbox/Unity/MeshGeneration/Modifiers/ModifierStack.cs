using System.Threading.Tasks;
using Mapbox.Map;

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


		//[NonSerialized] private int vertexIndex = 1;
		[NonSerialized] private Dictionary<UnityTile, List<VectorEntity>> _activeObjects;
		[NonSerialized] private ObjectPool<VectorEntity> _pool;

		//[NonSerialized] private Vector3 _tempPoint;
		//[NonSerialized] private VectorEntity _tempVectorEntity;
		[NonSerialized] private ObjectPool<List<VectorEntity>> _listPool;

		//[NonSerialized] private int _counter;
		//[NonSerialized] private int _secondCounter;
		protected virtual void OnEnable()
		{
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
			_listPool = new ObjectPool<List<VectorEntity>>(() => { return new List<VectorEntity>(); });
			_activeObjects = new Dictionary<UnityTile, List<VectorEntity>>();
		}

		public override void OnUnregisterTile(UnityTile tile)
		{
			if (_activeObjects.ContainsKey(tile))
			{
				var counter = _activeObjects[tile].Count;
				for (int i = 0; i < counter; i++)
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

			var counter = MeshModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				MeshModifiers[i].Initialize();
			}

			counter = GoModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				GoModifiers[i].Initialize();
			}
		}

		private Dictionary<CanonicalTileId, List<Tuple<VectorFeatureUnity, MeshData>>> _allMeshes = new Dictionary<CanonicalTileId, List<Tuple<VectorFeatureUnity, MeshData>>>();
		public override void RunLayer(Mapbox.Map.VectorTile.VectorLayerResult vectorTileLayer, UnityTile tt, GameObject parent, string type = "")
		{
			var layer = vectorTileLayer;
			var tile = tt;
			var localTile = tile.CanonicalTileId;
			var task = Task.Run(() =>
			{
				var lineMeshCore = new LineMeshCore();
				foreach (var feature in layer.Features)
				{
					if (feature.Properties.ContainsKey("extrude") && !bool.Parse(feature.Properties["extrude"].ToString()))
						continue;

					if (feature.Points.Count < 1)
						continue;

					var meshData = new MeshData();
					Taskable(tile, feature, meshData, lineMeshCore);
					if (!_allMeshes.ContainsKey(localTile))
					{
						_allMeshes.Add(localTile, new List<Tuple<VectorFeatureUnity, MeshData>>());
					}
					_allMeshes[localTile].Add(new Tuple<VectorFeatureUnity, MeshData>(feature, meshData));
				}
			});

			task.ContinueWith((t) =>
			{
				if (tile.CanonicalTileId == localTile)
				{
					foreach (var mesh in _allMeshes[localTile])
					{
						CreateObject(tile, mesh.Item1, mesh.Item2, parent, type);
					}
				}
				else
				{
					Debug.Log("Recycled");
				}
			}, TaskScheduler.FromCurrentSynchronizationContext());

		}

		// public override GameObject Execute(UnityTile tile, VectorFeatureUnity feature, MeshData asd, GameObject parent = null, string type = "")
		// {
		// 	var counter = feature.Points.Count;
		// 	var secondCounter = 0;
		//
		// 	var meshData = new MeshData();
		// 	var task = new Task( () => Taskable(tile, feature, meshData));
		// 	task.ContinueWith((t) =>
		// 	{
		// 		CreateObject(tile, feature, meshData, parent, type);
		// 	}, TaskScheduler.FromCurrentSynchronizationContext());
		// 	task.Start();
		//
		// 	return null; //CreateObject(tile, feature, meshData, parent, type);
		// }


		private MeshData Taskable(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, LineMeshCore lineMeshCore)
		{
			foreach (var subfeature in feature.Points)
			{
				for (var index = 0; index < subfeature.Count; index++)
				{
					subfeature[index] *= tile.TileSize;
				}
			}

			var tempPoint = Constants.Math.Vector3Zero;
			var counter = feature.Points.Count;
			var secondCounter = 0;
			if (moveFeaturePositionTo != PositionTargetType.TileCenter)
			{

				if (moveFeaturePositionTo == PositionTargetType.FirstVertex)
				{
					tempPoint = feature.Points[0][0];
				}
				else if (moveFeaturePositionTo == PositionTargetType.CenterOfVertices)
				{
					//this is not precisely the center because of the duplicates  (first/last vertex) but close to center
					tempPoint = feature.Points[0][0];
					var vertexIndex = 1;

					for (int i = 0; i < counter; i++)
					{
						secondCounter = feature.Points[i].Count;
						for (int j = 0; j < secondCounter; j++)
						{
							tempPoint += feature.Points[i][j];
							vertexIndex++;
						}
					}

					tempPoint /= vertexIndex;
				}

				for (int i = 0; i < counter; i++)
				{
					secondCounter = feature.Points[i].Count;
					for (int j = 0; j < secondCounter; j++)
					{
						feature.Points[i][j] = new Vector3(feature.Points[i][j].x - tempPoint.x, 0, feature.Points[i][j].z - tempPoint.z);
					}
				}

				meshData.PositionInTile = tempPoint;
			}

			meshData.PositionInTile = tempPoint;
			counter = MeshModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				if (MeshModifiers[i] != null && MeshModifiers[i].Active)
				{
					if (MeshModifiers[i] is ICoreWrapper)
					{
						(MeshModifiers[i] as ICoreWrapper).SetCore(lineMeshCore);
					}
					MeshModifiers[i].Run(feature, meshData, tile);
				}
			}

			return meshData;
		}

		private GameObject CreateObject(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, GameObject parent, string type)
		{
			if (meshData.Vertices.Count != meshData.UV[0].Count ||
			    meshData.Vertices.Count != meshData.Tangents.Count)
			{
				return null;
			}

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
			tempVectorEntity.Feature = feature;

#if UNITY_EDITOR
			if (feature.Data != null)
			{
				tempVectorEntity.GameObject.name = type + " - " + feature.Data.Id;
			}
			else
			{
				tempVectorEntity.GameObject.name = type;
			}
#endif
			tempVectorEntity.Mesh.subMeshCount = meshData.Triangles.Count;
			tempVectorEntity.Mesh.SetVertices(meshData.Vertices);
			tempVectorEntity.Mesh.SetNormals(meshData.Normals);
			if (meshData.Tangents.Count > 0)
			{
				tempVectorEntity.Mesh.SetTangents(meshData.Tangents);
			}

			var counter = meshData.Triangles.Count;
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

			counter = GoModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				if (GoModifiers[i].Active)
				{
					GoModifiers[i].Run(tempVectorEntity, tile);
				}
			}

			return tempVectorEntity.GameObject;
		}

		public override void Clear()
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
