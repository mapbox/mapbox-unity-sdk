using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Core.VectorModule
{
	public class VectorModuleBase : MonoBehaviour
	{
		public AbstractMap AbstractMap;
		[SerializeField] private VectorProcessor _vectorProcessor;
		private Dictionary<UnwrappedTileId, System.Collections.Generic.List<MeshData>> _cachedMeshData;

		private HashSet<UnwrappedTileId> _processing = new HashSet<UnwrappedTileId>();
		private Dictionary<UnwrappedTileId, List<GameObject>> _trackedObjects = new Dictionary<UnwrappedTileId, List<GameObject>>();

		public Material[] Materials;

		private void Awake()
		{
			_cachedMeshData = new Dictionary<UnwrappedTileId, List<MeshData>>();
			_vectorProcessor.MeshOutput += MeshOutputRecieved;
			AbstractMap.OnTilesStarting += LoadTiles;
			AbstractMap.OnTileFinished += CheckTileData;
			AbstractMap.OnTilesDisposing += DisposeTile;
		}

		private void LoadTiles(List<UnwrappedTileId> tileIdList)
		{
			foreach (var tileId in tileIdList)
			{
				if (!_processing.Contains(tileId))
				{
					_processing.Add(tileId);
				}
				else
				{
					Debug.Log("wtf");
				}
			}
			_vectorProcessor.CreateVectorVisuals(tileIdList);
		}

		private void CheckTileData(UnityTile tile)
		{
			if (_cachedMeshData.ContainsKey(tile.UnwrappedTileId))
			{
				CreateObjects(_cachedMeshData[tile.UnwrappedTileId], tile.UnwrappedTileId);
				_cachedMeshData.Remove(tile.UnwrappedTileId);
			}
		}

		private void DisposeTile(List<UnwrappedTileId> tileIds)
		{
			foreach (var tileId in tileIds)
			{
				if (_trackedObjects.ContainsKey(tileId))
				{
					foreach (var go in _trackedObjects[tileId])
					{
						Destroy(go);
					}
				}
			}
		}

		private void MeshOutputRecieved(CanonicalTileId tileId, List<MeshData> meshDataList)
		{
			var uwt = new UnwrappedTileId(tileId.Z, tileId.X, tileId.Y);
			_processing.Remove(uwt);

;			if (AbstractMap.MapVisualizer.ActiveTiles.ContainsKey(uwt))
			{
				CreateObjects(meshDataList, uwt);
			}
			else
			{
				_cachedMeshData.Add(uwt, meshDataList);
			}
		}

		private void CreateObjects(List<MeshData> meshDataList, UnwrappedTileId uwt)
		{
			foreach (var meshData in meshDataList)
			{
				var go = new GameObject();
				var meshFilter = go.AddComponent<MeshFilter>();
				var meshRenderer = go.AddComponent<MeshRenderer>();

				var mesh = new Mesh();
				mesh.SetVertices(meshData.Vertices);
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

				meshFilter.mesh = mesh;

				meshRenderer.materials = Materials;

				var unityTile = AbstractMap.MapVisualizer.ActiveTiles[uwt];
				go.transform.SetParent(unityTile.transform, false);

				if(!_trackedObjects.ContainsKey(uwt))
					_trackedObjects.Add(uwt, new List<GameObject>());

				_trackedObjects[uwt].Add(go);
			}
		}
	}
}
