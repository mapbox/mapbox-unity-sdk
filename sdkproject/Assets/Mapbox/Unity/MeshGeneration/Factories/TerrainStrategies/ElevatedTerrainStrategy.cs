using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Utils;
using Debug = UnityEngine.Debug;

namespace Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies
{
	public class MeshDataArray
	{
		public Vector3[] Vertices;
		public Vector3[] Normals;
		public int[] Triangles;
		public Vector2[] Uvs;
	}

	public class ElevatedTerrainStrategy : TerrainStrategy, IElevationBasedTerrainStrategy
	{
		private Dictionary<UnwrappedTileId, Mesh> _meshData;
		private MeshData _currentTileMeshData;
		private Dictionary<UnityTile, MeshDataArray> _cachedMeshDataArrays;
		private Dictionary<UnwrappedTileId, MeshDataArray> _dataArrays;
		private Dictionary<int, MeshDataArray> _meshSamples;

		private List<Vector3> _newVertexList;
		private List<Vector3> _newNormalList;
		private List<Vector2> _newUvList;
		private List<int> _newTriangleList;
		private Vector3 _newDir;
		private int _vertA, _vertB, _vertC;
		private int _counter;

		public override int RequiredVertexCount
		{
			get { return _elevationOptions.modificationOptions.sampleCount * _elevationOptions.modificationOptions.sampleCount; }
		}

		public override void Initialize(ElevationLayerProperties elOptions)
		{
			base.Initialize(elOptions);

			_meshSamples = new Dictionary<int, MeshDataArray>();
			_dataArrays = new Dictionary<UnwrappedTileId, MeshDataArray>();
			_cachedMeshDataArrays = new Dictionary<UnityTile, MeshDataArray>();

			_meshData = new Dictionary<UnwrappedTileId, Mesh>();
			_currentTileMeshData = new MeshData();
			var sampleCountSquare = _elevationOptions.modificationOptions.sampleCount * _elevationOptions.modificationOptions.sampleCount;
			_newVertexList = new List<Vector3>(sampleCountSquare);
			_newNormalList = new List<Vector3>(sampleCountSquare);
			_newUvList = new List<Vector2>(sampleCountSquare);
			_newTriangleList = new List<int>();
		}

		public override void RegisterTile(UnityTile tile)
		{
			if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
			{
				tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
			}

			if (tile.MeshFilter.sharedMesh.vertexCount != RequiredVertexCount || !_cachedMeshDataArrays.ContainsKey(tile))
			{
				tile.MeshFilter.sharedMesh.Clear();

				if (_meshSamples.ContainsKey(_elevationOptions.modificationOptions.sampleCount))
				{
					var newMesh = _meshSamples[_elevationOptions.modificationOptions.sampleCount];
					tile.MeshFilter.sharedMesh.vertices = newMesh.Vertices;
					tile.MeshFilter.sharedMesh.normals = newMesh.Normals;
					tile.MeshFilter.sharedMesh.triangles = newMesh.Triangles;
					tile.MeshFilter.sharedMesh.uv = newMesh.Uvs;
				}
				else
				{
					//TODO remoev tile dependency from CreateBaseMesh method
					var newMesh = CreateBaseMesh(tile, _elevationOptions.modificationOptions.sampleCount);
					_meshSamples.Add(_elevationOptions.modificationOptions.sampleCount, newMesh);
					tile.MeshFilter.sharedMesh.vertices = newMesh.Vertices;
					tile.MeshFilter.sharedMesh.normals = newMesh.Normals;
					tile.MeshFilter.sharedMesh.triangles = newMesh.Triangles;
					tile.MeshFilter.sharedMesh.uv = newMesh.Uvs;
				}

				if (!_dataArrays.ContainsKey(tile.UnwrappedTileId))
				{
					_dataArrays.Add(tile.UnwrappedTileId, new MeshDataArray()
					{
						Normals = tile.MeshFilter.sharedMesh.normals,
						Vertices = tile.MeshFilter.sharedMesh.vertices,
						Triangles = tile.MeshFilter.sharedMesh.triangles
					});
				}
			}
			else
			{
				_dataArrays.Add(tile.UnwrappedTileId, _cachedMeshDataArrays[tile]);
				_cachedMeshDataArrays.Remove(tile);
			}

			tile.ElevationType = TileTerrainType.Elevated;

			GenerateTerrainMesh(tile);
		}

