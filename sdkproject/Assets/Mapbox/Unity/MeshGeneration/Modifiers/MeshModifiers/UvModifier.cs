namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Utils;

	/// <summary>
	/// UV Modifier works only with (and right after) Polygon Modifier and not with Line Mesh Modifier.
	/// If UseSatelliteRoof parameter is false, it creates a tiled UV map, otherwise it creates a stretched UV map.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/UV Modifier")]
	public class UvModifier : MeshModifier
	{
		public override ModifierType Type { get { return ModifierType.Preprocess; } }
		public bool UseSatelliteRoof = false;

		private int _mdVertexCount;
		private Vector2d _size;
		private Vector3 _vert;
		private List<Vector2> _uv = new List<Vector2>();

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			_uv.Clear();
			_mdVertexCount = md.Vertices.Count;
			_size = md.TileRect.Size;

			for (int i = 0; i < _mdVertexCount; i++)
			{
				_vert = md.Vertices[i];
				if (UseSatelliteRoof)
				{
					var fromBottomLeft = new Vector2((float)(((_vert.x + md.PositionInTile.x) / tile.TileScale + _size.x / 2) / _size.x),
						(float)(((_vert.z + md.PositionInTile.z) / tile.TileScale + _size.x / 2) / _size.x));
					_uv.Add(fromBottomLeft);
				}
				else
				{
					_uv.Add(new Vector2(_vert.x, _vert.z));
				}
			}
			md.UV[0].AddRange(_uv);
		}
	}
}
