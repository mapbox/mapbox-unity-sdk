using Mapbox.Unity.Map;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;
	using System;

	/// <summary>
	/// Polygon modifier creates the polygon (vertex&triangles) using the original vertex list.
	/// Currently uses Triangle.Net for triangulation, which occasionally adds extra vertices to maintain a good triangulation so output vertex list might not be exactly same as the original vertex list.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
	public class PolygonMeshModifier : MeshGenerationBase
	{
		public override ModifierType Type
		{
			get { return ModifierType.Preprocess; }
		}

		private UVModifierOptions _options;
		private Vector3 _v1, _v2;

		#region Atlas Fields

		private Vector3 _vert;
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
			_options = (UVModifierOptions) properties;
			_options.PropertyHasChanged += UpdateModifier;
		}

		public override void UnbindProperties()
		{
			_options.PropertyHasChanged -= UpdateModifier;
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (Criteria != null && Criteria.Count > 0)
			{
				foreach (var criterion in Criteria)
				{
					if (criterion.ShouldReplaceFeature(feature))
					{
						return;
					}
				}
			}

			var _counter = feature.Points.Count;
			var subset = new List<List<Vector3>>(_counter);
			Data flatData = null;
			List<int> result = null;
			var currentIndex = 0;
			int vertCount = 0, polygonVertexCount = 0;
			List<int> triList = null;
			List<Vector3> sub = null;



			for (int i = 0; i < _counter; i++)
			{
				sub = feature.Points[i];
				//earcut is built to handle one polygon with multiple holes
				//point data can contain multiple polygons though, so we're handling them separately here

				vertCount = md.Vertices.Count;
				if (IsClockwise(sub) && vertCount > 0)
				{
					flatData = EarcutLibrary.Flatten(subset);
					result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);
					polygonVertexCount = result.Count;
					if (triList == null)
					{
						triList = new List<int>(polygonVertexCount);
					}
					else
					{
						triList.Capacity = triList.Count + polygonVertexCount;
					}

					for (int j = 0; j < polygonVertexCount; j++)
					{
						triList.Add(result[j] + currentIndex);
					}

					currentIndex = vertCount;
					subset.Clear();
				}

				subset.Add(sub);

				polygonVertexCount = sub.Count;
				md.Vertices.Capacity = md.Vertices.Count + polygonVertexCount;
				md.Normals.Capacity = md.Normals.Count + polygonVertexCount;
				md.Edges.Capacity = md.Edges.Count + polygonVertexCount * 2;
				var _size = md.TileRect.Size;

				for (int j = 0; j < polygonVertexCount; j++)
				{
					md.Edges.Add(vertCount + ((j + 1) % polygonVertexCount));
					md.Edges.Add(vertCount + j);
					md.Vertices.Add(sub[j]);
					md.Tangents.Add(Constants.Math.Vector3Forward);
					md.Normals.Add(Constants.Math.Vector3Up);

					if (_options.style == StyleTypes.Satellite)
					{
						var fromBottomLeft = new Vector2(
							(float) (((sub[j].x + md.PositionInTile.x) / tile.TileScale + _size.x / 2) / _size.x),
							(float) (((sub[j].z + md.PositionInTile.z) / tile.TileScale + _size.x / 2) / _size.x));
						md.UV[0].Add(fromBottomLeft);
					}
					else if (_options.texturingType == UvMapType.Tiled)
					{
						md.UV[0].Add(new Vector2(sub[j].x, sub[j].z));
					}
				}
			}

			flatData = EarcutLibrary.Flatten(subset);
			result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);
			polygonVertexCount = result.Count;

			if (_options.texturingType == UvMapType.Atlas || _options.texturingType == UvMapType.AtlasWithColorPalette)
			{
				_currentFacade = _options.atlasInfo.Roofs[UnityEngine.Random.Range(0, _options.atlasInfo.Roofs.Count)];

				minx = float.MaxValue;
				miny = float.MaxValue;
				maxx = float.MinValue;
				maxy = float.MinValue;

				_textureUvCoordinates = new Vector2[md.Vertices.Count];
				_textureDirection = Quaternion.FromToRotation((md.Vertices[0] - md.Vertices[1]), Mapbox.Unity.Constants.Math.Vector3Right);
				_textureUvCoordinates[0] = new Vector2(0, 0);
				_firstVert = md.Vertices[0];
				for (int i = 1; i < md.Vertices.Count; i++)
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

				for (int i = 0; i < md.Vertices.Count; i++)
				{
					md.UV[0].Add(new Vector2(
						(((_textureUvCoordinates[i].x - minx) / width) * _currentFacade.TextureRect.width) + _currentFacade.TextureRect.x,
						(((_textureUvCoordinates[i].y - miny) / height) * _currentFacade.TextureRect.height) + _currentFacade.TextureRect.y));
				}
			}

			if (triList == null)
			{
				triList = new List<int>(polygonVertexCount);
			}
			else
			{
				triList.Capacity = triList.Count + polygonVertexCount;
			}

			for (int i = 0; i < polygonVertexCount; i++)
			{
				triList.Add(result[i] + currentIndex);
			}

			md.Triangles.Add(triList);
		}


		private bool IsClockwise(IList<Vector3> vertices)
		{
			double sum = 0.0;
			var _counter = vertices.Count;
			for (int i = 0; i < _counter; i++)
			{
				_v1 = vertices[i];
				_v2 = vertices[(i + 1) % _counter];
				sum += (_v2.x - _v1.x) * (_v2.z + _v1.z);
			}

			return sum > 0.0;
		}
	}
}
