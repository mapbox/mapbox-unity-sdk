using System.Collections.Generic;
using Mapbox.Unity.DataContainers;
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
			base.Initialize(elOptions);
			BuildQuad();
		}

		public override void RegisterTile(UnityTile tile, bool createElevatedMesh)
		{
			var meshFilter = tile.MeshFilter;

			if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
			{
				tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
			}

			if (meshFilter.sharedMesh.vertexCount != RequiredVertexCount)
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
				var sharedMesh = tile.MeshFilter.sharedMesh;
				sharedMesh.Clear();

				// HACK: This is here in to make the system trigger a finished state.
				//GetQuad(tile, _elevationOptions.sideWallOptions.isActive);
				sharedMesh.vertices = _cachedQuad.Vertices;
				sharedMesh.normals = _cachedQuad.Normals;
				sharedMesh.triangles = _cachedQuad.Triangles;
				sharedMesh.uv = _cachedQuad.Uvs;

				tile.ElevationType = TileTerrainType.Flat;
			}

			if (_elevationOptions.colliderOptions.addCollider)
			{
				var meshCollider = tile.Collider as MeshCollider;
				if (meshCollider)
				{
					meshCollider.sharedMesh = meshFilter.sharedMesh;
				}
			}
		}

		private void BuildQuad()
		{
			var halfSize = _elevationOptions.TileMeshSize / 2;

			//32
			//01
			var verts = new Vector3[4];
			var norms = new Vector3[4];
			verts[0] = new Vector3(-halfSize, 0, -halfSize); //tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
			verts[1] = new Vector3(halfSize, 0, -halfSize); //tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
			verts[2] = new Vector3(halfSize, 0, halfSize); //tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
			verts[3] = new Vector3(-halfSize, 0, halfSize); //tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
			norms[0] = Constants.Math.Vector3Up;
			norms[1] = Constants.Math.Vector3Up;
			norms[2] = Constants.Math.Vector3Up;
			norms[3] = Constants.Math.Vector3Up;

			var trilist = new int[6] { 0, 2, 1, 0, 3, 2 };

			var uvlist = new Vector2[4]
			{
				new Vector2(0,0),
				new Vector2(1,0),
				new Vector2(1,1),
				new Vector2(0,1)
			};

			_cachedQuad = new MeshDataArray()
			{
				Vertices =  verts,
				Normals = norms,
				Triangles = trilist,
				Uvs = uvlist
			};
		}
	}
}