		public override void UnregisterTile(UnityTile tile)
		{
			_meshData.Remove(tile.UnwrappedTileId);
			if (_dataArrays.ContainsKey(tile.UnwrappedTileId))
			{
				_cachedMeshDataArrays.Add(tile, _dataArrays[tile.UnwrappedTileId]);
				_dataArrays.Remove(tile.UnwrappedTileId);
			}
		}

		public override void DataErrorOccurred(UnityTile t, TileErrorEventArgs e)
		{
			ResetToFlatMesh(t);
		}

		public override void PostProcessTile(UnityTile tile)
		{

		}

		#region mesh gen

		private MeshDataArray CreateBaseMesh(UnityTile tile, int sampleCount)
		{
			//TODO use arrays instead of lists
			_newVertexList.Clear();
			_newNormalList.Clear();
			_newUvList.Clear();
			_newTriangleList.Clear();

			for (float y = 0; y < sampleCount; y++)
			{
				var yrat = y / (sampleCount - 1);
				for (float x = 0; x < sampleCount; x++)
				{
					var xrat = x / (sampleCount - 1);

					var xx = Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, xrat);
					var yy = Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, yrat);

					_newVertexList.Add(new Vector3(
						(float) (xx - tile.Rect.Center.x) * tile.TileScale,
						0,
						(float) (yy - tile.Rect.Center.y) * tile.TileScale));
					_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);
					_newUvList.Add(new Vector2(x * 1f / (sampleCount - 1), 1 - (y * 1f / (sampleCount - 1))));
				}
			}

			int vertA, vertB, vertC;
			for (int y = 0; y < sampleCount - 1; y++)
			{
				for (int x = 0; x < sampleCount - 1; x++)
				{
					vertA = (y * sampleCount) + x;
					vertB = (y * sampleCount) + x + sampleCount + 1;
					vertC = (y * sampleCount) + x + sampleCount;
					_newTriangleList.Add(vertA);
					_newTriangleList.Add(vertB);
					_newTriangleList.Add(vertC);

					vertA = (y * sampleCount) + x;
					vertB = (y * sampleCount) + x + 1;
					vertC = (y * sampleCount) + x + sampleCount + 1;
					_newTriangleList.Add(vertA);
					_newTriangleList.Add(vertB);
					_newTriangleList.Add(vertC);
				}
			}

			var mesh = new MeshDataArray();
			mesh.Vertices = _newVertexList.ToArray();
			mesh.Normals = _newNormalList.ToArray();
			mesh.Uvs = _newUvList.ToArray();
			mesh.Triangles = _newTriangleList.ToArray();
			return mesh;
		}

		private Vector3[] _verts;
		private Vector3[] _normals;
		private Vector3[] _targetVerts;
		private Vector3[] _targetNormals;


		private void GenerateTerrainMesh(UnityTile tile)
		{
			_verts = _dataArrays[tile.UnwrappedTileId].Vertices;
			_normals = _dataArrays[tile.UnwrappedTileId].Normals;

			var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
			var hd = tile.HeightData;
			var ts = tile.TileScale;
			for (float y = 0; y < _sampleCount; y++)
			{
				for (float x = 0; x < _sampleCount; x++)
				{
					_verts[(int) (y * _sampleCount + x)] = new Vector3(
						_verts[(int) (y * _sampleCount + x)].x,
						hd[((int)((1 - y / (_sampleCount - 1)) * 255) * 256) + ((int)(x / (_sampleCount - 1) * 255))] * ts,
						_verts[(int) (y * _sampleCount + x)].z);
					_normals[(int) (y * _sampleCount + x)] = Mapbox.Unity.Constants.Math.Vector3Zero;
				}
			}

			FixStitches(tile.UnwrappedTileId, _verts, _normals);

			tile.MeshFilter.sharedMesh.vertices = _verts;
			tile.MeshFilter.sharedMesh.RecalculateNormals();

			tile.MeshFilter.sharedMesh.RecalculateBounds();

			if (!_meshData.ContainsKey(tile.UnwrappedTileId))
			{
				_meshData.Add(tile.UnwrappedTileId, tile.MeshFilter.sharedMesh);
			}

			if (_elevationOptions.colliderOptions.addCollider)
			{
				var meshCollider = tile.Collider as MeshCollider;
				if (meshCollider)
				{
					meshCollider.sharedMesh = tile.MeshFilter.sharedMesh;
				}
			}
		}

		private void ResetToFlatMesh(UnityTile tile)
		{
			if (tile.MeshFilter.sharedMesh.vertexCount == 0)
			{
				CreateBaseMesh(tile, _elevationOptions.modificationOptions.sampleCount);
			}
			else
			{
				tile.MeshFilter.sharedMesh.GetVertices(_currentTileMeshData.Vertices);
				tile.MeshFilter.sharedMesh.GetNormals(_currentTileMeshData.Normals);

				_counter = _currentTileMeshData.Vertices.Count;
				for (int i = 0; i < _counter; i++)
				{
					_currentTileMeshData.Vertices[i] = new Vector3(
						_currentTileMeshData.Vertices[i].x,
						0,
						_currentTileMeshData.Vertices[i].z);
					_currentTileMeshData.Normals[i] = Mapbox.Unity.Constants.Math.Vector3Up;
				}

				tile.MeshFilter.sharedMesh.SetVertices(_currentTileMeshData.Vertices);
				tile.MeshFilter.sharedMesh.SetNormals(_currentTileMeshData.Normals);

				tile.MeshFilter.sharedMesh.RecalculateBounds();
			}
		}

		/// <summary>
		/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
		/// </summary>
		/// <param name="tileId">UnwrappedTileId of the tile being processed.</param>
		/// <param name="mesh"></param>
		private void FixStitches(UnwrappedTileId tileId, Vector3[] verts, Vector3[] normals)
		{
			var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
			var meshVertCount = verts.Length;

			if (_dataArrays.ContainsKey(tileId.North))
			{
				_targetVerts = _dataArrays[tileId.North].Vertices;
				_targetNormals = _dataArrays[tileId.North].Normals;

				for (int i = 0; i < _sampleCount; i++)
				{
					//just snapping the y because vertex pos is relative and we'll have to do tile pos + vertex pos for x&z otherwise
					verts[i] = new Vector3(
						verts[i].x,
						_targetVerts[meshVertCount - _sampleCount + i].y,
						verts[i].z);

					normals[i] = new Vector3(_targetNormals[meshVertCount - _sampleCount + i].x,
						_targetNormals[meshVertCount - _sampleCount + i].y,
						_targetNormals[meshVertCount - _sampleCount + i].z);
				}
			}

			if (_dataArrays.ContainsKey(tileId.South))
			{
				_targetVerts = _dataArrays[tileId.South].Vertices;
				_targetNormals = _dataArrays[tileId.South].Normals;

				for (int i = 0; i < _sampleCount; i++)
				{
					verts[meshVertCount - _sampleCount + i] = new Vector3(
						verts[meshVertCount - _sampleCount + i].x,
						_targetVerts[i].y,
						verts[meshVertCount - _sampleCount + i].z);

					normals[meshVertCount - _sampleCount + i] = new Vector3(
						_targetNormals[i].x,
						_targetNormals[i].y,
						_targetNormals[i].z);
				}
			}

			if (_dataArrays.ContainsKey(tileId.West))
			{
				_targetVerts = _dataArrays[tileId.West].Vertices;
				_targetNormals = _dataArrays[tileId.West].Normals;

				for (int i = 0; i < _sampleCount; i++)
				{
					verts[i * _sampleCount] = new Vector3(
						verts[i * _sampleCount].x,
						_targetVerts[i * _sampleCount + _sampleCount - 1].y,
						verts[i * _sampleCount].z);

					normals[i * _sampleCount] = new Vector3(
						_targetNormals[i * _sampleCount + _sampleCount - 1].x,
						_targetNormals[i * _sampleCount + _sampleCount - 1].y,
						_targetNormals[i * _sampleCount + _sampleCount - 1].z);
				}
			}

			if (_dataArrays.ContainsKey(tileId.East))
			{
				_targetVerts = _dataArrays[tileId.East].Vertices;
				_targetNormals = _dataArrays[tileId.East].Normals;

				for (int i = 0; i < _sampleCount; i++)
				{
					verts[i * _sampleCount + _sampleCount - 1] = new Vector3(
						verts[i * _sampleCount + _sampleCount - 1].x,
						_targetVerts[i * _sampleCount].y,
						verts[i * _sampleCount + _sampleCount - 1].z);

					normals[i * _sampleCount + _sampleCount - 1] = new Vector3(
						_targetNormals[i * _sampleCount].x,
						_targetNormals[i * _sampleCount].y,
						_targetNormals[i * _sampleCount].z);
				}
			}

			if (_dataArrays.ContainsKey(tileId.NorthWest))
			{
				_targetVerts = _dataArrays[tileId.NorthWest].Vertices;
				_targetNormals = _dataArrays[tileId.NorthWest].Normals;

				verts[0] = new Vector3(
					verts[0].x,
					_targetVerts[meshVertCount - 1].y,
					verts[0].z);

				normals[0] = new Vector3(
					_targetNormals[meshVertCount - 1].x,
					_targetNormals[meshVertCount - 1].y,
					_targetNormals[meshVertCount - 1].z);
			}

			if (_dataArrays.ContainsKey(tileId.NorthEast))
			{
				_targetVerts = _dataArrays[tileId.NorthEast].Vertices;
				_targetNormals = _dataArrays[tileId.NorthEast].Normals;

				verts[_sampleCount - 1] = new Vector3(
					verts[_sampleCount - 1].x,
					_targetVerts[meshVertCount - _sampleCount].y,
					verts[_sampleCount - 1].z);

				normals[_sampleCount - 1] = new Vector3(
					_targetNormals[meshVertCount - _sampleCount].x,
					_targetNormals[meshVertCount - _sampleCount].y,
					_targetNormals[meshVertCount - _sampleCount].z);
			}

			if (_dataArrays.ContainsKey(tileId.SouthWest))
			{
				_targetVerts = _dataArrays[tileId.SouthWest].Vertices;
				_targetNormals = _dataArrays[tileId.SouthWest].Normals;

				verts[meshVertCount - _sampleCount] = new Vector3(
					verts[meshVertCount - _sampleCount].x,
					_targetVerts[_sampleCount - 1].y,
					verts[meshVertCount - _sampleCount].z);

				normals[meshVertCount - _sampleCount] = new Vector3(
					_targetNormals[_sampleCount - 1].x,
					_targetNormals[_sampleCount - 1].y,
					_targetNormals[_sampleCount - 1].z);
			}

			if (_dataArrays.ContainsKey(tileId.SouthEast))
			{
				_targetVerts = _dataArrays[tileId.SouthEast].Vertices;
				_targetNormals = _dataArrays[tileId.SouthEast].Normals;

				verts[meshVertCount - 1] = new Vector3(
					verts[meshVertCount - 1].x,
					_targetVerts[0].y,
					verts[meshVertCount - 1].z);

				normals[meshVertCount - 1] = new Vector3(
					_targetNormals[0].x,
					_targetNormals[0].y,
					_targetNormals[0].z);
			}
		}

		#endregion
	}
}
