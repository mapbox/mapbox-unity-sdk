namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Platform;
	using Mapbox.Unity.Utilities;
	using Utils;

	public enum TerrainGenerationType
	{
		Flat,
		Height
	}

	public enum MapIdType
	{
		Standard,
		Custom
	}

	/// <summary>
	/// Uses Mapbox Terrain api and creates terrain meshes.
	/// </summary>
	// TODO: split into flat and non-flat
	[CreateAssetMenu(menuName = "Mapbox/Factories/Terrain Factory")]
	public class TerrainFactory : AbstractTileFactory
	{
		[SerializeField]
		private TerrainGenerationType _generationType;
		[SerializeField]
		private Material _baseMaterial;

		[SerializeField]
		private MapIdType _mapIdType;
		[SerializeField]
		private string _customMapId = "mapbox.terrain-rgb";
		[SerializeField]
		private string _mapId = "";
		[SerializeField]
		private float _heightModifier = 1f;
		[SerializeField]
		private int _sampleCount = 40;
		[SerializeField]
		private bool _addCollider = false;
		[SerializeField]
		private bool _addToLayer = false;
		[SerializeField]
		private int _layerId = 0;

		MeshData _stitchTarget;

		Mesh _cachedQuad;

		protected Dictionary<CanonicalTileId, MeshData> _meshData;

		/// <summary>
		/// Clears the mesh data and re-runs the terrain creation procedure using current settings. Clearing the old mesh data is important as terrain stitching function checks if the data exists or not.
		/// </summary>
		// TODO: come back to this
		//public override void Update()
		//{
		//    base.Update();
		//    foreach (var tile in _tiles.Values)
		//    {
		//        tile.MeshData = null;
		//    }
		//    foreach (var tile in _tiles.Values)
		//    {
		//        Run(tile);
		//    }
		//}

		internal override void OnInitialized()
		{
			_meshData = new Dictionary<CanonicalTileId, MeshData>();
		}

		internal override void OnRegistered(UnityTile tile)
		{
			_meshData.Add(tile.CanonicalTileId, null);

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
			}

			if (_generationType == TerrainGenerationType.Flat)
			{
				CreateFlatMesh(tile);
			}
			else
			{
				CreateTerrainHeight(tile);
			}
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			_meshData.Remove(tile.CanonicalTileId);
		}

		/// <summary>
		/// Creates the non-flat terrain using a height multiplier
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="heightMultiplier">Multiplier for queried height value</param>
		private void CreateTerrainHeight(UnityTile tile)
		{
			var parameters = new Tile.Parameters
			{
				Fs = _fileSource,
				Id = tile.CanonicalTileId,
				MapId = _mapId
			};

			tile.HeightDataState = TilePropertyState.Loading;

			var pngRasterTile = new RawPngRasterTile();

			_tiles.Add(tile, pngRasterTile);
			pngRasterTile.Initialize(parameters, () =>
			{
				// HACK: we need to check state because a cancel could have happened immediately following a response.
				if (pngRasterTile.HasError || pngRasterTile.CurrentState == Tile.State.Canceled)
				{
					tile.HeightDataState = TilePropertyState.Error;

					// HACK: handle missing tile from server (404)!
					CreateFlatMesh(tile);
					return;
				}
				_tiles.Remove(tile);

				tile.SetHeightData(pngRasterTile.Data, _heightModifier);
				GenerateTerrainMesh(tile);
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
			var mesh = new MeshData();
			_meshData[tile.CanonicalTileId] = mesh;

			mesh.Vertices = new List<Vector3>(_sampleCount * _sampleCount);
			var step = 1f / (_sampleCount - 1);
			for (float y = 0; y < _sampleCount; y++)
			{
				var yrat = y / (_sampleCount - 1);
				for (float x = 0; x < _sampleCount; x++)
				{
					var xrat = x / (_sampleCount - 1);

					var xx = Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, xrat);
					var yy = Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, yrat);

					mesh.Vertices.Add(new Vector3(
						(float)(xx - tile.Rect.Center.x),
						tile.QueryHeightData(xrat, 1 - yrat),
						(float)(yy - tile.Rect.Center.y)));
					mesh.Normals.Add(Unity.Constants.Math.Vector3Up);
					mesh.UV[0].Add(new Vector2(x * step, 1 - (y * step)));
				}
			}

			var trilist = new List<int>();
			var dir = Unity.Constants.Math.Vector3Zero;
			int vertA, vertB, vertC;
			for (int y = 0; y < _sampleCount - 1; y++)
			{
				for (int x = 0; x < _sampleCount - 1; x++)
				{
					vertA = (y * _sampleCount) + x;
					vertB = (y * _sampleCount) + x + _sampleCount + 1;
					vertC = (y * _sampleCount) + x + _sampleCount;
					trilist.Add(vertA);
					trilist.Add(vertB);
					trilist.Add(vertC);
					dir = Vector3.Cross(mesh.Vertices[vertB] - mesh.Vertices[vertA], mesh.Vertices[vertC] - mesh.Vertices[vertA]);
					mesh.Normals[vertA] += dir;
					mesh.Normals[vertB] += dir;
					mesh.Normals[vertC] += dir;

					vertA = (y * _sampleCount) + x;
					vertB = (y * _sampleCount) + x + 1;
					vertC = (y * _sampleCount) + x + _sampleCount + 1;
					trilist.Add(vertA);
					trilist.Add(vertB);
					trilist.Add(vertC);
					dir = Vector3.Cross(mesh.Vertices[vertB] - mesh.Vertices[vertA], mesh.Vertices[vertC] - mesh.Vertices[vertA]);
					mesh.Normals[vertA] += dir;
					mesh.Normals[vertB] += dir;
					mesh.Normals[vertC] += dir;
				}
			}
			mesh.Triangles.Add(trilist);

			for (int i = 0; i < mesh.Vertices.Count; i++)
			{
				mesh.Normals[i].Normalize();
			}

			FixStitches(tile.CanonicalTileId, mesh);

			// FIXME: recycling tiles that were once quads causes issues!
			// Don't leak the mesh, just reuse it.

			var unityMesh = tile.MeshFilter.sharedMesh ?? new Mesh();
			unityMesh.SetVertices(mesh.Vertices);
			unityMesh.SetUVs(0, mesh.UV[0]);
			unityMesh.SetNormals(mesh.Normals);
			unityMesh.SetTriangles(mesh.Triangles[0], 0);
			unityMesh.RecalculateBounds();

			// TODO: should we recalculate normals here, too?
			tile.MeshFilter.sharedMesh = unityMesh;

			if (_addCollider)
			{
				if (tile.Collider == null)
				{
					tile.gameObject.AddComponent<MeshCollider>();
				}
				else
				{
					// Reuse the collider.
					tile.Collider.sharedMesh = unityMesh;
				}
			}
		}

		/// <summary>
		/// Creates a basic quad to be used as flat base mesh. Normals are up and UV is fitted to quad.
		/// A quad is enough for basic usage but the resolution should be increased if any mesh deformation, like bending, to work.
		/// </summary>
		/// <param name="tile"></param>
		private void CreateFlatMesh(UnityTile tile)
		{
			Mesh unityMesh = null;
			if (_cachedQuad != null)
			{
				unityMesh = _cachedQuad;
			}
			else
			{
				unityMesh = new Mesh();
				var verts = new Vector3[4];

				verts[0] = ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
				verts[2] = (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
				verts[1] = (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
				verts[3] = ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());

				unityMesh.vertices = verts;
				var trilist = new int[6] { 0, 1, 2, 1, 3, 2 };
				unityMesh.SetTriangles(trilist, 0);
				var uvlist = new Vector2[4]
				{
				new Vector2(0,1),
				new Vector2(1,1),
				new Vector2(0,0),
				new Vector2(1,0)
				};

				unityMesh.uv = uvlist;
				unityMesh.RecalculateNormals();

				// HACK: comment this out if you see rendering errors related to terrain!
				_cachedQuad = unityMesh;
			}

			tile.MeshFilter.sharedMesh = unityMesh;

			if (_addCollider)
			{
				if (tile.Collider == null)
				{
					tile.gameObject.AddComponent<MeshCollider>();
				}
				else
				{
					// Reuse the collider.
					tile.Collider.sharedMesh = unityMesh;
				}
			}
		}

		/// <summary>
		/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="mesh"></param>
		private void FixStitches(CanonicalTileId tileId, MeshData mesh)
		{
			var meshVertCount = mesh.Vertices.Count;
			var targetVertCount = 0;

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.South, out _stitchTarget);
			if (_stitchTarget != null)
			{
				for (int i = 0; i < _sampleCount; i++)
				{
					//just snapping the y because vertex pos is relative and we'll have to do tile pos + vertex pos for x&z otherwise
					mesh.Vertices[i] = new Vector3(
						mesh.Vertices[i].x,
						_stitchTarget.Vertices[meshVertCount - _sampleCount + i].y,
						mesh.Vertices[i].z);

					mesh.Normals[i] = new Vector3(_stitchTarget.Normals[meshVertCount - _sampleCount + i].x,
						_stitchTarget.Normals[meshVertCount - _sampleCount + i].y,
						_stitchTarget.Normals[meshVertCount - _sampleCount + i].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.North, out _stitchTarget);
			if (_stitchTarget != null)
			{
				for (int i = 0; i < _sampleCount; i++)
				{
					mesh.Vertices[meshVertCount - _sampleCount + i] = new Vector3(
						mesh.Vertices[meshVertCount - _sampleCount + i].x,
						_stitchTarget.Vertices[i].y,
						mesh.Vertices[meshVertCount - _sampleCount + i].z);

					mesh.Normals[meshVertCount - _sampleCount + i] = new Vector3(
						_stitchTarget.Normals[i].x,
						_stitchTarget.Normals[i].y,
						_stitchTarget.Normals[i].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.West, out _stitchTarget);
			if (_stitchTarget != null)
			{
				for (int i = 0; i < _sampleCount; i++)
				{
					mesh.Vertices[i * _sampleCount] = new Vector3(
						mesh.Vertices[i * _sampleCount].x,
						_stitchTarget.Vertices[i * _sampleCount + _sampleCount - 1].y,
						mesh.Vertices[i * _sampleCount].z);

					mesh.Normals[i * _sampleCount] = new Vector3(
						_stitchTarget.Normals[i * _sampleCount + _sampleCount - 1].x,
						_stitchTarget.Normals[i * _sampleCount + _sampleCount - 1].y,
						_stitchTarget.Normals[i * _sampleCount + _sampleCount - 1].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.East, out _stitchTarget);
			if (_stitchTarget != null)
			{
				for (int i = 0; i < _sampleCount; i++)
				{
					mesh.Vertices[i * _sampleCount + _sampleCount - 1] = new Vector3(
						mesh.Vertices[i * _sampleCount + _sampleCount - 1].x,
						_stitchTarget.Vertices[i * _sampleCount].y,
						mesh.Vertices[i * _sampleCount + _sampleCount - 1].z);

					mesh.Normals[i * _sampleCount + _sampleCount - 1] = new Vector3(
						_stitchTarget.Normals[i * _sampleCount].x,
						_stitchTarget.Normals[i * _sampleCount].y,
						_stitchTarget.Normals[i * _sampleCount].z);
				}
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.SouthWest, out _stitchTarget);
			if (_stitchTarget != null)
			{
				targetVertCount = _stitchTarget.Vertices.Count;
				mesh.Vertices[0] = new Vector3(
					mesh.Vertices[0].x,
					_stitchTarget.Vertices[targetVertCount - 1].y,
					mesh.Vertices[0].z);

				mesh.Normals[0] = new Vector3(
					_stitchTarget.Normals[targetVertCount - 1].x,
					_stitchTarget.Normals[targetVertCount - 1].y,
					_stitchTarget.Normals[targetVertCount - 1].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.SouthEast, out _stitchTarget);
			if (_stitchTarget != null)
			{
				targetVertCount = _stitchTarget.Vertices.Count;
				mesh.Vertices[_sampleCount - 1] = new Vector3(
					mesh.Vertices[_sampleCount - 1].x,
					_stitchTarget.Vertices[targetVertCount - _sampleCount].y,
					mesh.Vertices[_sampleCount - 1].z);

				mesh.Normals[_sampleCount - 1] = new Vector3(
					_stitchTarget.Normals[targetVertCount - _sampleCount].x,
					_stitchTarget.Normals[targetVertCount - _sampleCount].y,
					_stitchTarget.Normals[targetVertCount - _sampleCount].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.NorthWest, out _stitchTarget);
			if (_stitchTarget != null)
			{
				mesh.Vertices[meshVertCount - _sampleCount] = new Vector3(
					mesh.Vertices[meshVertCount - _sampleCount].x,
					_stitchTarget.Vertices[_sampleCount - 1].y,
					mesh.Vertices[meshVertCount - _sampleCount].z);

				mesh.Normals[meshVertCount - _sampleCount] = new Vector3(
					_stitchTarget.Normals[_sampleCount - 1].x,
					_stitchTarget.Normals[_sampleCount - 1].y,
					_stitchTarget.Normals[_sampleCount - 1].z);
			}

			_stitchTarget = null;
			_meshData.TryGetValue(tileId.NorthEast, out _stitchTarget);
			if (_stitchTarget != null)
			{
				targetVertCount = _stitchTarget.Vertices.Count;
				mesh.Vertices[targetVertCount - 1] = new Vector3(
					mesh.Vertices[targetVertCount - 1].x,
					_stitchTarget.Vertices[0].y,
					mesh.Vertices[targetVertCount - 1].z);

				mesh.Normals[targetVertCount - 1] = new Vector3(
					_stitchTarget.Normals[0].x,
					_stitchTarget.Normals[0].y,
					_stitchTarget.Normals[0].z);
			}
		}
	}
}