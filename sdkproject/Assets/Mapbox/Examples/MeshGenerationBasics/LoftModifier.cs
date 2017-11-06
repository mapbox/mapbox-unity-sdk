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
		public override ModifierType Type { get { return ModifierType.Preprocess; } }
		public GameObject Slice;
		public bool _closeEdges = false;
		private int _counter = 0;

		private List<Vector3> _slice;
		private int _sliceCount;

		public override void Initialize()
		{
			base.Initialize();
			_slice = new List<Vector3>();
			foreach (Transform tr in Slice.transform)
			{
				_slice.Add(tr.position);
			}
			_sliceCount = _slice.Count;
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (feature.Points.Count < 1)
				return;

			var vertexCountTotal = 0;
			for (int i = 0; i < _counter; i++)
			{
				vertexCountTotal += feature.Points.Count;
			}

			foreach (var roadSegment in feature.Points)
			{
				_counter = roadSegment.Count;
				if (_counter <= 1)
					continue;

				var vl = new List<Vector3>(_sliceCount * _counter);
				var edges = new List<Vector3>(_counter);

				for (int j = 0; j < _counter; j++)
				{
					var prev = Constants.Math.Vector3Zero;
					var current = Constants.Math.Vector3Zero;
					var next = Constants.Math.Vector3Zero;

					current = roadSegment[j];
					Vector3 dirCurrent, dir1, dir2, norm1, norm2, normCurrent;
					float miter = 0f;
					if (j > 0 && j < (_counter - 1))
					{
						dir1 = (roadSegment[j] - roadSegment[j - 1]).normalized;
						norm1 = new Vector3(-dir1.z, 0, dir1.x);
						dir2 = (roadSegment[j + 1] - roadSegment[j]).normalized;
						norm2 = new Vector3(-dir2.z, 0, dir2.x);
						dirCurrent =  (dir2 + dir1).normalized;
						normCurrent = new Vector3(-dirCurrent.z, 0, dirCurrent.x);

						var cosHalfAngle = normCurrent.x * norm2.x + normCurrent.y * norm2.y;
						miter = (cosHalfAngle != 0 ? 1 / cosHalfAngle : 0) * tile.TileScale;
					}
					else if (j == 0) //first
					{
						dirCurrent = (roadSegment[j + 1] - roadSegment[j]).normalized;
						normCurrent = new Vector3(-dirCurrent.z, 0, dirCurrent.x);
					}
					else //last
					{
						dirCurrent = (roadSegment[j] - roadSegment[j - 1]).normalized;
						normCurrent = new Vector3(-dirCurrent.z, 0, dirCurrent.x);
					}
					var q = Quaternion.LookRotation(dirCurrent);
					


					foreach (var item in _slice)
					{
						var p = q * item;
						vl.Add(p + current);
						//var mo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						//mo.transform.position = tile.transform.position + p + current;
					}
				}

				if (md.Triangles.Count == 0)
				{
					md.Triangles.Add(new List<int>());
				}
				md.Vertices.Capacity = md.Vertices.Count + (vl.Count - _sliceCount) * 4;
				md.Normals.Capacity = md.Normals.Count + (vl.Count - _sliceCount) * 4;
				md.Triangles.Capacity = md.Triangles.Count + (vl.Count - _sliceCount) * 6;

				for (int i = 0; i < vl.Count - _sliceCount; i++) //creating a quad for each point except last column and last in each row
				{
					if ((i + 1) % _sliceCount == 0) //no quad for last vertex in the end of the line
					{
						continue;
					}

					var co = md.Vertices.Count;
					var norm = Vector3.Cross(vl[i] - vl[i + 1], vl[i + _sliceCount] - vl[i]).normalized;
					md.Vertices.Add(vl[i]);
					md.Vertices.Add(vl[i + 1]);
					md.Vertices.Add(vl[i + _sliceCount]);
					md.Vertices.Add(vl[i + _sliceCount + 1]);

					md.Normals.Add(norm);
					md.Normals.Add(norm);
					md.Normals.Add(norm);
					md.Normals.Add(norm);

					md.Triangles[0].Add(co);
					md.Triangles[0].Add(co + 1);
					md.Triangles[0].Add(co + 2);

					md.Triangles[0].Add(co + 1);
					md.Triangles[0].Add(co + 3);
					md.Triangles[0].Add(co + 2);
				}

				if (_closeEdges && edges.Count > 2)
				{
					if (md.Triangles.Count < 2)
					{
						md.Triangles.Add(new List<int>());
					}

					var flatData = EarcutLibrary.Flatten(new List<List<Vector3>>() { edges });
					var result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);

					md.Triangles[1].AddRange(result.Select(x => md.Vertices.Count + x).ToList());
					for (int i = 0; i < edges.Count; i++)
					{
						md.Vertices.Add(edges[i]);
						md.Normals.Add(Constants.Math.Vector3Up);
					}
				}
			}
		}

		/*
		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (feature.Points.Count < 1)
				return;

			Vector3 v1, v2, n1, n2, pij1, pij2, pjk1, pjk2;
			Vector3 poi, close1, close2;

			var vertexCountTotal = 0;
			for (int i = 0; i < _counter; i++)
			{
				vertexCountTotal += feature.Points.Count;
			}

			foreach (var roadSegment in feature.Points)
			{
				_counter = roadSegment.Count;
				if (_counter <= 1)
					continue;

				var vl = new List<Vector3>(_sliceCount * _counter);
				var edges = new List<Vector3>(_counter);
				
				for (int j = 0; j < _counter; j++)
				{
					var prev = Constants.Math.Vector3Zero;
					var current = Constants.Math.Vector3Zero;
					var next = Constants.Math.Vector3Zero;

					current = roadSegment[j];
					Vector3 dir;
					if (j > 0 && j < (_counter - 1))
					{
						dir = (roadSegment[j + 1] - roadSegment[j]).normalized + (roadSegment[j] - roadSegment[j - 1]).normalized;
					}
					else if (j == 0) //first
					{
						dir = (roadSegment[j + 1] - roadSegment[j]).normalized;
					}
					else //last
					{
						dir = (roadSegment[j] - roadSegment[j - 1]).normalized * -1;
					}
					var q = Quaternion.LookRotation(dir);
									

					foreach (var item in _slice)
					{
						var p = q * item;
						vl.Add(p);
						//var mo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						//mo.transform.position = tile.transform.position + p + current;
					}
				}

				if (md.Triangles.Count == 0)
				{
					md.Triangles.Add(new List<int>());
				}
				md.Vertices.Capacity = md.Vertices.Count + (vl.Count - _sliceCount) * 4;
				md.Normals.Capacity = md.Normals.Count + (vl.Count - _sliceCount) * 4;
				md.Triangles.Capacity = md.Triangles.Count + (vl.Count - _sliceCount) * 6;

				for (int i = 0; i < vl.Count - _sliceCount; i++) //creating a quad for each point except last column and last in each row
				{
					if ((i + 1) % _sliceCount == 0) //no quad for last vertex in the end of the line
					{
						continue;
					}

					var co = md.Vertices.Count;
					var norm = Vector3.Cross(vl[i] - vl[i + 1], vl[i + _sliceCount] - vl[i]).normalized;
					md.Vertices.Add(vl[i]);
					md.Vertices.Add(vl[i + 1]);
					md.Vertices.Add(vl[i + _sliceCount]);
					md.Vertices.Add(vl[i + _sliceCount + 1]);

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

				if (_closeEdges && edges.Count > 2)
				{
					if (md.Triangles.Count < 2)
					{
						md.Triangles.Add(new List<int>());
					}

					var flatData = EarcutLibrary.Flatten(new List<List<Vector3>>() { edges });
					var result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);

					md.Triangles[1].AddRange(result.Select(x => md.Vertices.Count + x).ToList());
					for (int i = 0; i < edges.Count; i++)
					{
						md.Vertices.Add(edges[i]);
						md.Normals.Add(Constants.Math.Vector3Up);
					}
				}
			}
		}
		*/
	}
}
