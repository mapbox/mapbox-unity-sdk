namespace Mapbox.Unity.MeshGeneration.Factories
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;

	[CreateAssetMenu(menuName = "Mapbox/Factories/Flat Terrain Factory")]
	public class FlatTerrainFactory : AbstractTileFactory
	{
		[SerializeField]
		private Material _baseMaterial;

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
				renderer.material = _baseMaterial;
			}

			if (tile.MeshFilter == null)
			{
				tile.gameObject.AddComponent<MeshFilter>();
			}

			// HACK: This is here in to make the system trigger a finished state.
			Progress++;
			tile.MeshFilter.sharedMesh = GetQuad(tile);
			Progress--;

			if (_addCollider && tile.Collider == null)
			{
				tile.gameObject.AddComponent<BoxCollider>();
			}
		}

		internal override void OnUnregistered(UnityTile tile)
		{

		}

		private Mesh GetQuad(UnityTile tile)
		{
			if (_cachedQuad != null)
			{
				return _cachedQuad;
			}

			return BuildQuad(tile);
		}

		Mesh BuildQuad(UnityTile tile)
		{
			var unityMesh = new Mesh();
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

			tile.MeshFilter.sharedMesh = unityMesh;
			_cachedQuad = unityMesh;

			return unityMesh;
		}
	}
}