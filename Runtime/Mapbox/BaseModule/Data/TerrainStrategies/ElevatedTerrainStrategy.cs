using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Terrain.Settings;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.TerrainStrategies
{
	public class MeshDataArray
	{
		public Vector3[] Vertices;
		public Vector3[] Normals;
		public List<int[]> Triangles;
		public Vector2[] Uvs;

		public MeshDataArray()
		{
			Triangles = new List<int[]>();
		}
	}

	public class ElevatedTerrainStrategy : TerrainStrategy, IElevationBasedTerrainStrategy
	{
		private MeshDataArray _baseMesh;
		private List<Vector3> _newVertexList;
		private List<Vector3> _newNormalList;
		private List<Vector2> _newUvList;
		//private List<int> _newTriangleList;
		private Vector3 _newDir;
		private int _vertA, _vertB, _vertC;
		private int _counter;

		private bool _useTileSkirts = false;
		private float _skirtSize = 1;

		private int _sideVertexCount;
		private int _requiredVertexCount;
		
		public override int RequiredVertexCount
		{
			get
			{
				return _requiredVertexCount;
			}
		}

		public override void Initialize(ElevationLayerProperties elOptions)
		{
			base.Initialize(elOptions);

			_useTileSkirts = elOptions.sideWallOptions.isActive;
			 _sideVertexCount = _useTileSkirts 
				? _elevationOptions.modificationOptions.sampleCount + 3 
				: _elevationOptions.modificationOptions.sampleCount + 1;
			_skirtSize = elOptions.sideWallOptions.wallHeight;
			
			_requiredVertexCount =  _sideVertexCount * _sideVertexCount;
			_newVertexList = new List<Vector3>(_requiredVertexCount);
			_newNormalList = new List<Vector3>(_requiredVertexCount);
			_newUvList = new List<Vector2>(_requiredVertexCount);
			//_newTriangleList = new List<int>();
			
			_baseMesh = CreateBaseMesh(TileSize, _sideVertexCount);
		}

		public override void RegisterTile(UnityMapTile tile, bool createElevatedMesh)
		{
			if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
			{
				tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
			}

			if (tile.MeshFilter.sharedMesh.vertexCount != RequiredVertexCount)
			{
				Mesh sharedMesh;
				(sharedMesh = tile.MeshFilter.sharedMesh).Clear();
				var newMesh = _baseMesh;
				sharedMesh.subMeshCount = 2;
				sharedMesh.vertices = newMesh.Vertices;
				sharedMesh.normals = newMesh.Normals;
				for (var i = 0; i < newMesh.Triangles.Count; i++)
				{
					var triangle = newMesh.Triangles[i];
					sharedMesh.SetTriangles(triangle, i);
				}
				sharedMesh.uv = newMesh.Uvs;
				tile.TerrainContainer.FixMeshBounds(!createElevatedMesh);
			}
			
			// if (_elevationOptions.sideWallOptions.isActive)
			// {
			// 	var firstMat = tile.MeshRenderer.materials[0];
			// 	tile.MeshRenderer.materials = new Material[2]
			// 	{
			// 		firstMat,
			// 		_elevationOptions.sideWallOptions.wallMaterial
			// 	};
			// }

			if (createElevatedMesh)
			{
				CreateElevatedMesh(tile);
			}
		}

		private void CreateElevatedMesh(UnityMapTile tile)
		{
			var mesh = tile.MeshFilter.mesh;
			var vertices = mesh.vertices;
			var sampleCount = (int)Mathf.Sqrt(mesh.vertexCount);
			for (int i = 0; i < vertices.Length; i++)
			{
				var x = i % sampleCount;
				var y = i / sampleCount;
				var dx = (float)x / (sampleCount - 2);
				var dy = (float)y / (sampleCount - 2);
				var elevation = 0f;
				if (!_useTileSkirts)
				{
					elevation = tile.TerrainContainer.QueryHeightData(dx, dy) * tile.TileScale;
				}
				else
				{
					elevation = (x == 0 || y == 0 || x == sampleCount - 1 || y == sampleCount - 1)
						? -_skirtSize
						: tile.TerrainContainer.QueryHeightData(dx, dy) * tile.TileScale;
				}

				vertices[i].Set(vertices[i].x, elevation, vertices[i].z);
			}
			mesh.vertices = vertices;
		}

		#region mesh gen
		private MeshDataArray CreateBaseMesh(float tileSize, int sampleCount)
		{
			return
				_useTileSkirts
					? CreateBaseMeshSkirts(tileSize, sampleCount)
					: CreateBaseMeshWithoutSkirts(tileSize, sampleCount);
		}

		private MeshDataArray CreateBaseMeshWithoutSkirts(float size, int sampleCount)
		{
			//TODO use arrays instead of lists
			_newVertexList.Clear();
			_newNormalList.Clear();
			_newUvList.Clear();
			var _newTriangleList = new List<int>();

			//012
			//345
			//678
			for (float y = 0; y < sampleCount; y++)
			{
				var yrat = y / (sampleCount - 1);
				for (float x = 0; x < sampleCount; x++)
				{
					var xrat = x / (sampleCount - 1);

					var xx = Mathf.LerpUnclamped(0, size, xrat);
					//lerp x/y swapped here because of the texture space conversion (y to -y)
					var yy = Mathf.LerpUnclamped(size, 0, yrat);

					var elevation = 0;

					_newVertexList.Add(new Vector3(
						xx,
						elevation,
						-1 * yy));
					_newNormalList.Add(Constants.Math.Vector3Up);
					_newUvList.Add(new Vector2(x * 1f / (sampleCount - 1), (y * 1f / (sampleCount - 1))));
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
					_newTriangleList.Add(vertC);
					_newTriangleList.Add(vertB);

					vertA = (y * sampleCount) + x;
					vertB = (y * sampleCount) + x + 1;
					vertC = (y * sampleCount) + x + sampleCount + 1;
					_newTriangleList.Add(vertA);
					_newTriangleList.Add(vertC);
					_newTriangleList.Add(vertB);
				}
			}

			var mesh = new MeshDataArray();
			mesh.Vertices = _newVertexList.ToArray();
			mesh.Normals = _newNormalList.ToArray();
			mesh.Uvs = _newUvList.ToArray();
			mesh.Triangles.Add(_newTriangleList.ToArray());
			return mesh;
		}

		private MeshDataArray CreateBaseMeshSkirts(float size, int sideVertexCount)
		{
			//TODO use arrays instead of lists
			_newVertexList.Clear();
			_newNormalList.Clear();
			_newUvList.Clear();
			var _newTriangleList = new List<int>();

			//012
			//345
			//678
			for (int y = -1; y < sideVertexCount - 1; y++)
			{
				var yrat = (float)y / (sideVertexCount - 3); // 1 for buffer pixel, 2 for skirts
				for (int x = -1; x < sideVertexCount - 1; x++)
				{
					var xrat = (float)x / (sideVertexCount - 3);

					var xx = Mathf.LerpUnclamped(0, size, xrat);
					//lerp x/y swapped here because of the texture space conversion (y to -y)
					var yy = Mathf.LerpUnclamped(size, 0, yrat);

					var elevation = x < 0 || y < 0 || x == sideVertexCount-2 || y == sideVertexCount-2 ? -_skirtSize : 0;

					_newVertexList.Add(new Vector3(
						xx,
						elevation,
						-1 * yy));
					_newNormalList.Add(Constants.Math.Vector3Up);
					_newUvList.Add(new Vector2(xrat, yrat));
					//_newUvList.Add(new Vector2((1f/514) + (xrat * 512)/514, 1 - ((1f/514) + (yrat * 512)/514)));
				}
			}

			int vertA, vertB, vertC;

			var topQuadTris = new List<int>();
			for (int y = 0; y < sideVertexCount - 1; y++)
			{
				for (int x = 0; x < sideVertexCount - 1; x++)
				{
					vertA = (y * sideVertexCount) + x;
					vertB = (y * sideVertexCount) + x + sideVertexCount + 1;
					vertC = (y * sideVertexCount) + x + sideVertexCount;

					if (x == 0 || y == 0 || x == sideVertexCount - 2 || y == sideVertexCount - 2)
					{
						_newTriangleList.Add(vertA);
						_newTriangleList.Add(vertC);
						_newTriangleList.Add(vertB);
					}
					else
					{
						topQuadTris.Add(vertA);
						topQuadTris.Add(vertC);
						topQuadTris.Add(vertB);
					}

					vertA = (y * sideVertexCount) + x;
					vertB = (y * sideVertexCount) + x + 1;
					vertC = (y * sideVertexCount) + x + sideVertexCount + 1;
					
					if (x == 0 || y == 0 || x == sideVertexCount - 2 || y == sideVertexCount - 2)
					{
						_newTriangleList.Add(vertA);
						_newTriangleList.Add(vertC);
						_newTriangleList.Add(vertB);
					}
					else
					{
						topQuadTris.Add(vertA);
						topQuadTris.Add(vertC);
						topQuadTris.Add(vertB);
					}
				}
			}

			var mesh = new MeshDataArray();
			mesh.Vertices = _newVertexList.ToArray();
			mesh.Normals = _newNormalList.ToArray();
			mesh.Uvs = _newUvList.ToArray();
			topQuadTris.AddRange(_newTriangleList.ToArray());
			mesh.Triangles.Add(topQuadTris.ToArray());
			//mesh.Triangles.Add(_newTriangleList.ToArray());
			return mesh;
		}


		/// <summary>
		/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
		/// </summary>
		/// <param name="tile">UnityMapTile of the tile being processed.</param>
		/// <param name="allTiles">All tiles, including this tile, used to find neighbors</param>
		///
		public void FixStitches(UnityMapTile tile, Dictionary<UnwrappedTileId, UnityMapTile> allTiles)
		{
			UnwrappedTileId tileId = tile.UnwrappedTileId;
			int sampleCount =  _useTileSkirts 
				? _elevationOptions.modificationOptions.sampleCount + 3 
				: _elevationOptions.modificationOptions.sampleCount + 1;

			Vector3[] targetVerts;
			Vector3[] verts = tile.MeshFilter.sharedMesh.vertices;
			var meshVertCount = verts.Length;
			if (allTiles.ContainsKey(tileId.North) && allTiles[tileId.North].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.North].MeshFilter.sharedMesh.vertices;

				for (int i = 0; i < sampleCount; i++)
				{
                    int source_i = meshVertCount - sampleCount + i;
                    int target_i = i;
					verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
				}
                allTiles[tileId.North].MeshFilter.sharedMesh.vertices = targetVerts;
			}

			if (allTiles.ContainsKey(tileId.South) && allTiles[tileId.South].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.South].MeshFilter.sharedMesh.vertices;

				for (int i = 0; i < sampleCount; i++)
				{
                    int source_i = i;
                    int target_i = meshVertCount - sampleCount + i;
					verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
				}
			}

			if (allTiles.ContainsKey(tileId.West) && allTiles[tileId.West].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.West].MeshFilter.sharedMesh.vertices;

				for (int i = 0; i < sampleCount; i++)
				{
                    int target_i = i * sampleCount + sampleCount - 1;
                    int source_i = i * sampleCount;
					verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
				}
			}

			if (allTiles.ContainsKey(tileId.East) && allTiles[tileId.East].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.East].MeshFilter.sharedMesh.vertices;

				for (int i = 0; i < sampleCount; i++)
				{
                    int target_i = i * sampleCount;
                    int source_i = i * sampleCount + sampleCount - 1;
					verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
				}
			}

			if (allTiles.ContainsKey(tileId.NorthWest) && allTiles[tileId.NorthWest].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.NorthWest].MeshFilter.sharedMesh.vertices;

                int source_i = meshVertCount - sampleCount;
                int target_i = sampleCount - 1;
                verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
			}

			if (allTiles.ContainsKey(tileId.NorthEast) && allTiles[tileId.NorthEast].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.NorthEast].MeshFilter.sharedMesh.vertices;

                int source_i = meshVertCount - 1;
                int target_i = 0;
                verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
			}

			if (allTiles.ContainsKey(tileId.SouthWest) && allTiles[tileId.SouthWest].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.SouthWest].MeshFilter.sharedMesh.vertices;

                int source_i = 0;
                int target_i = meshVertCount - 1;
                verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
			}

			if (allTiles.ContainsKey(tileId.SouthEast) && allTiles[tileId.SouthEast].TerrainContainer.State == TileContainerState.Final)
			{
				targetVerts = allTiles[tileId.SouthEast].MeshFilter.sharedMesh.vertices;
                int source_i = sampleCount - 1;
                int target_i = meshVertCount - sampleCount;
                verts[source_i] = new Vector3(verts[source_i].x, targetVerts[target_i].y, verts[source_i].z);
			}

			tile.MeshFilter.sharedMesh.vertices = verts;
			tile.MeshFilter.sharedMesh.RecalculateNormals();
		}

		#endregion
	}
}
