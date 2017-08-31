namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;

	/// <summary>
	/// Line Mesh Modifier creates line polygons from a list of vertices. It offsets the original vertices to both sides using Width parameter and triangulates them manually.
	/// It also creates tiled UV mapping using the line length.
	/// MergeStartEnd parameter connects both edges of the line segment and creates a closed loop which is useful for some cases like pavements around a building block.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Loft Modifier")]
	public class LoftModifier : MeshModifier
	{
		[SerializeField]
		private float Width;
		public override ModifierType Type { get { return ModifierType.Preprocess; } }
		public GameObject Slice;
		public bool _closeEdges = false;

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (feature.Points.Count < 1)
				return;

			Vector3 v1, v2, n1, n2, pij1, pij2, pjk1, pjk2;
			Vector3 poi, close1, close2;

			foreach (var roadSegment in feature.Points)
			{
				var count = roadSegment.Count;
				if (count <= 1)
					continue;

				var dotCount = Slice.transform.childCount;
				var vl = new List<Vector3>();
				var tl = new List<int>();
				var nl = new List<Vector3>();
				var edges = new List<Vector3>();

				for (int j = 0; j < count; j++)
				{
					var prev = Constants.Math.Vector3Zero;
					var current = Constants.Math.Vector3Zero;
					var next = Constants.Math.Vector3Zero;

					if (j > 0)
					{
						prev = roadSegment[j - 1];
					}

					current = roadSegment[j];
					next = current;
					if (j + 1 < roadSegment.Count)
					{
						next = roadSegment[j + 1];
					}
					else
					{
						next = (current - prev) + current + new Vector3(1, 0, 1);
					}
					if (j == 0)
					{
						prev = (current - next) + current + new Vector3(1, 0, 1);
					}

					var counter = 0;
					foreach (Transform tr in Slice.transform)
					{
						counter++;
						v1 = new Vector3(current.x - next.x, 0, current.z - next.z);
						v1.Normalize();
						var a = Vector3.Angle(v1, Vector3.forward) * Mathf.Sign(Vector3.Dot(Vector3.right, v1));
						n1 = Quaternion.Euler(0, a, 0) * tr.position;
						pij1 = new Vector3((next.x + n1.x), 0, (next.z + n1.z));
						pij2 = new Vector3((current.x + n1.x), 0, (current.z + n1.z));
						//Debug.DrawLine(pij1 * tile.transform.lossyScale.x, pij2 * tile.transform.lossyScale.x, Color.red, 10000);

						v2 = new Vector3(prev.x - current.x, 0, prev.z - current.z);
						v2.Normalize();
						a = Vector3.Angle(v2, Vector3.forward) * Mathf.Sign(Vector3.Dot(Vector3.right, v2));
						n2 = Quaternion.Euler(0, a, 0) * tr.position;
						pjk1 = new Vector3((current.x + n2.x), 0, (current.z + n2.z));
						pjk2 = new Vector3((prev.x + n2.x), 0, (prev.z + n2.z));
						//Debug.DrawLine(pjk1 * tile.transform.lossyScale.x, pjk2 * tile.transform.lossyScale.x, Color.red, 10000);

						// See where the shifted lines ij and jk intersect.
						bool lines_intersect, segments_intersect;

						FindIntersection(pij1, pij2, pjk1, pjk2,
							out lines_intersect, out segments_intersect,
							out poi, out close1, out close2);
						if (!float.IsNaN(poi.x))
						{
							//var re = GameObject.CreatePrimitive(PrimitiveType.Cube);
							//re.transform.position = poi + new Vector3(0, tr.position.y, 0);
							//re.transform.SetParent(tile.transform, false);
							poi += new Vector3(0, tr.position.y, 0);
						}
						else
						{
							//var re = GameObject.CreatePrimitive(PrimitiveType.Cube);
							//re.transform.position = pjk1 + new Vector3(0, tr.position.y, 0);
							//re.transform.SetParent(tile.transform, false);
							poi = pjk1 + new Vector3(0, tr.position.y, 0);
						}
						vl.Add(poi);
						if (counter == dotCount)
						{
							//var re = GameObject.CreatePrimitive(PrimitiveType.Cube);
							//re.transform.position = poi;
							//re.transform.SetParent(tile.transform, false);
							edges.Add(poi);
						}
					}
				}

				if (md.Triangles.Count == 0)
					md.Triangles.Add(new List<int>());

				for (int i = 0; i < vl.Count - dotCount; i++)
				{
					if ((i + 1) % dotCount == 0)
						continue;

					var co = md.Vertices.Count;
					var norm = Vector3.Cross(vl[i] - vl[i + 1], vl[i + dotCount] - vl[i]).normalized * -1;
					md.Vertices.Add(vl[i]);
					md.Vertices.Add(vl[i + 1]);
					md.Vertices.Add(vl[i + dotCount]);
					md.Vertices.Add(vl[i + dotCount + 1]);

					md.Normals.Add(norm);
					md.Normals.Add(norm);
					md.Normals.Add(norm);
					md.Normals.Add(norm);

					md.Triangles[0].Add(co);
					md.Triangles[0].Add(co + 2);
					md.Triangles[0].Add(co + 1);

					md.Triangles[0].Add(co + 1);
					md.Triangles[0].Add(co + 2);
					md.Triangles[0].Add(co + 3);
				}

				if (md.Triangles.Count < 2)
					md.Triangles.Add(new List<int>());
				if (_closeEdges && edges.Count > 2)
				{
					var flatData = EarcutLibrary.Flatten(new List<List<Vector3>>() { edges });
					var result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);

					md.Triangles[1].AddRange(result.Select(x => md.Vertices.Count + x).ToList());
					for (int i = 0; i < edges.Count; i++)
					{
						md.Vertices.Add(edges[i]);
						md.Normals.Add(Vector3.up);
					}
				}
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

		private void FindIntersection(
			Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
			out bool lines_intersect, out bool segments_intersect,
			out Vector3 intersection,
			out Vector3 close_p1, out Vector3 close_p2)
		{
			// Get the segments' parameters.
			float dx12 = p2.x - p1.x;
			float dy12 = p2.z - p1.z;
			float dx34 = p4.x - p3.x;
			float dy34 = p4.z - p3.z;

			// Solve for t1 and t2
			float denominator = (dy12 * dx34 - dx12 * dy34);

			float t1 =
				((p1.x - p3.x) * dy34 + (p3.z - p1.z) * dx34)
					/ denominator;
			if (float.IsInfinity(t1))
			{
				// The lines are parallel (or close enough to it).
				lines_intersect = false;
				segments_intersect = false;
				intersection = new Vector3(float.NaN, 0, float.NaN);
				close_p1 = new Vector3(float.NaN, 0, float.NaN);
				close_p2 = new Vector3(float.NaN, 0, float.NaN);
				return;
			}
			lines_intersect = true;

			float t2 =
				((p3.x - p1.x) * dy12 + (p1.z - p3.z) * dx12)
					/ -denominator;

			// Find the point of intersection.
			intersection = new Vector3(p1.x + dx12 * t1, 0, p1.z + dy12 * t1);

			// The segments intersect if t1 and t2 are between 0 and 1.
			segments_intersect =
				((t1 >= 0) && (t1 <= 1) &&
				 (t2 >= 0) && (t2 <= 1));

			// Find the closest points on the segments.
			if (t1 < 0)
			{
				t1 = 0;
			}
			else if (t1 > 1)
			{
				t1 = 1;
			}

			if (t2 < 0)
			{
				t2 = 0;
			}
			else if (t2 > 1)
			{
				t2 = 1;
			}

			close_p1 = new Vector3(p1.x + dx12 * t1, 0, p1.z + dy12 * t1);
			close_p2 = new Vector3(p3.x + dx34 * t2, 0, p3.z + dy34 * t2);
		}
	}
}
