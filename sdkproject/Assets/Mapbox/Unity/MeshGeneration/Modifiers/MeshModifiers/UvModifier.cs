namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;

	/// <summary>
	/// UV Modifier works only with (and right after) Polygon Modifier and not with Line Mesh Modifier.
	/// If UseSatelliteRoof parameter is false, it creates a tiled UV map, otherwise it creates a stretched UV map.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/UV Modifier")]
	public class UvModifier : MeshModifier
	{
		UVModifierOptions _options;
		//public UvMapType UvType;
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		private int _mdVertexCount;
		private Vector2d _size;
		private Vector3 _vert;
		private List<Vector2> _uv = new List<Vector2>();

		#region Atlas Fields
		//texture uv fields
		//public AtlasInfo AtlasInfo;
		private AtlasEntity _currentFacade;
		private Quaternion _textureDirection;
		private Vector2[] _textureUvCoordinates;
		private Vector3 _vertexRelativePos;
		private Vector3 _firstVert;

		private float minx;
		private float miny;
		private float maxx;
		private float maxy;
		#endregion

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (UVModifierOptions)properties;
			_options.PropertyHasChanged += UpdateModifier;
		}

		public override void UnbindProperties()
		{
			_options.PropertyHasChanged -= UpdateModifier;
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
			{
				return;
			}

			_uv.Clear();
			_mdVertexCount = md.Vertices.Count;
			_size = md.TileRect.Size;

			if (_options.texturingType != UvMapType.Atlas && _options.texturingType != UvMapType.AtlasWithColorPalette)
			{
				for (int i = 0; i < _mdVertexCount; i++)
				{
					_vert = md.Vertices[i];

					if (_options.style == StyleTypes.Satellite)
					{
						var fromBottomLeft = new Vector2((float)(((_vert.x + md.PositionInTile.x) / tile.TileScale + _size.x / 2) / _size.x),
							(float)(((_vert.z + md.PositionInTile.z) / tile.TileScale + _size.x / 2) / _size.x));
						_uv.Add(fromBottomLeft);
					}
					else if (_options.texturingType == UvMapType.Tiled)
					{
						_uv.Add(new Vector2(_vert.x, _vert.z));
					}
				}
			}
			else if (_options.texturingType == UvMapType.Atlas || _options.texturingType == UvMapType.AtlasWithColorPalette)
			{
				_currentFacade = _options.atlasInfo.Roofs[UnityEngine.Random.Range(0, _options.atlasInfo.Roofs.Count)];

				minx = float.MaxValue;
				miny = float.MaxValue;
				maxx = float.MinValue;
				maxy = float.MinValue;

				_textureUvCoordinates = new Vector2[_mdVertexCount];
				_textureDirection = Quaternion.FromToRotation((md.Vertices[0] - md.Vertices[1]), Mapbox.Unity.Constants.Math.Vector3Right);
				_textureUvCoordinates[0] = new Vector2(0, 0);
				_firstVert = md.Vertices[0];
				for (int i = 1; i < _mdVertexCount; i++)
				{
					_vert = md.Vertices[i];
					_vertexRelativePos = _vert - _firstVert;
					_vertexRelativePos = _textureDirection * _vertexRelativePos;
					_textureUvCoordinates[i] = new Vector2(_vertexRelativePos.x, _vertexRelativePos.z);
					if (_vertexRelativePos.x < minx)
						minx = _vertexRelativePos.x;
					if (_vertexRelativePos.x > maxx)
						maxx = _vertexRelativePos.x;
					if (_vertexRelativePos.z < miny)
						miny = _vertexRelativePos.z;
					if (_vertexRelativePos.z > maxy)
						maxy = _vertexRelativePos.z;
				}

				var width = maxx - minx;
				var height = maxy - miny;

				for (int i = 0; i < _mdVertexCount; i++)
				{
					_uv.Add(new Vector2(
						(((_textureUvCoordinates[i].x - minx) / width) * _currentFacade.TextureRect.width) + _currentFacade.TextureRect.x,
						(((_textureUvCoordinates[i].y - miny) / height) * _currentFacade.TextureRect.height) + _currentFacade.TextureRect.y));
				}
			}

			md.UV[0].AddRange(_uv);
		}
	}
}
