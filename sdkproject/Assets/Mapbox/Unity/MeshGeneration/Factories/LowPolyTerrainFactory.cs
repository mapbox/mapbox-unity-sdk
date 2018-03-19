namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Utils;
	using System;

	/// <summary>
	/// Uses Mapbox Terrain api and creates terrain meshes.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Terrain Factory - Low Poly")]
	public class LowPolyTerrainFactory : AbstractTileFactory
	{
		[SerializeField]
		private Material _baseMaterial = null;
		[SerializeField]
		private MapIdType _mapIdType;

		[SerializeField]
#pragma warning disable 0414
		private string _customMapId = "mapbox.terrain-rgb";
#pragma warning restore 0414

		[SerializeField]
		private string _mapId = "";
		[SerializeField]
		private float _heightModifier = 2f;
		[SerializeField]
		private int _sampleCount = 10;
		[SerializeField]
		private bool _addCollider = false;
		[SerializeField]
		private bool _addToLayer = false;
		[SerializeField]
		private int _layerId = 0;
		[SerializeField]
		bool _useRelativeHeight = true;

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
				return _mapId;
			}

			set
			{
				_mapId = value;
			}
		}

		internal override void OnInitialized()
		{
			_meshData = new Dictionary<UnwrappedTileId, Mesh>();
			_currentTileMeshData = new MeshData();
			_stitchTargetMeshData = new MeshData();
			_newVertexList = new List<Vector3>(_sampleCount * _sampleCount);
			_newNormalList = new List<Vector3>(_sampleCount * _sampleCount);
			_newUvList = new List<Vector2>(_sampleCount * _sampleCount);
			_newTriangleList = new List<int>();
		}

		internal override void OnRegistered(UnityTile tile)
		{
			if (_addToLayer && tile.gameObject.layer != _layerId)
			{
				tile.gameObject.layer = _layerId;
			}

			if (tile.MeshRenderer == null)
			{
				var renderer = tile.gameObject.AddComponent<MeshRenderer>();
				renderer.material = _baseMaterial;
			}

			if (tile.MeshFilter == null)
			{
				tile.gameObject.AddComponent<MeshFilter>();
				CreateBaseMesh(tile);
			}

			if (_addCollider && tile.Collider == null)
			{
				tile.gameObject.AddComponent<MeshCollider>();
			}

			CreateTerrainHeight(tile);
		}

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(TileErrorEventArgs e)
		{
			base.OnErrorOccurred(e);
		}

		private void CreateBaseMesh(UnityTile tile)
		{
			//TODO use arrays instead of lists
			_newVertexList.Clear();
			_newNormalList.Clear();
			_newUvList.Clear();
			_newTriangleList.Clear();

			var cap = (_sampleCount - 1);
			for (float y = 0; y < cap; y++)
			{
				for (float x = 0; x < cap; x++)
				{
					var x1 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, x / cap) - tile.Rect.Center.x);
					var y1 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, y / cap) - tile.Rect.Center.y);
					var x2 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, (x + 1) / cap) - tile.Rect.Center.x);
					var y2 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, (y + 1) / cap) - tile.Rect.Center.y);

					var triStart = _newVertexList.Count;
					_newVertexList.Add(new Vector3(x1, 0, y1));
					_newVertexList.Add(new Vector3(x2, 0, y1));
					_newVertexList.Add(new Vector3(x1, 0, y2));
					//--
					_newVertexList.Add(new Vector3(x2, 0, y1));
					_newVertexList.Add(new Vector3(x2, 0, y2));
					_newVertexList.Add(new Vector3(x1, 0, y2));

					_newNormalList.Add(Unity.Constants.Math.Vector3Up);
					_newNormalList.Add(Unity.Constants.Math.Vector3Up);
					_newNormalList.Add(Unity.Constants.Math.Vector3Up);
					//--
					_newNormalList.Add(Unity.Constants.Math.Vector3Up);
					_newNormalList.Add(Unity.Constants.Math.Vector3Up);
					_newNormalList.Add(Unity.Constants.Math.Vector3Up);


					_newUvList.Add(new Vector2(x / cap, 1 - y / cap));
					_newUvList.Add(new Vector2((x + 1) / cap, 1 - y / cap));
					_newUvList.Add(new Vector2(x / cap, 1 - (y + 1) / cap));
					//--
					_newUvList.Add(new Vector2((x + 1) / cap, 1 - y / cap));
					_newUvList.Add(new Vector2((x + 1) / cap, 1 - (y + 1) / cap));
					_newUvList.Add(new Vector2(x / cap, 1 - (y + 1) / cap));

					_newTriangleList.Add(triStart);
					_newTriangleList.Add(triStart + 1);
					_newTriangleList.Add(triStart + 2);
					//--
					_newTriangleList.Add(triStart + 3);
					_newTriangleList.Add(triStart + 4);
					_newTriangleList.Add(triStart + 5);
				}
			}


			var mesh = tile.MeshFilter.mesh;
			mesh.SetVertices(_newVertexList);
			mesh.SetNormals(_newNormalList);
			mesh.SetUVs(0, _newUvList);
			mesh.SetTriangles(_newTriangleList, 0);
			mesh.RecalculateBounds();
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			_meshData.Remove(tile.UnwrappedTileId);
		}

		/// <summary>
		/// Creates the non-flat terrain using a height multiplier
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="heightMultiplier">Multiplier for queried height value</param>
		private void CreateTerrainHeight(UnityTile tile)
		{
			tile.HeightDataState = TilePropertyState.Loading;
			var pngRasterTile = new RawPngRasterTile();

			tile.AddTile(pngRasterTile);
			Progress++;

			pngRasterTile.Initialize(_fileSource, tile.CanonicalTileId, _mapId, () =>
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

				tile.SetHeightData(pngRasterTile.Data, _heightModifier, _useRelativeHeight);
				GenerateTerrainMesh(tile);
				Progress--;
			});
		}

		/// <summary>
		/// Creates the non-flat terrain mesh, using a grid by defined resolution (_sampleCount). Vertex order goes right & up. Normals are calculated manually and UV map is fitted/stretched 1-1.
		/// Any additional scripts or logic, like MeshCollider or setting layer, can be done here.
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="heightMultiplier">Multiplier for queried height value</param>
		private void GenerateTerrainMesh(UnityTile tile)
		{
			tile.MeshFilter.mesh.GetVertices(_currentTileMeshData.Vertices);
			tile.MeshFilter.mesh.GetNormals(_currentTileMeshData.Normals);

			var cap = (_sampleCount - 1);
			for (float y = 0; y < cap; y++)
			{
				for (float x = 0; x < cap; x++)
				{
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6] = new Vector3(
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6].x,
						tile.QueryHeightData(x / cap, 1 - y / cap),
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6].z);

					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1] = new Vector3(
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1].x,
						tile.QueryHeightData((x + 1) / cap, 1 - y / cap),
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1].z);

					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2] = new Vector3(
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2].x,
						tile.QueryHeightData(x / cap, 1 - (y + 1) / cap),
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2].z);

					//-- 

					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3] = new Vector3(
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3].x,
						tile.QueryHeightData((x + 1) / cap, 1 - y / cap),
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3].z);

					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4] = new Vector3(
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4].x,
						tile.QueryHeightData((x + 1) / cap, 1 - (y + 1) / cap),
						_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4].z);

					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5] = new Vector3(
					   _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5].x,
					   tile.QueryHeightData(x / cap, 1 - (y + 1) / cap),
					   _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5].z);



					_newDir = Vector3.Cross(_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6], _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6]);
					_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 0] = _newDir;
					_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 1] = _newDir;
					_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 2] = _newDir;
					//--
					_newDir = Vector3.Cross(_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3], _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3]);
					_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 3] = _newDir;
					_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 4] = _newDir;
					_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 5] = _newDir;
				}
			}
			FixStitches(tile.UnwrappedTileId, _currentTileMeshData);
			tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);
			tile.MeshFilter.mesh.SetNormals(_currentTileMeshData.Normals);
			tile.MeshFilter.mesh.RecalculateBounds();

			if (!_meshData.ContainsKey(tile.UnwrappedTileId))
			{
				_meshData.Add(tile.UnwrappedTileId, tile.MeshFilter.mesh);
			}

			if (_addCollider)
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
		}

		/// <summary>
		/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="mesh"></param>
		private void FixStitches(UnwrappedTileId tileId, MeshData mesh)
		{
			var meshVertCount = mesh.Vertices.Count;
			_stitchTarget = null;
			_meshData.TryGetValue(tileId.North, out _stitchTarget);
			var cap = _sampleCount - 1;
			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				for (int i = 0; i < cap; i++)
				{
					mesh.Vertices[6 * i] = new Vector3(
						mesh.Vertices[6 * i].x,
						_stitchTargetMeshData.Vertices[6 * cap * (cap - 1) + 6 * i + 2].y,
						mesh.Vertices[6 * i].z);
					mesh.Vertices[6 * i + 1] = new Vector3(
						mesh.Vertices[6 * i + 1].x,
						_stitchTargetMeshData.Vertices[6 * cap * (cap - 1) + 6 * i + 4].y,
						mesh.Vertices[6 * i + 1].z);
					mesh.Vertices[6 * i + 3] = new Vector3(
						mesh.Vertices[6 * i + 3].x,
						_stitchTargetMeshData.Vertices[6 * cap * (cap - 1) + 6 * i + 4].y,
						mesh.Vertices[6 * i + 3].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.South, out _stitchTarget);
			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				for (int i = 0; i < cap; i++)
				{
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 2] = new Vector3(
						mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 2].x,
						_stitchTargetMeshData.Vertices[6 * i].y,
						mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 2].z);
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 5] = new Vector3(
						mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 5].x,
						_stitchTargetMeshData.Vertices[6 * i].y,
						mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 5].z);
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 4] = new Vector3(
						mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 4].x,
						_stitchTargetMeshData.Vertices[6 * i + 3].y,
						mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 4].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.West, out _stitchTarget);
			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				for (int i = 0; i < cap; i++)
				{
					mesh.Vertices[6 * cap * i] = new Vector3(
						mesh.Vertices[6 * cap * i].x,
						_stitchTargetMeshData.Vertices[6 * cap * i + 6 * cap - 5].y,
						mesh.Vertices[6 * cap * i].z);

					mesh.Vertices[6 * cap * i + 2] = new Vector3(
						mesh.Vertices[6 * cap * i + 2].x,
						_stitchTargetMeshData.Vertices[6 * cap * i + 6 * cap - 2].y,
						mesh.Vertices[6 * cap * i + 2].z);

					mesh.Vertices[6 * cap * i + 5] = new Vector3(
						mesh.Vertices[6 * cap * i + 5].x,
						_stitchTargetMeshData.Vertices[6 * cap * i + 6 * cap - 2].y,
						mesh.Vertices[6 * cap * i + 5].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.East, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				for (int i = 0; i < cap; i++)
				{
					mesh.Vertices[6 * cap * i + 6 * cap - 5] = new Vector3(
						mesh.Vertices[6 * cap * i + 6 * cap - 5].x,
						_stitchTargetMeshData.Vertices[6 * cap * i].y,
						mesh.Vertices[6 * cap * i + 6 * cap - 5].z);

					mesh.Vertices[6 * cap * i + 6 * cap - 3] = new Vector3(
						mesh.Vertices[6 * cap * i + 6 * cap - 3].x,
						_stitchTargetMeshData.Vertices[6 * cap * i].y,
						mesh.Vertices[6 * cap * i + 6 * cap - 3].z);

					mesh.Vertices[6 * cap * i + 6 * cap - 2] = new Vector3(
						mesh.Vertices[6 * cap * i + 6 * cap - 2].x,
						_stitchTargetMeshData.Vertices[6 * cap * i + 5].y,
						mesh.Vertices[6 * cap * i + 6 * cap - 2].z);
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
					_stitchTargetMeshData.Vertices[meshVertCount - 2].y,
					mesh.Vertices[0].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.NorthEast, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				mesh.Vertices[6 * cap - 5] = new Vector3(
					mesh.Vertices[6 * cap - 5].x,
					_stitchTargetMeshData.Vertices[6 * (cap - 1) * cap + 2].y,
					mesh.Vertices[6 * cap - 5].z);

				mesh.Vertices[6 * cap - 3] = new Vector3(
					mesh.Vertices[6 * cap - 3].x,
					_stitchTargetMeshData.Vertices[6 * (cap - 1) * cap + 2].y,
					mesh.Vertices[6 * cap - 3].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.SouthWest, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				mesh.Vertices[6 * (cap - 1) * cap + 2] = new Vector3(
					mesh.Vertices[6 * (cap - 1) * cap + 2].x,
					_stitchTargetMeshData.Vertices[6 * cap - 5].y,
					mesh.Vertices[6 * (cap - 1) * cap + 2].z);

				mesh.Vertices[6 * (cap - 1) * cap + 5] = new Vector3(
					mesh.Vertices[6 * (cap - 1) * cap + 5].x,
					_stitchTargetMeshData.Vertices[6 * cap - 5].y,
					mesh.Vertices[6 * (cap - 1) * cap + 5].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.SouthEast, out _stitchTarget);

			if (_stitchTarget != null)
			{
				_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
				_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

				mesh.Vertices[6 * cap * cap - 2] = new Vector3(
					mesh.Vertices[6 * cap * cap - 2].x,
					_stitchTargetMeshData.Vertices[0].y,
					mesh.Vertices[6 * cap * cap - 2].z);
			}
		}
	}
}