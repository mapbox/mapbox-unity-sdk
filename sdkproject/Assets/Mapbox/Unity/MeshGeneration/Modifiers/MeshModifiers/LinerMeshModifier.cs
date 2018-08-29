using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	public class Segment
	{
	}

	public enum JoinType
	{
		Join,
		BeginCap,
		EndCap
	}

	/// <summary>
	/// Line Mesh Modifier creates line polygons from a list of vertices. It offsets the original vertices to both sides using Width parameter and triangulates them manually.
	/// It also creates tiled UV mapping using the line length.
	/// MergeStartEnd parameter connects both edges of the line segment and creates a closed loop which is useful for some cases like pavements around a building block.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Liner Mesh Modifier")]
	public class LinerMeshModifier : MeshModifier
	{
		private readonly float COS_HALF_SHARP_CORNER = Mathf.Cos(75f / 2f * (Mathf.PI / 180f));
		private readonly float SHARP_CORNER_OFFSET = 15f;
		private float miterLimit = 1;

		[SerializeField] public float Width = 3.0f;
		private float _scaledWidth;

		public override ModifierType Type
		{
			get { return ModifierType.Preprocess; }
		}

		protected virtual void OnEnable()
		{
			_scaledWidth = Width;
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, float scale)
		{
			_scaledWidth = Width * scale;
			ExtureLine(feature, md);
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			string Join = "miter";
			var isPolygon = feature.Properties["type"] == "Polygon";

			foreach (var vertices in feature.Points)
			{
				// If the line has duplicate vertices at the ends, adjust start/length to remove them.
				var len = vertices.Count;
				while (len >= 2 && vertices[len - 1] == vertices[len - 2])
				{
					len--;
				}

				var first = 0;
				while (first < len - 1 && vertices[first] == vertices[first + 1])
				{
					first++;
				}

				// Ignore invalid geometry.
				if ((len - first) < (isPolygon ? 3 : 2)) return;

//				if (lineDistances)
//				{
//					lineDistances.tileTotal = calculateFullDistance(vertices, first, len);
//				}

				if (Join == "bevel")
				{
					miterLimit = 1.05f;
				}

				var firstVertex = vertices[first];
				var segment = PrepareSegment(len * 10);
				var distance = 0f;

				var startOfLine = true;
				var currentVertex = Vector3.zero;
				var prevVertex = Vector3.zero;
				var nextVertex = Vector3.zero;
				var prevNormal = Vector3.zero;
				var nextNormal = Vector3.zero;
				float offsetA;
				float offsetB;
				var e1 = -1;
				var e2 = -1;
				var e3 = -1;

				if (isPolygon)
				{
					currentVertex = vertices[len - 2];
					nextNormal = (firstVertex - currentVertex).normalized.Perpendicular();
				}

				for (var i = first; i < len; i++)
				{
					if (isPolygon)
					{
						nextVertex = i == (len - 1)
							? vertices[first + 1]
							: vertices[i + 1];
					}
					else
					{
						nextVertex = i != (len - 1)
							? vertices[i + 1]
							: Constants.Math.Vector3Zero;
					}

					// if two consecutive vertices exist, skip the current one
					if (vertices[i] == nextVertex)
					{
						continue;
					}

					//setting prev normal the value (nextNormal) we calculated in previous cycle
					if (nextNormal != Constants.Math.Vector3Zero)
					{
						prevNormal = nextNormal;
					}

					//prevVertex equals to currentVertex in first cycle
					if (currentVertex != Constants.Math.Vector3Zero)
					{
						prevVertex = currentVertex;
					}

					currentVertex = vertices[i];
					// Calculate the normal towards the next vertex in this line. In case
					// there is no next vertex, pretend that the line is continuing straight,
					// meaning that we are just using the previous normal.
					nextNormal = nextVertex != Constants.Math.Vector3Zero ? (nextVertex - currentVertex).normalized.Perpendicular() : prevNormal;
					// If we still don't have a previous normal, this is the beginning of a
					// non-closed line, so we're doing a straight "join".
					if (prevNormal == Constants.Math.Vector3Zero)
					{
						prevNormal = nextNormal;
					}

					// Determine the normal of the join extrusion. It is the angle bisector
					// of the segments between the previous line and the next line.
					// In the case of 180° angles, the prev and next normals cancel each other out:
					// prevNormal + nextNormal = (0, 0), its magnitude is 0, so the unit vector would be
					// undefined. In that case, we're keeping the joinNormal at (0, 0), so that the cosHalfAngle
					// below will also become 0 and miterLength will become Infinity.
					var joinNormal = prevNormal + nextNormal;
					if (joinNormal.x != 0 || joinNormal.y != 0)
					{
						joinNormal.Normalize();
					}

					/*  joinNormal     prevNormal
					*             ↖      ↑
					*                .________. prevVertex
					*                |
					* nextNormal  ←  |  currentVertex
					*                |
					*     nextVertex !
					*
					*/

					// Calculate the length of the miter (the ratio of the miter to the width).
					// Find the cosine of the angle between the next and join normals
					// using dot product. The inverse of that is the miter length.
					var cosHalfAngle = joinNormal.x * nextNormal.x + joinNormal.y * nextNormal.y;
					var miterLength = cosHalfAngle != 0 ? 1 / cosHalfAngle : Mathf.Infinity;
					var isSharpCorner = cosHalfAngle < COS_HALF_SHARP_CORNER && prevVertex != Constants.Math.Vector3Zero && nextVertex != Constants.Math.Vector3Zero;

					if (isSharpCorner && i > first)
					{
						var prevSegmentLength = Vector3.Distance(currentVertex, prevVertex);
						if (prevSegmentLength > 2 * SHARP_CORNER_OFFSET)
						{
							var newPrevVertex = currentVertex - ((currentVertex - prevVertex) * (SHARP_CORNER_OFFSET / prevSegmentLength));
							distance += Vector3.Distance(newPrevVertex, prevVertex);
							AddCurrentVertex(newPrevVertex, distance, prevNormal, 0, 0, false, segment);
							prevVertex = newPrevVertex;
						}
					}

					// The join if a middle vertex, otherwise the cap.
					var middleVertex = prevVertex != Constants.Math.Vector3Zero && nextVertex != Constants.Math.Vector3Zero;
					var currentJoin = middleVertex ? Join : (nextVertex != Constants.Math.Vector3Zero) ? "BeginCap" : "EndCap";

//					if (middleVertex && currentJoin == "round")
//					{
//						if (miterLength < roundLimit)
//						{
//							currentJoin = 'miter';
//						}
//						else if (miterLength <= 2)
//						{
//							currentJoin = 'fakeround';
//						}
//					}

					if (currentJoin == "miter" && miterLength > miterLimit)
					{
						currentJoin = "bevel";
					}

					if (currentJoin == "bevel")
					{
						// The maximum extrude length is 128 / 63 = 2 times the width of the line
						// so if miterLength >= 2 we need to draw a different type of bevel here.
						if (miterLength > 2)
						{
							currentJoin = "flipbevel";
						}

						// If the miterLength is really small and the line bevel wouldn't be visible,
						// just draw a miter join to save a triangle.
						if (miterLength < miterLimit)
						{
							currentJoin = "miter";
						}
					}

					if (prevVertex != Constants.Math.Vector3Zero)
					{
						distance += Vector3.Distance(currentVertex, prevVertex);
					}

					if (currentJoin == "miter")
					{
						joinNormal *= miterLength;
						AddCurrentVertex(currentVertex, distance, joinNormal, 0, 0, false, segment);
					}
					
					if (isSharpCorner && i < len - 1) 
					{
						var nextSegmentLength = Vector3.Distance(currentVertex, nextVertex);
						if (nextSegmentLength > 2 * SHARP_CORNER_OFFSET) 
						{
							var newCurrentVertex = currentVertex + ((nextVertex - currentVertex) * (SHARP_CORNER_OFFSET / nextSegmentLength)); //._round()
							distance += Vector3.Distance(newCurrentVertex, currentVertex);
							AddCurrentVertex(newCurrentVertex, distance, nextNormal, 0, 0, false, segment);
							currentVertex = newCurrentVertex;
						}
					}

					startOfLine = false;
				}
			}
		}

		private Segment PrepareSegment(int i)
		{
			return new Segment();
		}

		private void AddCurrentVertex(Vector3 newPrevVertex, float distance, Vector3 mult, int i, int i1, bool b, Segment segment)
		{
		}

//		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
//		{
//			_scaledWidth = tile != null ? Width * tile.TileScale : Width;
//			ExtureLine(feature, md);
//		}

		private void ExtureLine(VectorFeatureUnity feature, MeshData md)
		{
			if (feature.Points.Count < 1)
				return;

			foreach (var roadSegment in feature.Points)
			{
				var mdVertexCount = md.Vertices.Count;
				var roadSegmentCount = roadSegment.Count;
				for (int i = 1; i < roadSegmentCount * 2; i++)
				{
					md.Edges.Add(mdVertexCount + i);
					md.Edges.Add(mdVertexCount + i - 1);
				}

				md.Edges.Add(mdVertexCount);
				md.Edges.Add(mdVertexCount + (roadSegmentCount * 2) - 1);

				var newVerticeList = new Vector3[roadSegmentCount * 2];
				var newNorms = new Vector3[roadSegmentCount * 2];
				var uvList = new Vector2[roadSegmentCount * 2];
				var newTangents = new Vector4[roadSegmentCount * 2];
				Vector3 norm;
				var lastUv = 0f;
				var p1 = Constants.Math.Vector3Zero;
				var p2 = Constants.Math.Vector3Zero;
				var p3 = Constants.Math.Vector3Zero;
				for (int i = 1; i < roadSegmentCount; i++)
				{
					p1 = roadSegment[i - 1];
					p2 = roadSegment[i];
					p3 = p2;
					if (i + 1 < roadSegmentCount)
						p3 = roadSegment[i + 1];

					if (i == 1)
					{
						norm = GetNormal(p1, p1, p2) * _scaledWidth; //road width
						newVerticeList[0] = (p1 + norm);
						newVerticeList[roadSegmentCount * 2 - 1] = (p1 - norm);
						newNorms[0] = Constants.Math.Vector3Up;
						newNorms[roadSegmentCount * 2 - 1] = Constants.Math.Vector3Up;
						uvList[0] = new Vector2(0, 0);
						uvList[roadSegmentCount * 2 - 1] = new Vector2(1, 0);
						newTangents[0] = new Vector4(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z, 1).normalized;
						newTangents[roadSegmentCount * 2 - 1] = newTangents[0];
					}

					var dist = Vector3.Distance(p1, p2);
					lastUv += dist;
					norm = GetNormal(p1, p2, p3) * _scaledWidth;
					newVerticeList[i] = (p2 + norm);
					newVerticeList[2 * roadSegmentCount - 1 - i] = (p2 - norm);
					newNorms[i] = Constants.Math.Vector3Up;
					newNorms[2 * roadSegmentCount - 1 - i] = Constants.Math.Vector3Up;

					uvList[i] = new Vector2(0, lastUv);
					uvList[2 * roadSegmentCount - 1 - i] = new Vector2(1, lastUv);

					newTangents[i] = new Vector4(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z, 1).normalized;
					newTangents[2 * roadSegmentCount - 1 - i] = newTangents[i];
				}

				md.Vertices.AddRange(newVerticeList);
				md.Normals.AddRange(newNorms);
				md.UV[0].AddRange(uvList);
				md.Tangents.AddRange(newTangents);
				var lineTri = new List<int>();
				var n = roadSegmentCount;

				for (int i = 0; i < n - 1; i++)
				{
					lineTri.Add(mdVertexCount + i);
					lineTri.Add(mdVertexCount + i + 1);
					lineTri.Add(mdVertexCount + 2 * n - 1 - i);

					lineTri.Add(mdVertexCount + i + 1);
					lineTri.Add(mdVertexCount + 2 * n - i - 2);
					lineTri.Add(mdVertexCount + 2 * n - i - 1);
				}

				if (md.Triangles.Count < 1)
					md.Triangles.Add(new List<int>());
				md.Triangles[0].AddRange(lineTri);
			}
		}

		private Vector3 GetNormal(Vector3 p1, Vector3 newPos, Vector3 p2)
		{
			if (newPos == p1 || newPos == p2)
			{
				var n = (p2 - p1).normalized;
				return new Vector3(-n.z, 0, n.x);
			}

			var b = (p2 - newPos).normalized + newPos;
			var a = (p1 - newPos).normalized + newPos;
			var t = (b - a).normalized;

			if (t == Mapbox.Unity.Constants.Math.Vector3Zero)
			{
				var n = (p2 - p1).normalized;
				return new Vector3(-n.z, 0, n.x);
			}

			return new Vector3(-t.z, 0, t.x);
		}
	}
}