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
		Height,
		ModifiedHeight
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

		private Vector2 _stitchTarget;

		protected Dictionary<UnityTile, Tile> _tiles;

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
			_tiles = new Dictionary<UnityTile, Tile>();
		}

		internal override void OnRegistered(UnityTile tile)
		{
			if (_addToLayer)
			{
				tile.gameObject.layer = _layerId;
			}
			Run(tile);
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			if (_tiles.ContainsKey(tile))
			{
				_tiles[tile].Cancel();
				_tiles.Remove(tile);
			}
		}

		private void Run(UnityTile tile)
		{
			if (_generationType == TerrainGenerationType.Height)
			{
				CreateTerrainHeight(tile);
			}
			else if (_generationType == TerrainGenerationType.ModifiedHeight)
			{
				CreateTerrainHeight(tile, _heightModifier);
			}
			else if (_generationType == TerrainGenerationType.Flat)
			{
				CreateFlatMesh(tile);
			}
		}

		/// <summary>
		/// Creates the non-flat terrain using a height multiplier
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="heightMultiplier">Multiplier for queried height value</param>
		private void CreateTerrainHeight(UnityTile tile, float heightMultiplier = 1)
		{
			// FIXME: when does this ever happen?
			//if (tile.HeightData == null)
			{
				var parameters = new Tile.Parameters
				{
					Fs = this.FileSource,
					Id = new CanonicalTileId(tile.Zoom, (int)tile.TileCoordinate.x, (int)tile.TileCoordinate.y),
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

					var heightTexture = new Texture2D(0, 0);
					heightTexture.LoadImage(pngRasterTile.Data);
					var colors = heightTexture.GetPixels32();
					Destroy(heightTexture);
					tile.SetHeightData(colors);
					tile.HeightDataState = TilePropertyState.Loaded;
					GenerateTerrainMesh(tile, heightMultiplier);
				});
			}
			//else
			//{
			//	GenerateTerrainMesh(tile, heightMultiplier);
			//}
		}

		/// <summary>
		/// Creates the non-flat terrain mesh, using a grid by defined resolution (_sampleCount). Vertex order goes right & up. Normals are calculated manually and UV map is fitted/stretched 1-1.
		/// Any additional scripts or logic, like MeshCollider or setting layer, can be done here.
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="heightMultiplier">Multiplier for queried height value</param>
		private void GenerateTerrainMesh(UnityTile tile, float heightMultiplier)
		{
			var go = tile.gameObject;
			var mesh = new MeshData();
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
						heightMultiplier * tile.QueryHeightData((int)(xrat * 255),(int)((1 - yrat) * 255)),
						(float)(yy - tile.Rect.Center.y)));
					mesh.Normals.Add(Unity.Constants.Math.Vector3Up);
					mesh.UV[0].Add(new Vector2(x * step, 1 - (y * step)));
				}
			}

			var trilist = new List<int>();
			var dir = Vector3.zero;
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

			// FIXME - does not work with recycling?
			//FixStitches(tile, mesh);

			tile.MeshData = mesh;

			// Don't leak the mesh, just reuse it.
			var unityMesh = tile.MeshFilter.sharedMesh ?? new Mesh();
			unityMesh.SetVertices(mesh.Vertices);
			unityMesh.SetUVs(0, mesh.UV[0]);
			unityMesh.SetNormals(mesh.Normals);
			unityMesh.SetTriangles(mesh.Triangles[0], 0);
			unityMesh.RecalculateBounds();
			tile.MeshFilter.sharedMesh = unityMesh;

			if (tile.MeshRenderer.material == null)
			{
				tile.MeshRenderer.material = _baseMaterial;
			}

			if (_addCollider)
			{
				if (tile.Collider == null)
				{
					go.AddComponent<MeshCollider>();
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
			var mesh = new Mesh();
			var verts = new Vector3[4];

			verts[0] = ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
			verts[2] = (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
			verts[1] = (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
			verts[3] = ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());

			mesh.vertices = verts;
			var trilist = new int[6] { 0, 1, 2, 1, 3, 2 };
			mesh.SetTriangles(trilist, 0);
			var uvlist = new Vector2[4]
			{
				new Vector2(0,1),
				new Vector2(1,1),
				new Vector2(0,0),
				new Vector2(1,0)
			};
			mesh.uv = uvlist;
			mesh.RecalculateNormals();
			tile.MeshFilter.sharedMesh = mesh;
			if (tile.MeshRenderer.material == null)
			{
				tile.MeshRenderer.material = _baseMaterial;
			}

			if (_addCollider && tile.Collider == null)
			{
				var bc = tile.gameObject.AddComponent<MeshCollider>();
			}
			if (_addToLayer)
			{
				tile.gameObject.layer = _layerId;
			}
		}

		/// <summary>
		/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="tmesh"></param>
		private void FixStitches(UnityTile tile, MeshData tmesh)
		{
			_stitchTarget.Set(tile.TileCoordinate.x, tile.TileCoordinate.y - 1);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;

				for (int i = 0; i < _sampleCount; i++)
				{
					//just snapping the y because vertex pos is relative and we'll have to do tile pos + vertex pos for x&z otherwise
					tmesh.Vertices[i] = new Vector3(
						tmesh.Vertices[i].x,
						t2mesh.Vertices[tmesh.Vertices.Count - _sampleCount + i].y,
						tmesh.Vertices[i].z);
					tmesh.Normals[i] = new Vector3(t2mesh.Normals[tmesh.Vertices.Count - _sampleCount + i].x,
						t2mesh.Normals[tmesh.Vertices.Count - _sampleCount + i].y,
						t2mesh.Normals[tmesh.Vertices.Count - _sampleCount + i].z);
				}
			}

			_stitchTarget.Set(tile.TileCoordinate.x, tile.TileCoordinate.y + 1);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;
				for (int i = 0; i < _sampleCount; i++)
				{
					tmesh.Vertices[tmesh.Vertices.Count - _sampleCount + i] = new Vector3(
						tmesh.Vertices[tmesh.Vertices.Count - _sampleCount + i].x,
						t2mesh.Vertices[i].y,
						tmesh.Vertices[tmesh.Vertices.Count - _sampleCount + i].z);

					tmesh.Normals[tmesh.Vertices.Count - _sampleCount + i] = new Vector3(
						t2mesh.Normals[i].x,
						t2mesh.Normals[i].y,
						t2mesh.Normals[i].z);
				}
			}

			_stitchTarget.Set(tile.TileCoordinate.x - 1, tile.TileCoordinate.y);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;
				for (int i = 0; i < _sampleCount; i++)
				{
					tmesh.Vertices[i * _sampleCount] = new Vector3(
						tmesh.Vertices[i * _sampleCount].x,
						t2mesh.Vertices[i * _sampleCount + _sampleCount - 1].y,
						tmesh.Vertices[i * _sampleCount].z);
					tmesh.Normals[i * _sampleCount] = new Vector3(
						t2mesh.Normals[i * _sampleCount + _sampleCount - 1].x,
						t2mesh.Normals[i * _sampleCount + _sampleCount - 1].y,
						t2mesh.Normals[i * _sampleCount + _sampleCount - 1].z);
				}
			}

			_stitchTarget.Set(tile.TileCoordinate.x + 1, tile.TileCoordinate.y);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;
				for (int i = 0; i < _sampleCount; i++)
				{
					tmesh.Vertices[i * _sampleCount + _sampleCount - 1] = new Vector3(
						tmesh.Vertices[i * _sampleCount + _sampleCount - 1].x,
						t2mesh.Vertices[i * _sampleCount].y,
						tmesh.Vertices[i * _sampleCount + _sampleCount - 1].z);
					tmesh.Normals[i * _sampleCount + _sampleCount - 1] = new Vector3(
						t2mesh.Normals[i * _sampleCount].x,
						t2mesh.Normals[i * _sampleCount].y,
						t2mesh.Normals[i * _sampleCount].z);
				}
			}

			_stitchTarget.Set(tile.TileCoordinate.x - 1, tile.TileCoordinate.y - 1);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;
				tmesh.Vertices[0] = new Vector3(
					tmesh.Vertices[0].x,
					t2mesh.Vertices[t2mesh.Vertices.Count - 1].y,
					tmesh.Vertices[0].z);
				tmesh.Normals[0] = new Vector3(
					t2mesh.Normals[t2mesh.Vertices.Count - 1].x,
					t2mesh.Normals[t2mesh.Vertices.Count - 1].y,
					t2mesh.Normals[t2mesh.Vertices.Count - 1].z);
			}

			_stitchTarget.Set(tile.TileCoordinate.x + 1, tile.TileCoordinate.y - 1);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;
				tmesh.Vertices[_sampleCount - 1] = new Vector3(
					tmesh.Vertices[_sampleCount - 1].x,
					t2mesh.Vertices[t2mesh.Vertices.Count - _sampleCount].y,
					tmesh.Vertices[_sampleCount - 1].z);
				tmesh.Normals[_sampleCount - 1] = new Vector3(
					t2mesh.Normals[t2mesh.Vertices.Count - _sampleCount].x,
					t2mesh.Normals[t2mesh.Vertices.Count - _sampleCount].y,
					t2mesh.Normals[t2mesh.Vertices.Count - _sampleCount].z);
			}

			_stitchTarget.Set(tile.TileCoordinate.x - 1, tile.TileCoordinate.y + 1);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;
				tmesh.Vertices[tmesh.Vertices.Count - _sampleCount] = new Vector3(
					tmesh.Vertices[tmesh.Vertices.Count - _sampleCount].x,
					t2mesh.Vertices[_sampleCount - 1].y,
					tmesh.Vertices[tmesh.Vertices.Count - _sampleCount].z);
				tmesh.Normals[tmesh.Vertices.Count - _sampleCount] = new Vector3(
					t2mesh.Normals[_sampleCount - 1].x,
					t2mesh.Normals[_sampleCount - 1].y,
					t2mesh.Normals[_sampleCount - 1].z);
			}

			_stitchTarget.Set(tile.TileCoordinate.x + 1, tile.TileCoordinate.y + 1);
			if (_unityTiles.ContainsKey(_stitchTarget) && _unityTiles[_stitchTarget].MeshData != null)
			{
				var t2mesh = _unityTiles[_stitchTarget].MeshData;
				tmesh.Vertices[t2mesh.Vertices.Count - 1] = new Vector3(
					tmesh.Vertices[t2mesh.Vertices.Count - 1].x,
					t2mesh.Vertices[0].y,
					tmesh.Vertices[t2mesh.Vertices.Count - 1].z);
				tmesh.Normals[t2mesh.Vertices.Count - 1] = new Vector3(
					t2mesh.Normals[0].x,
					t2mesh.Normals[0].y,
					t2mesh.Normals[0].z);
			}
		}
	}
}
