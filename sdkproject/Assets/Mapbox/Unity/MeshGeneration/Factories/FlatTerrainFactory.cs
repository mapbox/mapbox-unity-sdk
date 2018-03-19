namespace Mapbox.Unity.MeshGeneration.Factories
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;

	[CreateAssetMenu(menuName = "Mapbox/Factories/Terrain Factory - Flat")]
	public class FlatTerrainFactory : AbstractTileFactory
	{
		[SerializeField]
		private Material _baseMaterial;

		[SerializeField]
		private bool _createSideWalls = false;
		[SerializeField]
		private float _sideWallHeight = 10;
		[SerializeField]
		private Material _sideWallMaterial;

		[SerializeField]
		private bool _addCollider = false;

		[SerializeField]
		private bool _addToLayer = false;

		[SerializeField]
		private int _layerId = 0;

		Mesh _cachedQuad;

		internal override void OnInitialized()
		{

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

				if (_createSideWalls)
				{
					renderer.materials = new Material[2]
					{
						_baseMaterial,
						_sideWallMaterial
					};
				}
				else
				{
					renderer.material = _baseMaterial;
				}
			}

			if (tile.MeshFilter == null)
			{
				tile.gameObject.AddComponent<MeshFilter>();
			}

			// HACK: This is here in to make the system trigger a finished state.
			Progress++;
			tile.MeshFilter.sharedMesh = GetQuad(tile, _createSideWalls);
			Progress--;

			if (_addCollider && tile.Collider == null)
			{
				tile.gameObject.AddComponent<BoxCollider>();
			}
		}

		internal override void OnUnregistered(UnityTile tile)
		{

		}

		private Mesh GetQuad(UnityTile tile, bool buildSide)
		{
			if (_cachedQuad != null)
			{
				return _cachedQuad;
			}

			return buildSide ? BuildQuadWithSides(tile) : BuildQuad(tile);
		}

		Mesh BuildQuad(UnityTile tile)
		{
			var unityMesh = new Mesh();
			var verts = new Vector3[4];
			var norms = new Vector3[4];
			verts[0] = tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
			verts[1] = tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
			verts[2] = tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
			verts[3] = tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
			norms[0] = Constants.Math.Vector3Up;
			norms[1] = Constants.Math.Vector3Up;
			norms[2] = Constants.Math.Vector3Up;
			norms[3] = Constants.Math.Vector3Up;

			unityMesh.vertices = verts;
			unityMesh.normals = norms;

			var trilist = new int[6] { 0, 1, 2, 0, 2, 3 };
			unityMesh.SetTriangles(trilist, 0);

			var uvlist = new Vector2[4]
			{
					new Vector2(0,1),
					new Vector2(1,1),
					new Vector2(1,0),
					new Vector2(0,0)
			};
			unityMesh.uv = uvlist;
			tile.MeshFilter.sharedMesh = unityMesh;
			_cachedQuad = unityMesh;

			return unityMesh;
		}

		private Mesh BuildQuadWithSides(UnityTile tile)
		{
			var unityMesh = new Mesh();
			var verts = new Vector3[20];
			var norms = new Vector3[20];
			verts[0] = tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
			verts[1] = tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
			verts[2] = tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
			verts[3] = tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
			norms[0] = Constants.Math.Vector3Up;
			norms[1] = Constants.Math.Vector3Up;
			norms[2] = Constants.Math.Vector3Up;
			norms[3] = Constants.Math.Vector3Up;

			//verts goes
			//01
			//32
			unityMesh.subMeshCount = 2;
			Vector3 norm = Constants.Math.Vector3Up;
			for (int i = 0; i < 4; i++)
			{
				verts[4 * (i + 1)] = verts[i];
				verts[4 * (i + 1) + 1] = verts[i + 1];
				verts[4 * (i + 1) + 2] = verts[i] + new Vector3(0, -_sideWallHeight, 0);
				verts[4 * (i + 1) + 3] = verts[i + 1] + new Vector3(0, -_sideWallHeight, 0);

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
			tile.MeshFilter.sharedMesh = unityMesh;
			_cachedQuad = unityMesh;

			return unityMesh;
		}
	}
}