namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Utils;
	using Mapbox.Unity.Map;
	using System;

	/// <summary>
	/// Uses Mapbox Terrain api and creates terrain meshes.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Terrain Factory - Side Walls")]
	public class TerrainWithSideWallsFactory : AbstractTileFactory
	{
		[SerializeField]
		ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();
		Mesh _stitchTarget;

		protected Dictionary<UnwrappedTileId, Mesh> _meshData;
		private MeshData _currentTileMeshData;
		private MeshData _stitchTargetMeshData;

		private List<Vector3> _newVertexList;
		private List<Vector3> _newNormalList;
		private List<Vector2> _newUvList;
		private List<int> _newTriangleList;
		private Vector3 _newDir;
		private int _vertA, _vertB, _vertC;
		private int _counter;

		public string MapId
		{
			get
			{
				return _elevationOptions.sourceOptions.layerSource.Id;
			}

			set
			{
				_elevationOptions.sourceOptions.layerSource.Id = value;
			}
		}

		public override void SetOptions(LayerProperties options)
		{
			_elevationOptions = (ElevationLayerProperties)options;
		}

		internal override void OnInitialized()
		{
			_meshData = new Dictionary<UnwrappedTileId, Mesh>();
			_currentTileMeshData = new MeshData();
			_stitchTargetMeshData = new MeshData();
			var sampleCountSquare = _elevationOptions.modificationOptions.sampleCount * _elevationOptions.modificationOptions.sampleCount;
			_newVertexList = new List<Vector3>(sampleCountSquare);
			_newNormalList = new List<Vector3>(sampleCountSquare);
			_newUvList = new List<Vector2>(sampleCountSquare);
			_newTriangleList = new List<int>();
		}

		internal override void OnRegistered(UnityTile tile)
		{
			if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
			{
				tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
			}

			if (tile.MeshRenderer == null)
			{
				var renderer = tile.gameObject.AddComponent<MeshRenderer>();
				renderer.materials = new Material[2]
				{
					_elevationOptions.requiredOptions.baseMaterial,
					_elevationOptions.sideWallOptions.wallMaterial
				};
			}

			if (tile.MeshFilter == null)
			{
				tile.gameObject.AddComponent<MeshFilter>();
				CreateBaseMesh(tile);
			}

			if (_elevationOptions.requiredOptions.addCollider && tile.Collider == null)
			{
				tile.gameObject.AddComponent<MeshCollider>();
			}

			CreateTerrainHeight(tile);
		}

		private void CreateBaseMesh(UnityTile tile)
		{
			//TODO use arrays instead of lists
			_newVertexList.Clear();
			_newNormalList.Clear();
			_newUvList.Clear();
			_newTriangleList.Clear();

			var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
			for (float y = 0; y < _sampleCount; y++)
			{
				var yrat = y / (_sampleCount - 1);
				for (float x = 0; x < _sampleCount; x++)
				{
					var xrat = x / (_sampleCount - 1);

					var xx = Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, xrat);
					var yy = Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, yrat);

					_newVertexList.Add(new Vector3(
						(float)(xx - tile.Rect.Center.x) * tile.TileScale,
						0,
						(float)(yy - tile.Rect.Center.y) * tile.TileScale));
					_newNormalList.Add(Unity.Constants.Math.Vector3Up);
					_newUvList.Add(new Vector2(x * 1f / (_sampleCount - 1), 1 - (y * 1f / (_sampleCount - 1))));
				}
			}

			int vertA, vertB, vertC;
			for (int y = 0; y < _sampleCount - 1; y++)
			{
				for (int x = 0; x < _sampleCount - 1; x++)
				{
					vertA = (y * _sampleCount) + x;
					vertB = (y * _sampleCount) + x + _sampleCount + 1;
					vertC = (y * _sampleCount) + x + _sampleCount;
					_newTriangleList.Add(vertA);
					_newTriangleList.Add(vertB);
					_newTriangleList.Add(vertC);

					vertA = (y * _sampleCount) + x;
					vertB = (y * _sampleCount) + x + 1;
					vertC = (y * _sampleCount) + x + _sampleCount + 1;
					_newTriangleList.Add(vertA);
					_newTriangleList.Add(vertB);
					_newTriangleList.Add(vertC);
				}
			}

			var sideVertBase = _newVertexList.Count;

			var lastRow = (_sampleCount - 1) * _sampleCount;
			var baseTriList = new List<int>();
			for (int x = 0; x < _sampleCount; x++)
			{
				//side wall
				//024
				//135
				_newVertexList.Add(_newVertexList[x]);
				_newVertexList.Add(new Vector3(
					_newVertexList[x].x,
					-_elevationOptions.sideWallOptions.wallHeight,
					_newVertexList[x].z));
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Forward);
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Forward);
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 1));
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 0));

				//--- 

				_newVertexList.Add(_newVertexList[x * _sampleCount]);
				_newVertexList.Add(new Vector3(
						_newVertexList[x * _sampleCount].x,
					-_elevationOptions.sideWallOptions.wallHeight,
						_newVertexList[x * _sampleCount].z));
				_newNormalList.Add(Vector3.left);
				_newNormalList.Add(Vector3.left);
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 1));
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 0));

				//--- 

				_newVertexList.Add(_newVertexList[(x + 1) * _sampleCount - 1]);
				_newVertexList.Add(new Vector3(
						_newVertexList[(x + 1) * _sampleCount - 1].x,
					-_elevationOptions.sideWallOptions.wallHeight,
						_newVertexList[(x + 1) * _sampleCount - 1].z));
				_newNormalList.Add(Vector3.right);
				_newNormalList.Add(Vector3.right);
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 1));
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 0));

				//--- 

				_newVertexList.Add(_newVertexList[lastRow + x]);
				_newVertexList.Add(new Vector3(
					_newVertexList[lastRow + x].x,
					-_elevationOptions.sideWallOptions.wallHeight,
					_newVertexList[lastRow + x].z));
				_newNormalList.Add(Vector3.back);
				_newNormalList.Add(Vector3.back);
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 1));
				_newUvList.Add(new Vector2(_newUvList[x * _sampleCount].y, 0));

				if (x > 0)
				{
					baseTriList.Add(sideVertBase + 8 * x);
					baseTriList.Add(sideVertBase + 8 * x - 8);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1);

					baseTriList.Add(sideVertBase + 8 * x);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1);
					baseTriList.Add(sideVertBase + 8 * x + 1);

					//---

					baseTriList.Add(sideVertBase + 8 * x + 2);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1 + 2);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 2);

					baseTriList.Add(sideVertBase + 8 * x + 2);
					baseTriList.Add(sideVertBase + 8 * x + 1 + 2);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1 + 2);

					//---

					baseTriList.Add(sideVertBase + 8 * x + 4);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 4);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1 + 4);

					baseTriList.Add(sideVertBase + 8 * x + 4);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1 + 4);
					baseTriList.Add(sideVertBase + 8 * x + 1 + 4);

					//---

					baseTriList.Add(sideVertBase + 8 * x + 6);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1 + 6);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 6);

					baseTriList.Add(sideVertBase + 8 * x + 6);
					baseTriList.Add(sideVertBase + 8 * x + 1 + 6);
					baseTriList.Add(sideVertBase + 8 * x - 8 + 1 + 6);
				}
			}


			var mesh = tile.MeshFilter.mesh;
			mesh.SetVertices(_newVertexList);
			mesh.SetNormals(_newNormalList);
			mesh.SetUVs(0, _newUvList);
			mesh.subMeshCount = 2;
			mesh.SetTriangles(_newTriangleList, 0);
			mesh.SetTriangles(baseTriList, 1);
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			_meshData.Remove(tile.UnwrappedTileId);
		}

		/// <summary>
		/// Creates the non-flat terrain using a height multiplier
		/// </summary>
		/// <param name="tile"></param>
		// <param name="heightMultiplier">Multiplier for queried height value</param>
		private void CreateTerrainHeight(UnityTile tile)
		{
			tile.HeightDataState = TilePropertyState.Loading;
			var pngRasterTile = new RawPngRasterTile();

			tile.AddTile(pngRasterTile);
			Progress++;

			pngRasterTile.Initialize(_fileSource, tile.CanonicalTileId, MapId, () =>
			{
				if (tile == null)
				{
					return;
				}

				if (pngRasterTile.HasError)
				{
					OnErrorOccurred(new TileErrorEventArgs(tile.CanonicalTileId, pngRasterTile.GetType(), tile, pngRasterTile.Exceptions));
					tile.HeightDataState = TilePropertyState.Error;

					// Handle missing elevation from server (404)!
					// TODO: optimize this search!
					if (pngRasterTile.ExceptionsAsString.Contains("404"))
					{
						ResetToFlatMesh(tile);
					}
					Progress--;
					return;
				}

				tile.SetHeightData(pngRasterTile.Data, _elevationOptions.requiredOptions.exaggerationFactor);
				GenerateTerrainMesh(tile);
				Progress--;
			});
		}

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(TileErrorEventArgs e)
		{
			base.OnErrorOccurred(e);
		}

		/// <summary>
		/// Creates the non-flat terrain mesh, using a grid by defined resolution (_sampleCount). Vertex order goes right & up. Normals are calculated manually and UV map is fitted/stretched 1-1.
		/// Any additional scripts or logic, like MeshCollider or setting layer, can be done here.
		/// </summary>
		/// <param name="tile"></param>
		// <param name="heightMultiplier">Multiplier for queried height value</param>
		private void GenerateTerrainMesh(UnityTile tile)
		{
			tile.MeshFilter.mesh.GetVertices(_currentTileMeshData.Vertices);
			tile.MeshFilter.mesh.GetNormals(_currentTileMeshData.Normals);

			var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
			int sideStart = _sampleCount * _sampleCount;
			for (float y = 0; y < _sampleCount; y++)
			{
				for (float x = 0; x < _sampleCount; x++)
				{
					_currentTileMeshData.Vertices[(int)(y * _sampleCount + x)] = new Vector3(
						_currentTileMeshData.Vertices[(int)(y * _sampleCount + x)].x,
						tile.QueryHeightData(x / (_sampleCount - 1), 1 - y / (_sampleCount - 1)),
						_currentTileMeshData.Vertices[(int)(y * _sampleCount + x)].z);
					_currentTileMeshData.Normals[(int)(y * _sampleCount + x)] = Unity.Constants.Math.Vector3Zero;

					if (y == 0)
					{
						_currentTileMeshData.Vertices[(int)(sideStart + 8 * x)] = _currentTileMeshData.Vertices[(int)(y * _sampleCount + x)];
					}
					else if (y == _sampleCount - 1)
					{
						_currentTileMeshData.Vertices[(int)(sideStart + 8 * x + 6)] = _currentTileMeshData.Vertices[(int)(y * _sampleCount + x)];
					}

					if (x == 0)
					{
						_currentTileMeshData.Vertices[(int)(sideStart + 8 * y + 2)] = _currentTileMeshData.Vertices[(int)(y * _sampleCount + x)];
					}
					else if (x == _sampleCount - 1)
					{
						_currentTileMeshData.Vertices[(int)(sideStart + 8 * y + 4)] = _currentTileMeshData.Vertices[(int)(y * _sampleCount + x)];
					}
				}
			}

			tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);

			for (int y = 0; y < _sampleCount - 1; y++)
			{
				for (int x = 0; x < _sampleCount - 1; x++)
				{
					_vertA = (y * _sampleCount) + x;
					_vertB = (y * _sampleCount) + x + _sampleCount + 1;
					_vertC = (y * _sampleCount) + x + _sampleCount;
					_newDir = Vector3.Cross(_currentTileMeshData.Vertices[_vertB] - _currentTileMeshData.Vertices[_vertA], _currentTileMeshData.Vertices[_vertC] - _currentTileMeshData.Vertices[_vertA]);
					_currentTileMeshData.Normals[_vertA] += _newDir;
					_currentTileMeshData.Normals[_vertB] += _newDir;
					_currentTileMeshData.Normals[_vertC] += _newDir;

					_vertA = (y * _sampleCount) + x;
					_vertB = (y * _sampleCount) + x + 1;
					_vertC = (y * _sampleCount) + x + _sampleCount + 1;
					_newDir = Vector3.Cross(_currentTileMeshData.Vertices[_vertB] - _currentTileMeshData.Vertices[_vertA], _currentTileMeshData.Vertices[_vertC] - _currentTileMeshData.Vertices[_vertA]);
					_currentTileMeshData.Normals[_vertA] += _newDir;
					_currentTileMeshData.Normals[_vertB] += _newDir;
					_currentTileMeshData.Normals[_vertC] += _newDir;
				}
			}

			FixStitches(tile.UnwrappedTileId, _currentTileMeshData);

			tile.MeshFilter.mesh.SetNormals(_currentTileMeshData.Normals);
			tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);

			tile.MeshFilter.mesh.RecalculateBounds();

			if (!_meshData.ContainsKey(tile.UnwrappedTileId))
			{
				_meshData.Add(tile.UnwrappedTileId, tile.MeshFilter.mesh);
			}

			if (_elevationOptions.requiredOptions.addCollider)
			{
				var meshCollider = tile.Collider as MeshCollider;
				if (meshCollider)
				{
					meshCollider.sharedMesh = tile.MeshFilter.mesh;
				}
			}
		}

		private void ResetToFlatMesh(UnityTile tile)
		{
			tile.MeshFilter.mesh.GetVertices(_currentTileMeshData.Vertices);
			tile.MeshFilter.mesh.GetNormals(_currentTileMeshData.Normals);

			_counter = _currentTileMeshData.Vertices.Count;
			for (int i = 0; i < _counter; i++)
			{
				_currentTileMeshData.Vertices[i] = new Vector3(
					_currentTileMeshData.Vertices[i].x,
					0,
					_currentTileMeshData.Vertices[i].z);
				_currentTileMeshData.Normals[i] = Unity.Constants.Math.Vector3Up;
			}

			tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);
			tile.MeshFilter.mesh.SetNormals(_currentTileMeshData.Normals);

			tile.MeshFilter.mesh.RecalculateBounds();
		}

		/// <summary>
		/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
		/// </summary>
		/// <param name="tileId"></param>
		/// <param name="mesh"></param>
		private void FixStitches(UnwrappedTileId tileId, MeshData mesh)
		{
			var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
			var meshVertCount = _sampleCount * _sampleCount;
			_stitchTarget = null;
			_meshData.TryGetValue(tileId.North, out _stitchTarget);
			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);
				for (int i = 0; i < _sampleCount; i++)
				{
					//just snapping the y because vertex pos is relative and we'll have to do tile pos + vertex pos for x&z otherwise
					mesh.Vertices[i] = new Vector3(
						mesh.Vertices[i].x,
						_stitchTargetMeshData.Vertices[meshVertCount - _sampleCount + i].y,
						mesh.Vertices[i].z);
					mesh.Vertices[meshVertCount + (8 * i)] = mesh.Vertices[i];

					mesh.Normals[i] = new Vector3(_stitchTargetMeshData.Normals[meshVertCount - _sampleCount + i].x,
						_stitchTargetMeshData.Normals[meshVertCount - _sampleCount + i].y,
						_stitchTargetMeshData.Normals[meshVertCount - _sampleCount + i].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.South, out _stitchTarget);
			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);
				for (int i = 0; i < _sampleCount; i++)
				{
					mesh.Vertices[meshVertCount - _sampleCount + i] = new Vector3(
						mesh.Vertices[meshVertCount - _sampleCount + i].x,
						_stitchTargetMeshData.Vertices[i].y,
						mesh.Vertices[meshVertCount - _sampleCount + i].z);
					mesh.Vertices[meshVertCount + 6 + (8 * i)] = mesh.Vertices[meshVertCount - _sampleCount + i];

					mesh.Normals[meshVertCount - _sampleCount + i] = new Vector3(
						_stitchTargetMeshData.Normals[i].x,
						_stitchTargetMeshData.Normals[i].y,
						_stitchTargetMeshData.Normals[i].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.West, out _stitchTarget);
			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				for (int i = 0; i < _sampleCount; i++)
				{
					mesh.Vertices[i * _sampleCount] = new Vector3(
						mesh.Vertices[i * _sampleCount].x,
						_stitchTargetMeshData.Vertices[i * _sampleCount + _sampleCount - 1].y,
						mesh.Vertices[i * _sampleCount].z);
					mesh.Vertices[meshVertCount + 2 + (8 * i)] = mesh.Vertices[i * _sampleCount];

					mesh.Normals[i * _sampleCount] = new Vector3(
						_stitchTargetMeshData.Normals[i * _sampleCount + _sampleCount - 1].x,
						_stitchTargetMeshData.Normals[i * _sampleCount + _sampleCount - 1].y,
						_stitchTargetMeshData.Normals[i * _sampleCount + _sampleCount - 1].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.East, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				for (int i = 0; i < _sampleCount; i++)
				{
					mesh.Vertices[i * _sampleCount + _sampleCount - 1] = new Vector3(
						mesh.Vertices[i * _sampleCount + _sampleCount - 1].x,
						_stitchTargetMeshData.Vertices[i * _sampleCount].y,
						mesh.Vertices[i * _sampleCount + _sampleCount - 1].z);
					mesh.Vertices[meshVertCount + 4 + (8 * i)] = mesh.Vertices[i * _sampleCount + _sampleCount - 1];

					mesh.Normals[i * _sampleCount + _sampleCount - 1] = new Vector3(
						_stitchTargetMeshData.Normals[i * _sampleCount].x,
						_stitchTargetMeshData.Normals[i * _sampleCount].y,
						_stitchTargetMeshData.Normals[i * _sampleCount].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.NorthWest, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				mesh.Vertices[0] = new Vector3(
					mesh.Vertices[0].x,
					_stitchTargetMeshData.Vertices[meshVertCount - 1].y,
					mesh.Vertices[0].z);

				mesh.Normals[0] = new Vector3(
					_stitchTargetMeshData.Normals[meshVertCount - 1].x,
					_stitchTargetMeshData.Normals[meshVertCount - 1].y,
					_stitchTargetMeshData.Normals[meshVertCount - 1].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.NorthEast, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				mesh.Vertices[_sampleCount - 1] = new Vector3(
					mesh.Vertices[_sampleCount - 1].x,
					_stitchTargetMeshData.Vertices[meshVertCount - _sampleCount].y,
					mesh.Vertices[_sampleCount - 1].z);

				mesh.Normals[_sampleCount - 1] = new Vector3(
					_stitchTargetMeshData.Normals[meshVertCount - _sampleCount].x,
					_stitchTargetMeshData.Normals[meshVertCount - _sampleCount].y,
					_stitchTargetMeshData.Normals[meshVertCount - _sampleCount].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.SouthWest, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				mesh.Vertices[meshVertCount - _sampleCount] = new Vector3(
					mesh.Vertices[meshVertCount - _sampleCount].x,
					_stitchTargetMeshData.Vertices[_sampleCount - 1].y,
					mesh.Vertices[meshVertCount - _sampleCount].z);

				mesh.Normals[meshVertCount - _sampleCount] = new Vector3(
					_stitchTargetMeshData.Normals[_sampleCount - 1].x,
					_stitchTargetMeshData.Normals[_sampleCount - 1].y,
					_stitchTargetMeshData.Normals[_sampleCount - 1].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.SouthEast, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);
				mesh.Vertices[meshVertCount - 1] = new Vector3(
					mesh.Vertices[meshVertCount - 1].x,
					_stitchTargetMeshData.Vertices[0].y,
					mesh.Vertices[meshVertCount - 1].z);

				mesh.Normals[meshVertCount - 1] = new Vector3(
					_stitchTargetMeshData.Normals[0].x,
					_stitchTargetMeshData.Normals[0].y,
					_stitchTargetMeshData.Normals[0].z);
			}
		}
	}
}