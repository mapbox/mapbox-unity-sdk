namespace Mapbox.Unity.MeshGeneration.Factories
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;
	using Mapbox.Utils;
	using Mapbox.Map;

	[CreateAssetMenu(menuName = "Mapbox/Factories/Terrain Factory - Flat Sphere")]
	public class FlatSphereTerrainFactory : AbstractTileFactory
	{
		[SerializeField]
		private Material _baseMaterial;
		[SerializeField]
		private float _radius;

		[SerializeField]
		[Range(2,256)]
		int _sampleCount;

		[SerializeField]
		private bool _addCollider = false;

		[SerializeField]
		private bool _addToLayer = false;

		[SerializeField]
		private int _layerId = 0;

		public float Radius
		{
			get
			{
				return _radius;
			}
		}

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
			GenerateTerrainMesh(tile);
			Progress--;

			if (_addCollider && tile.Collider == null)
			{
				tile.gameObject.AddComponent<MeshCollider>();
			}
		}

		void GenerateTerrainMesh(UnityTile tile)
		{
			var verts = new List<Vector3>();
			for (float x = 0; x < _sampleCount; x++)
			{
				for (float y = 0; y < _sampleCount; y++)
				{
					var xx = Mathf.Lerp((float)tile.Rect.Min.x, ((float)tile.Rect.Min.x + (float)tile.Rect.Size.x),
						x / (_sampleCount - 1));
					var yy = Mathf.Lerp((float)tile.Rect.Max.y, ((float)tile.Rect.Max.y + (float)tile.Rect.Size.y),
						y / (_sampleCount - 1));

					var ll = Conversions.MetersToLatLon(new Vector2d(xx, yy));

					var latitude = (float)(Mathf.Deg2Rad * ll.x);
					var longitude = (float)(Mathf.Deg2Rad * ll.y);

					float xPos = (_radius) * Mathf.Cos(latitude) * Mathf.Cos(longitude);
					float zPos = (_radius) * Mathf.Cos(latitude) * Mathf.Sin(longitude);
					float yPos = (_radius) * Mathf.Sin(latitude);

					var pp = new Vector3(xPos, yPos, zPos);
					verts.Add(pp);
				}
			}

			var trilist = new List<int>();
			for (int y = 0; y < _sampleCount - 1; y++)
			{
				for (int x = 0; x < _sampleCount - 1; x++)
				{
					trilist.Add((y * _sampleCount) + x);
					trilist.Add((y * _sampleCount) + x + _sampleCount + 1);
					trilist.Add((y * _sampleCount) + x + _sampleCount);

					trilist.Add((y * _sampleCount) + x);
					trilist.Add((y * _sampleCount) + x + 1);
					trilist.Add((y * _sampleCount) + x + _sampleCount + 1);
				}
			}

			var uvlist = new List<Vector2>();
			var step = 1f / (_sampleCount - 1);
			for (int i = 0; i < _sampleCount; i++)
			{
				for (int j = 0; j < _sampleCount; j++)
				{
					uvlist.Add(new Vector2(i * step, (j * step)));
				}
			}

			tile.MeshFilter.mesh.SetVertices(verts);
			tile.MeshFilter.mesh.SetTriangles(trilist, 0);
			tile.MeshFilter.mesh.SetUVs(0, uvlist);
			tile.MeshFilter.mesh.RecalculateBounds();
			tile.MeshFilter.mesh.RecalculateNormals();

			tile.transform.localPosition = Mapbox.Unity.Constants.Math.Vector3Zero;
		}

		internal override void OnUnregistered(UnityTile tile)
		{

		}
	}
}