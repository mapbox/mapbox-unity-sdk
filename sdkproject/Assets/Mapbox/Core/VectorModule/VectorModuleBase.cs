using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Rendering;

namespace Mapbox.Core.VectorModule
{
	public class VectorModuleBase : MonoBehaviour
	{
		public AbstractMap AbstractMap;
		private VectorProcessor _vectorProcessor;
		private Dictionary<UnwrappedTileId, List<MeshData>> _cachedMeshData;

		private HashSet<UnwrappedTileId> _processing = new HashSet<UnwrappedTileId>();
		private Dictionary<UnwrappedTileId, List<Tuple<GameObject, MeshFilter, MeshRenderer, Mesh>>> _trackedObjects = new Dictionary<UnwrappedTileId, List<Tuple<GameObject, MeshFilter, MeshRenderer, Mesh>>>();

		public Material[] Materials;
		public MeshModifier[] Modifiers;

		private Queue<Tuple<UnwrappedTileId, List<MeshData>>> _queue;
		private Queue<Tuple<GameObject, MeshFilter, MeshRenderer, Mesh>> _gameObjectStash;

		private void Awake()
		{
			CreateGameObjectPool();

			_queue = new Queue<Tuple<UnwrappedTileId, List<MeshData>>>();
			_cachedMeshData = new Dictionary<UnwrappedTileId, List<MeshData>>();
			_vectorProcessor = new VectorProcessor()
			{
				ModifierStack = new VectorModuleMergedModifierStack()
				{
					MeshModifiers = Modifiers.ToList()
				}
			};
			AbstractMap.OnTilesStarting += LoadTiles;
			//AbstractMap.OnTileFinished += TilesLoaded;
			AbstractMap.OnTileFinished += CheckTileData;
			AbstractMap.OnTilesDisposing += DisposeTile;
			_vectorProcessor.MeshOutput += MeshOutputReceived;
		}

		#region LoadVectorAfterTileFinished
		private void TilesLoaded(UnityTile tile)
		{
			_vectorProcessor.CreateVectorVisuals(tile);
		}

		private void CheckTileData(UnityTile tile)
		{
			if (_cachedMeshData.ContainsKey(tile.UnwrappedTileId))
			{
				CreateObjects(_cachedMeshData[tile.UnwrappedTileId], tile.UnwrappedTileId);
				_cachedMeshData.Remove(tile.UnwrappedTileId);
			}
		}
		#endregion

		#region LoadVectorBeforeStart
		private void LoadTiles(List<UnwrappedTileId> tileIdList)
		{
			foreach (var tileId in tileIdList)
			{
				if (!_processing.Contains(tileId))
				{
					_processing.Add(tileId);
					_vectorProcessor.CreateVectorVisuals(tileIdList);
				}
				else
				{
					Debug.Log("tile already in processing list? this shouldn't happen");
				}
			}
		}
		#endregion

		private void DisposeTile(List<UnwrappedTileId> tileIds)
		{
			foreach (var tileId in tileIds)
			{
				if (_trackedObjects.ContainsKey(tileId))
				{
					foreach (var go in _trackedObjects[tileId])
					{
						go.Item1.SetActive(false);
						go.Item4.Clear(false);
						go.Item1.transform.SetParent(transform);
						_gameObjectStash.Enqueue(go);
					}
				}
			}
		}

		private void MeshOutputReceived(CanonicalTileId tileId, List<MeshData> meshDataList)
		{
			if (meshDataList == null || meshDataList.Count == 0)
				return;

			var uwt = new UnwrappedTileId(tileId.Z, tileId.X, tileId.Y);
			_processing.Remove(uwt);

			if (!_cachedMeshData.ContainsKey(uwt))
			{
				_cachedMeshData.Add(uwt, meshDataList);
				_queue.Enqueue(new Tuple<UnwrappedTileId, List<MeshData>>(uwt, meshDataList));
			}
			else
			{
				//Debug.Log("Thread what are you doing? " + uwt);
			}
		}

		private void Update()
		{
			lock (_cachedMeshData)
			{
				if (_cachedMeshData.Count > 0)
				{
					for (int i = 0; i < _queue.Count; i++)
					{
						var entry = _queue.Dequeue();
						if (entry != null)
						{
							_cachedMeshData.Remove(entry.Item1);
							CreateObjects(entry.Item2, entry.Item1);
						}
					}
				}
			}
		}

		private void CreateObjects(List<MeshData> meshDataList, UnwrappedTileId uwt)
		{
			if (meshDataList == null || meshDataList.Count == 0)
				return;

			if (!AbstractMap.MapVisualizer.ActiveTiles.ContainsKey(uwt))
				return;

			foreach (var meshData in meshDataList)
			{
				if (meshData.Vertices.Count < 3)
				{
					continue;
				}

				var entry = _gameObjectStash.Dequeue();

				var mesh = entry.Item4;
				mesh.SetVertices(meshData.Vertices);
				mesh.subMeshCount = 0;
				mesh.subMeshCount = meshData.Triangles.Count;
				for (int i = 0; i < meshData.Triangles.Count; i++)
				{
					mesh.SetTriangles(meshData.Triangles[i], i);
				}

				mesh.SetNormals(meshData.Normals);
				for (int i = 0; i < meshData.UV.Count; i++)
				{
					mesh.SetUVs(i, meshData.UV[i]);
				}

				var unityTile = AbstractMap.MapVisualizer.ActiveTiles[uwt];
				entry.Item1.transform.SetParent(unityTile.transform, false);
				entry.Item1.transform.localPosition = Vector3.zero;

				if (!_trackedObjects.ContainsKey(uwt))
				{
					_trackedObjects.Add(uwt, new List<Tuple<GameObject, MeshFilter, MeshRenderer, Mesh>>());
				}

				_trackedObjects[uwt].Add(entry);
				entry.Item1.SetActive(true);
			}
		}

		private void CreateGameObjectPool()
		{
			_gameObjectStash = new Queue<Tuple<GameObject, MeshFilter, MeshRenderer, Mesh>>();
			for (int i = 0; i < 900; i++)
			{
				var go = new GameObject();
				go.transform.SetParent(transform);
				var mf = go.AddComponent<MeshFilter>();
				var mr = go.AddComponent<MeshRenderer>();
				mr.materials = Materials;
				var mesh = new Mesh();
				mesh.indexFormat = IndexFormat.UInt32;
				mesh.Clear();
				mf.mesh = mesh;
				go.SetActive(false);
				_gameObjectStash.Enqueue(new Tuple<GameObject, MeshFilter, MeshRenderer, Mesh>(go, mf, mr, mesh));
			}
		}
	}
}
