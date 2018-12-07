using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies
{
	public class FlatTerrainStrategy : TerrainStrategy
	{
		MeshDataArray _cachedQuad;

		public override int RequiredVertexCount
		{
			get { return 4; }
		}

		public override void Initialize(ElevationLayerProperties elOptions)
		{
			_elevationOptions = elOptions;
		}

		public override void RegisterTile(UnityTile tile)
		{
			if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
			{
				tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
			}

			if (tile.RasterDataState != Enums.TilePropertyState.Loaded ||
			    tile.MeshFilter.sharedMesh.vertexCount != RequiredVertexCount)
			{
				if (_elevationOptions.sideWallOptions.isActive)
				{
					var firstMat = tile.MeshRenderer.materials[0];
					tile.MeshRenderer.materials = new Material[2]
					{
						firstMat,
						_elevationOptions.sideWallOptions.wallMaterial
					};
				}
			}

			if ((int)tile.ElevationType != (int)ElevationLayerType.FlatTerrain)
			{
				tile.MeshFilter.sharedMesh.Clear();
				// HACK: This is here in to make the system trigger a finished state.
				GetQuad(tile, _elevationOptions.sideWallOptions.isActive);
				tile.ElevationType = TileTerrainType.Flat;
			}
		}

		private void GetQuad(UnityTile tile, bool buildSide)
		{
			if (_cachedQuad != null)
			{
				var mesh = tile.MeshFilter.sharedMesh;
				mesh.vertices = _cachedQuad.Vertices;
				mesh.normals = _cachedQuad.Normals;
				mesh.triangles = _cachedQuad.Triangles;
				mesh.uv = _cachedQuad.Uvs;
			}
			else
			{
				if (buildSide)
				{
					BuildQuadWithSides(tile);
				}
				else
				{
					BuildQuad(tile);
				}
			}
		}

		private void BuildQuad(UnityTile tile)
		{
			var unityMesh = tile.MeshFilter.sharedMesh;
			var verts = new Vector3[4];
			var norms = new Vector3[4];
			verts[0] = tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
			verts[1] = tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
			verts[2] = tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
			verts[3] = tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
			norms[0] = Mapbox.Unity.Constants.Math.Vector3Up;
			norms[1] = Mapbox.Unity.Constants.Math.Vector3Up;
			norms[2] = Mapbox.Unity.Constants.Math.Vector3Up;
			norms[3] = Mapbox.Unity.Constants.Math.Vector3Up;

			unityMesh.vertices = verts;
			unityMesh.normals = norms;

			var trilist = new int[6] { 0, 1, 2, 0, 2, 3 };
			unityMesh.triangles = trilist;

			var uvlist = new Vector2[4]
			{
					new Vector2(0,1),
					new Vector2(1,1),
					new Vector2(1,0),
					new Vector2(0,0)
			};
			unityMesh.uv = uvlist;
			_cachedQuad = new MeshDataArray()
			{
				Vertices =  verts,
				Normals = norms,
				Triangles = trilist,
				Uvs = uvlist
			};
		}

		private void BuildQuadWithSides(UnityTile tile)
		{
			var unityMesh = tile.MeshFilter.sharedMesh;
			var verts = new Vector3[20];
			var norms = new Vector3[20];
			verts[0] = tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
			verts[1] = tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
			verts[2] = tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
			verts[3] = tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
			norms[0] = Mapbox.Unity.Constants.Math.Vector3Up;
			norms[1] = Mapbox.Unity.Constants.Math.Vector3Up;
			norms[2] = Mapbox.Unity.Constants.Math.Vector3Up;
			norms[3] = Mapbox.Unity.Constants.Math.Vector3Up;

			//verts goes
			//01
			//32
			unityMesh.subMeshCount = 2;
			Vector3 norm = Mapbox.Unity.Constants.Math.Vector3Up;
			for (int i = 0; i < 4; i++)
			{
				verts[4 * (i + 1)] = verts[i];
				verts[4 * (i + 1) + 1] = verts[i + 1];
				verts[4 * (i + 1) + 2] = verts[i] + new Vector3(0, -_elevationOptions.sideWallOptions.wallHeight, 0);
				verts[4 * (i + 1) + 3] = verts[i + 1] + new Vector3(0, -_elevationOptions.sideWallOptions.wallHeight, 0);

				norm = Vector3.Cross(verts[4 * (i + 1) + 1] - verts[4 * (i + 1) + 2], verts[4 * (i + 1)] - verts[4 * (i + 1) + 1]).normalized;
				norms[4 * (i + 1)] = norm;
				norms[4 * (i + 1) + 1] = norm;
				norms[4 * (i + 1) + 2] = norm;
				norms[4 * (i + 1) + 3] = norm;
			}

			unityMesh.vertices = verts;
			unityMesh.normals = norms;

			var trilist = new List<int>(6) { 0, 1, 2, 0, 2, 3 };
			unityMesh.SetTriangles(trilist, 0);

			trilist = new List<int>(8);
			for (int i = 0; i < 4; i++)
			{
				trilist.Add(4 * (i + 1));
				trilist.Add(4 * (i + 1) + 2);
				trilist.Add(4 * (i + 1) + 1);

				trilist.Add(4 * (i + 1) + 1);
				trilist.Add(4 * (i + 1) + 2);
				trilist.Add(4 * (i + 1) + 3);
			}
			unityMesh.SetTriangles(trilist, 1);

			var uvlist = new Vector2[20];
			uvlist[0] = new Vector2(0, 1);
			uvlist[1] = new Vector2(1, 1);
			uvlist[2] = new Vector2(1, 0);
			uvlist[3] = new Vector2(0, 0);
			for (int i = 4; i < 20; i += 4)
			{
				uvlist[i] = new Vector2(1, 1);
				uvlist[i + 1] = new Vector2(0, 1);
				uvlist[i + 2] = new Vector2(1, 0);
				uvlist[i + 3] = new Vector2(0, 0);
			}
			unityMesh.uv = uvlist;
			_cachedQuad = new MeshDataArray()
			{
				Vertices =  verts,
				Normals = norms,
				Triangles = trilist.ToArray(),
				Uvs = uvlist
			};
		}
	}
}
