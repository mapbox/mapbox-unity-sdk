namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;
	using System;
	using System.Linq;

	/// <summary>
	/// Polygon modifier creates the polygon (vertex&triangles) using the original vertex list.
	/// Currently uses Triangle.Net for triangulation, which occasionally adds extra vertices to maintain a good triangulation so output vertex list might not be exactly same as the original vertex list.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
	public class PolygonMeshModifier : MeshModifier
	{
		public override ModifierType Type { get { return ModifierType.Preprocess; } }
		private int _counter, _secondCounter;
		private Vector3 _v1, _v2;
		
		public bool IsClockwise(IList<Vector3> vertices)
		{
			double sum = 0.0;
			_counter = vertices.Count;
			for (int i = 0; i < _counter; i++)
			{
				_v1 = vertices[i];
				_v2 = vertices[(i + 1) % _counter];
				sum += (_v2.x - _v1.x) * (_v2.z + _v1.z);
			}
			return sum > 0.0;
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (
				(feature.Points.Count == 1 && feature.Points[0].Count < 6 && Convert.ToSingle(feature.Properties["height"]) < 50) ||
				(feature.Points.Count == 1 && feature.Points[0].Count < 12 && Convert.ToSingle(feature.Properties["height"]) < 5))
			{
				PyramidRoof(feature, md);
			}
			else
			{
				FlatRoof(feature, md);
			}
		}

		private void FlatRoof(VectorFeatureUnity feature, MeshData md)
		{
			_secondCounter = feature.Points.Count;
			var subset = new List<List<Vector3>>(_secondCounter);
			Data flatData = null;
			List<int> result = null;
			var currentIndex = 0;
			int vertCount = 0, c2 = 0;
			List<int> triList = null;
			List<Vector3> sub = null;

			for (int i = 0; i < _secondCounter; i++)
			{
				sub = feature.Points[i];
				//earcut is built to handle one polygon with multiple holes
				//point data can contain multiple polygons though, so we're handling them separately here

				vertCount = md.Vertices.Count;
				if (IsClockwise(sub) && vertCount > 0)
				{
					flatData = EarcutLibrary.Flatten(subset);
					result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);
					c2 = result.Count;
					if (triList == null)
					{
						triList = new List<int>(c2);
					}
					else
					{
						triList.Capacity = triList.Count + c2;
					}

					for (int j = 0; j < c2; j++)
					{
						triList.Add(result[j] + currentIndex);
					}
					currentIndex = vertCount;
					subset.Clear();
				}

				subset.Add(sub);

				c2 = sub.Count;
				md.Vertices.Capacity = md.Vertices.Count + c2;
				md.Normals.Capacity = md.Normals.Count + c2;
				md.Edges.Capacity = md.Edges.Count + c2 * 2;
				for (int j = 0; j < c2; j++)
				{
					md.Edges.Add(vertCount + ((j + 1) % c2));
					md.Edges.Add(vertCount + j);
					md.Vertices.Add(sub[j]);
					md.Normals.Add(Constants.Math.Vector3Up);
					md.Tangents.Add(Constants.Math.Vector3Forward);
				}

			}

			flatData = EarcutLibrary.Flatten(subset);
			result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);
			c2 = result.Count;
			if (triList == null)
			{
				triList = new List<int>(c2);
			}
			else
			{
				triList.Capacity = triList.Count + c2;
			}
			for (int i = 0; i < c2; i++)
			{
				triList.Add(result[i] + currentIndex);
			}

			md.Triangles.Add(triList);
		}

		private void PyramidRoof(VectorFeatureUnity feature, MeshData md)
		{
			var cc = md.Vertices.Count;
			foreach (var poly in feature.Points)
			{
				for (int i = 0; i < poly.Count - 1; i++)
				{
					md.Vertices.Add(poly[i]);
					md.Vertices.Add(poly[i + 1]);

					md.Normals.Add(Constants.Math.Vector3Up);
					md.Normals.Add(Constants.Math.Vector3Up);

					md.Tangents.Add(Constants.Math.Vector3Forward);
					md.Tangents.Add(Constants.Math.Vector3Forward);
					md.Edges.Add(cc + 1);
					md.Edges.Add(cc + 0);
					cc += 2;
				}
			}

			var bb = Calculate(feature.Points[0]);
			Vector3 sum = Vector3.zero;
			for (int i = 0; i < bb.Count; i++)
			{
				sum += bb[i];
			}
			sum /= bb.Count;
			sum += Constants.Math.Vector3Up;
			bb.Add(bb[0]);

			var trilist = new List<int>();

			for (int i = 0; i < bb.Count - 1; i++)
			{
				md.Vertices.Add(sum);
				md.Vertices.Add(bb[i]);
				md.Vertices.Add(bb[i + 1]);

				var norm = Vector3.Cross(bb[i + 1] - bb[i], sum - bb[i]).normalized;
				md.Normals.Add(norm);
				md.Normals.Add(norm);
				md.Normals.Add(norm);

				md.Tangents.Add(Constants.Math.Vector3Forward);
				md.Tangents.Add(Constants.Math.Vector3Forward);
				md.Tangents.Add(Constants.Math.Vector3Forward);

				trilist.Add(cc);
				trilist.Add(cc + 2);
				trilist.Add(cc + 1);
				cc += 3;
			}

			cc = md.Vertices.Count;
			md.Triangles.Add(trilist);
		}

		public static double Cross(Vector3 o, Vector3 a, Vector3 b)
		{
			return (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);
		}

		private int _hullPointCount;
		private int _hullCounter;
		private List<Vector3> _hullPoints;
		private Vector3 currentHullPoint;
		private Vector3 nextHullPoint;
		private Vector3 otherCurrentHullPoint;
		private float top = float.MinValue;
		private float bottom = float.MaxValue;
		private float left = float.MaxValue;
		private float right = float.MinValue;

		private Vector3 _delta;
		private double _angle;
		private Vector3 _rotatedPoint;
		private double _minAngle;

		public List<Vector3> MonotoneChainConvexHull(List<Vector3> points)
		{
			points.Sort((x, y) => { return (int)((x.x * 1000 + x.z) - (y.x * 1000 + y.z)); });
			_hullPoints = new List<Vector3>(2 * points.Count);

			//break if only one point as input
			if (points.Count <= 1)
				return points;

			_hullPointCount = points.Count;
			_hullCounter = 0;

			//iterate for lowerHull
			for (var i = 0; i < _hullPointCount; ++i)
			{
				while (_hullCounter >= 2 && Cross(_hullPoints[_hullCounter - 2], _hullPoints[_hullCounter - 1], points[i]) <= 0)
				{
					_hullCounter--;
				}
				_hullPoints.Insert(_hullCounter++, points[i]);
			}

			//iterate for upperHull
			for (int i = _hullPointCount - 2, j = _hullCounter + 1; i >= 0; i--)
			{
				while (_hullCounter >= j && Cross(_hullPoints[_hullCounter - 2], _hullPoints[_hullCounter - 1], points[i]) <= 0)
				{
					_hullCounter--;
				}
				_hullPoints.Insert(_hullCounter++, points[i]);
			}
			return _hullPoints;
		}

		public List<Vector3> Calculate(List<Vector3> points)
		{
			_hullPoints = MonotoneChainConvexHull(points);

			if (_hullPoints.Count <= 1)
				return _hullPoints;

			Rectangle2d minBox = null;
			_minAngle = 0d;

			//foreach edge of the convex hull
			for (var i = 0; i < _hullPoints.Count; i++)
			{
				currentHullPoint = _hullPoints[i];
				nextHullPoint = _hullPoints[(i + 1) % _hullPoints.Count];

				//get angle of segment to x axis
				_delta = currentHullPoint - nextHullPoint;
				_angle = -Math.Atan(_delta.z / _delta.x);

				//rotate every point and get min and max values for each direction
				for (int j = 0; j < _hullPoints.Count; j++)
				{
					otherCurrentHullPoint = _hullPoints[j];
					_rotatedPoint = new Vector3(
						(float)(otherCurrentHullPoint.x * Math.Cos(_angle) - otherCurrentHullPoint.z * Math.Sin(_angle)),
						0,
						(float)(otherCurrentHullPoint.x * Math.Sin(_angle) + otherCurrentHullPoint.z * Math.Cos(_angle)));

					top = Math.Max(top, _rotatedPoint.z);
					bottom = Math.Min(bottom, _rotatedPoint.z);

					left = Math.Min(left, _rotatedPoint.x);
					right = Math.Max(right, _rotatedPoint.x);
				}

				//create axis aligned bounding box
				var box = new Rectangle2d(new Vector3(left, 0, bottom), new Vector3(right, 0, top));

				if (minBox == null || minBox.Area() > box.Area())
				{
					minBox = box;
					_minAngle = _angle;
				}
			}

			//rotate axis algined box back
			for (int i = 0; i < minBox.Points.Count; i++)
			{
				currentHullPoint = minBox.Points[i];
				minBox.Points[i] = new Vector3(
					(float)(currentHullPoint.x * Math.Cos(-_minAngle) - minBox.Points[i].z * Math.Sin(-_minAngle)), 
					0, 
					(float)(currentHullPoint.x * Math.Sin(-_minAngle) + minBox.Points[i].z * Math.Cos(-_minAngle)));
			}

			return minBox.Points;
		}
	}

	public class Rectangle2d
	{
		public Vector3 Location { get; set; }
		public Vector3 Size { get; set; }
		public List<Vector3> Points;

		public Rectangle2d()
		{
		}

		public Rectangle2d(Vector3 a, Vector3 c)
		{
			Location = a;
			Size = c - a;

			Points = new List<Vector3>(4)
			{
				new Vector3 (Location.x, 0, Location.z),
					new Vector3 (Location.x + Size.x, 0, Location.z),
					new Vector3 (Location.x + Size.x, 0, Location.z + Size.z),
					new Vector3 (Location.x, 0, Location.z + Size.z)
			};
		}

		public double Area()
		{
			return Size.x * Size.z;
		}
	}

}
