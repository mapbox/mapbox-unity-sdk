using System;
using Mapbox.Unity.Utilities;
using System.Collections.Generic;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	public class LineMeshCore
	{
		public float Width = 1.0f;
		public float MiterLimit = 0.2f;
		public float RoundLimit = 1.05f;
		public JoinType JoinType = JoinType.Round;
		public JoinType CapType = JoinType.Round;
		public Vector3 PushUp = new Vector3(0, 1, 0);

		private readonly float _cosHalfSharpCorner = Mathf.Cos(75f / 2f * (Mathf.PI / 180f));
		private readonly float _sharpCornerOffset = 15f;
		private float _scaledWidth;
		private float _tileSize;
		private List<Vector3> _vertexList;
		private List<Vector3> _normalList;
		private List<int> _triangleList;
		private List<Vector2> _uvList;
		private List<Vector4> _tangentList;
		private int _index1 = -1;
		private int _index2 = -1;
		private int _index3 = -1;
		private float _cornerOffsetA;
		private float _cornerOffsetB;
		private bool _startOfLine = true;
		private Vector3 _prevVertex;
		private Vector3 _currentVertex;
		private Vector3 _nextVertex;
		private Vector3 _prevNormal;
		private Vector3 _nextNormal;
		private float _distance = 0f;

		public ModifierType Type
		{
			get { return ModifierType.Preprocess; }
		}

		public void Initialize()
		{
			_scaledWidth = Width;
			_vertexList = new List<Vector3>();
			_normalList = new List<Vector3>();
			_triangleList = new List<int>();
			_uvList = new List<Vector2>();
			_tangentList = new List<Vector4>();
		}

		public void Run(VectorFeatureUnity feature, MeshData md, float scale)
		{
			_scaledWidth = Width * scale;
			ExtrudeLine(feature, md);
		}

		public void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			_scaledWidth = tile != null ? Width * tile.TileScale : Width;
			_tileSize = Convert.ToSingle(tile.Rect.Size.x * tile.TileScale);
			ExtrudeLine(feature, md);
		}

		private void ExtrudeLine(VectorFeatureUnity feature, MeshData md)
		{
			if (feature.Points.Count < 1)
			{
				return;
			}

			var allPoints = new List<List<Vector3>>();
			foreach (var segment in feature.Points)
			{
				var filteredRoadSegment = new List<Vector3>();
				var tolerance = 0.001f;
				for (int i = 0; i < segment.Count - 1; i++)
				{
					var p1 = segment[i];
					var p2 = segment[i + 1];
					if (!IsOnEdge(p1, p2, _tileSize, tolerance))
					{
						filteredRoadSegment.Add(p1);
						if (i == segment.Count - 2)
						{
							filteredRoadSegment.Add(p2);
						}
					}
					else
					{
						filteredRoadSegment.Add(p1);
						allPoints.Add(filteredRoadSegment);
						filteredRoadSegment = new List<Vector3>();
					}
				}
				allPoints.Add(filteredRoadSegment);
			}

			foreach (var roadSegment in allPoints)
			{
				if (roadSegment.Count < 2)
					continue;

				ResetFields();

				var roadSegmentCount = roadSegment.Count;
				for (int i = 0; i < roadSegmentCount; i++)
				{
					_nextVertex = i != (roadSegmentCount - 1) ? roadSegment[i + 1] : Constants.Math.Vector3Unused;

					if (_nextNormal != Constants.Math.Vector3Unused)
					{
						_prevNormal = _nextNormal;
					}

					if (_currentVertex != Constants.Math.Vector3Unused)
					{
						_prevVertex = _currentVertex;
					}

					_currentVertex = roadSegment[i];

					_nextNormal = (_nextVertex != Constants.Math.Vector3Unused)
						? (_nextVertex - _currentVertex).normalized.Perpendicular()
						: _prevNormal;

					if (_prevNormal == Constants.Math.Vector3Unused)
					{
						_prevNormal = _nextNormal;
					}

					var joinNormal = (_prevNormal + _nextNormal).normalized;

					/*  joinNormal     prevNormal
					 *             ↖      ↑
					 *                .________. prevVertex
					 *                |
					 * nextNormal  ←  |  currentVertex
					 *                |
					 *     nextVertex !
					 *
					 */

					var cosHalfAngle = joinNormal.x * _nextNormal.x + joinNormal.z * _nextNormal.z;
					var miterLength = cosHalfAngle != 0 ? 1 / cosHalfAngle : float.PositiveInfinity;
					var isSharpCorner = cosHalfAngle < _cosHalfSharpCorner && _prevVertex != Constants.Math.Vector3Unused && _nextVertex != Constants.Math.Vector3Unused;

					if (isSharpCorner && i > 0)
					{
						var prevSegmentLength = Vector3.Distance(_currentVertex, _prevVertex);
						if (prevSegmentLength > 2 * _sharpCornerOffset)
						{
							var dir = (_currentVertex - _prevVertex);
							var newPrevVertex = _currentVertex - (dir * (_sharpCornerOffset / prevSegmentLength));
							_distance += Vector3.Distance(newPrevVertex, _prevVertex);
							AddCurrentVertex(newPrevVertex, _distance, _prevNormal, md);
							_prevVertex = newPrevVertex;
						}
					}

					var middleVertex = _prevVertex != Constants.Math.Vector3Unused && _nextVertex != Constants.Math.Vector3Unused;
					var currentJoin = middleVertex ? JoinType : CapType;

					if (middleVertex && currentJoin == JoinType.Round)
					{
						if (miterLength < RoundLimit)
						{
							currentJoin = JoinType.Miter;
						}
						else if (miterLength <= 2)
						{
							currentJoin = JoinType.Fakeround;
						}
					}

					if (currentJoin == JoinType.Miter && miterLength > MiterLimit)
					{
						currentJoin = JoinType.Bevel;
					}

					if (currentJoin == JoinType.Bevel)
					{
						// The maximum extrude length is 128 / 63 = 2 times the width of the line
						// so if miterLength >= 2 we need to draw a different type of bevel here.
						if (miterLength > 2)
						{
							currentJoin = JoinType.Flipbevel;
						}

						// If the miterLength is really small and the line bevel wouldn't be visible,
						// just draw a miter join to save a triangle.
						if (miterLength < MiterLimit)
						{
							currentJoin = JoinType.Miter;
						}
					}

					if (_prevVertex != Constants.Math.Vector3Unused)
					{
						_distance += Vector3.Distance(_currentVertex, _prevVertex);
					}

					if (currentJoin == JoinType.Miter)
					{
						joinNormal *= miterLength;
						AddCurrentVertex(_currentVertex, _distance, joinNormal, md);
					}
					else if (currentJoin == JoinType.Flipbevel)
					{
						// miter is too big, flip the direction to make a beveled join

						if (miterLength > 100)
						{
							// Almost parallel lines
							joinNormal = _nextNormal * -1;
						}
						else
						{
							var direction = (_prevNormal.x * _nextNormal.z - _prevNormal.z * _nextNormal.x) > 0 ? -1 : 1;
							var bevelLength = miterLength * (_prevNormal + _nextNormal).magnitude / (_prevNormal - _nextNormal).magnitude;
							joinNormal = joinNormal.Perpendicular() * (bevelLength * direction);
						}

						AddCurrentVertex(_currentVertex, _distance, joinNormal, md, 0, 0);
						AddCurrentVertex(_currentVertex, _distance, joinNormal * -1, md, 0, 0);
					}
					else if (currentJoin == JoinType.Bevel || currentJoin == JoinType.Fakeround)
					{
						var lineTurnsLeft = (_prevNormal.x * _nextNormal.z - _prevNormal.z * _nextNormal.x) > 0;
						var offset = (float) -Math.Sqrt(miterLength * miterLength - 1);
						if (lineTurnsLeft)
						{
							_cornerOffsetB = 0;
							_cornerOffsetA = offset;
						}
						else
						{
							_cornerOffsetA = 0;
							_cornerOffsetB = offset;
						}

						// Close previous segment with a bevel
						if (!_startOfLine)
						{
							AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, _cornerOffsetA, _cornerOffsetB);
						}

						if (currentJoin == JoinType.Fakeround)
						{
							// The join angle is sharp enough that a round join would be visible.
							// Bevel joins fill the gap between segments with a single pie slice triangle.
							// Create a round join by adding multiple pie slices. The join isn't actually round, but
							// it looks like it is at the sizes we render lines at.

							// Add more triangles for sharper angles.
							// This math is just a good enough approximation. It isn't "correct".
							var n = Mathf.Floor((0.5f - (cosHalfAngle - 0.5f)) * 8);
							Vector3 approxFractionalJoinNormal;
							for (var m = 0f; m < n; m++)
							{
								approxFractionalJoinNormal = (_nextNormal * ((m + 1f) / (n + 1f)) + (_prevNormal)).normalized;
								AddPieSliceVertex(_currentVertex, _distance, approxFractionalJoinNormal, lineTurnsLeft, md);
							}

							AddPieSliceVertex(_currentVertex, _distance, joinNormal, lineTurnsLeft, md);

							//change it to go -1, not sure if it's a good idea but it adds the last vertex in the corner,
							//as duplicate of next road segment start
							for (var k = n - 1; k >= -1; k--)
							{
								approxFractionalJoinNormal = (_prevNormal * ((k + 1) / (n + 1)) + (_nextNormal)).normalized;
								AddPieSliceVertex(_currentVertex, _distance, approxFractionalJoinNormal, lineTurnsLeft, md);
							}

							//ending corner
							_index1 = -1;
							_index2 = -1;
						}

						if (_nextVertex != Constants.Math.Vector3Unused)
						{
							AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, -_cornerOffsetA, -_cornerOffsetB);
						}
					}
					else if (currentJoin == JoinType.Butt)
					{
						if (!_startOfLine)
						{
							// Close previous segment with a butt
							AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, 0, 0);
						}

						// Start next segment with a butt
						if (_nextVertex != Constants.Math.Vector3Unused)
						{
							AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, 0, 0);
						}
					}
					else if (currentJoin == JoinType.Square)
					{
						if (!_startOfLine)
						{
							// Close previous segment with a square cap
							AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, 1, 1);

							// The segment is done. Unset vertices to disconnect segments.
							_index1 = _index2 = -1;
						}

						// Start next segment
						if (_nextVertex != Constants.Math.Vector3Unused)
						{
							AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, -1, -1);
						}
					}
					else if (currentJoin == JoinType.Round)
					{
						if (_startOfLine)
						{
							AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.33f, md, -2f, -2f);
							AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.66f, md, -.7f, -.7f);
							AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, 0, 0);
						}
						else if (_nextVertex == Constants.Math.Vector3Unused)
						{
							AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, 0, 0);
							AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.66f, md, .7f, .7f);
							AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.33f, md, 2f, 2f);
							_index1 = -1;
							_index2 = -1;
						}
						else
						{
							AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, 0, 0);
							AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, 0, 0);
						}
					}

					if (isSharpCorner && i < roadSegmentCount - 1)
					{
						var nextSegmentLength = Vector3.Distance(_currentVertex, _nextVertex);
						if (nextSegmentLength > 2 * _sharpCornerOffset)
						{
							var newCurrentVertex = _currentVertex + ((_nextVertex - _currentVertex) * (_sharpCornerOffset / nextSegmentLength)); //._round()
							_distance += Vector3.Distance(newCurrentVertex, _currentVertex);
							AddCurrentVertex(newCurrentVertex, _distance, _nextNormal, md);
							_currentVertex = newCurrentVertex;
						}
					}

					_startOfLine = false;
				}

				md.Edges.Add(md.Vertices.Count);
				md.Edges.Add(md.Vertices.Count + 1);
				md.Edges.Add(md.Vertices.Count + _vertexList.Count - 1);
				md.Edges.Add(md.Vertices.Count + _vertexList.Count - 2);

				md.Vertices.AddRange(_vertexList);
				md.Normals.AddRange(_normalList);
				if (md.Triangles.Count == 0)
				{
					md.Triangles.Add(new List<int>());
				}

				md.Triangles[0].AddRange(_triangleList);
				md.Tangents.AddRange(_tangentList);
				md.UV[0].AddRange(_uvList);
			}
		}



		private static bool IsOnEdge(Vector3 p1, Vector3 p2, float _tileSize, float tolerance)
		{
			return ((Math.Abs(Math.Abs(p1.x) - (_tileSize/2)) < tolerance && Math.Abs(Math.Abs(p2.x) - (_tileSize/2)) < tolerance && Math.Sign(p1.x) == Math.Sign(p2.x)) ||
			        (Math.Abs(Math.Abs(p1.z) - (_tileSize/2)) < tolerance && Math.Abs(Math.Abs(p2.z) - (_tileSize/2)) < tolerance && Math.Sign(p1.z) == Math.Sign(p2.z)));
		}

		private void ResetFields()
		{
			_index1 = -1;
			_index2 = -1;
			_index3 = -1;
			_startOfLine = true;
			_cornerOffsetA = 0f;
			_cornerOffsetB = 0f;

			_vertexList.Clear();
			_normalList.Clear();
			_uvList.Clear();
			_tangentList.Clear();
			_triangleList.Clear();

			_prevVertex = Constants.Math.Vector3Unused;
			_currentVertex = Constants.Math.Vector3Unused;
			_nextVertex = Constants.Math.Vector3Unused;

			_prevNormal = Constants.Math.Vector3Unused;
			_nextNormal = Constants.Math.Vector3Unused;
			_distance = 0f;
		}

		private void AddPieSliceVertex(Vector3 vertexPosition, float dist, Vector3 normal, bool lineTurnsLeft, MeshData md)
		{
			var triIndexStart = md.Vertices.Count;
			var extrude = normal * (lineTurnsLeft ? -1 : 1);
			_vertexList.Add(vertexPosition + extrude * _scaledWidth + PushUp);
			_normalList.Add(Constants.Math.Vector3Up);
			_uvList.Add(new Vector2(1, dist));
			_tangentList.Add(normal.Perpendicular() * -1);

			_index3 = triIndexStart + _vertexList.Count - 1;
			if (_index1 >= 0 && _index2 >= 0)
			{
				_triangleList.Add(_index1);
				_triangleList.Add(_index3);
				_triangleList.Add(_index2);
				if (!lineTurnsLeft)
				{
					md.Edges.Add(_index3);
					md.Edges.Add(_index1);
				}
				else
				{
					md.Edges.Add(_index2);
					md.Edges.Add(_index3);
				}
			}

			if (lineTurnsLeft)
			{
				_index2 = _index3;
			}
			else
			{
				_index1 = _index3;
			}
		}

		private void AddCurrentVertex(Vector3 vertexPosition, float dist, Vector3 normal, MeshData md, float endLeft = 0, float endRight = 0)
		{
			var triIndexStart = md.Vertices.Count;
			var extrude = normal;
			if (endLeft != 0)
			{
				extrude -= (normal.Perpendicular() * endLeft);
			}

			var vert = vertexPosition + extrude * _scaledWidth;
			_vertexList.Add(vert + PushUp);
			_normalList.Add(Constants.Math.Vector3Up);
			_uvList.Add(new Vector2(1, dist));
			_tangentList.Add(normal.Perpendicular() * -1);

			_index3 = triIndexStart + _vertexList.Count - 1;
			if (_index1 >= triIndexStart && _index2 >= triIndexStart)
			{
				_triangleList.Add(_index1);
				_triangleList.Add(_index3);
				_triangleList.Add(_index2);
				md.Edges.Add(triIndexStart + _vertexList.Count - 1);
				md.Edges.Add(triIndexStart + _vertexList.Count - 3);
			}

			_index1 = _index2;
			_index2 = _index3;


			extrude = normal * -1;
			if (endRight != 0)
			{
				extrude -= normal.Perpendicular() * endRight;
			}

			_vertexList.Add(vertexPosition + extrude * _scaledWidth + PushUp);
			_normalList.Add(Constants.Math.Vector3Up);
			_uvList.Add(new Vector2(0, dist));
			_tangentList.Add(normal.Perpendicular() * -1);

			_index3 = triIndexStart + _vertexList.Count - 1;
			if (_index1 >= triIndexStart && _index2 >= triIndexStart)
			{
				_triangleList.Add(_index1);
				_triangleList.Add(_index2);
				_triangleList.Add(_index3);
				md.Edges.Add(triIndexStart + _vertexList.Count - 3);
				md.Edges.Add(triIndexStart + _vertexList.Count - 1);
			}

			_index1 = _index2;
			_index2 = _index3;
		}
	}

	public interface ICoreWrapper
	{
		void SetCore(LineMeshCore core);
	}
	/// <summary>
	/// Line Mesh Modifier creates line polygons from a list of vertices. It offsets the original vertices to both sides using Width parameter and triangulates them manually.
	/// It also creates tiled UV mapping using the line length.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Line Mesh For Polygons Modifier")]
	public class LineMeshForPolygonsModifier : MeshModifier, ICoreWrapper
	{
		#region Line Parameters

		//[SerializeField] private LineGeometryOptions _options;

		[Tooltip("Width of the line feature.")]
		public float Width = 1.0f;
		[Tooltip("Miter Limit")]
		public float MiterLimit = 0.2f;

		[Tooltip("Round Limit")]
		public float RoundLimit = 1.05f;

		[Tooltip("Join type of the line feature")]
		public JoinType JoinType = JoinType.Round;

		[Tooltip("Cap type of the line feature")]
		public JoinType CapType = JoinType.Round;

		public Vector3 PushUp = new Vector3(0, 1, 0);
		#endregion

		#region Constants

		#endregion

		#region Mesh Generation Fields

		//triangle indices

		private LineMeshCore _lineMeshCore;

		#endregion

		#region Modifier Overrides

		public override ModifierType Type
		{
			get { return _lineMeshCore.Type; }
		}

		public void SetCore(LineMeshCore core)
		{
			_lineMeshCore = core;
			_lineMeshCore.Width = Width;
			_lineMeshCore.MiterLimit = MiterLimit;
			_lineMeshCore.RoundLimit = RoundLimit;
			_lineMeshCore.JoinType = JoinType;
			_lineMeshCore.CapType = CapType;
			_lineMeshCore.PushUp = PushUp;
			_lineMeshCore.Initialize();
		}

		public override void Initialize()
		{
			_lineMeshCore?.Initialize();
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, float scale)
		{
			_lineMeshCore.Run(feature, md, scale);
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			_lineMeshCore.Run(feature, md, tile);
		}

		#endregion
	}
}