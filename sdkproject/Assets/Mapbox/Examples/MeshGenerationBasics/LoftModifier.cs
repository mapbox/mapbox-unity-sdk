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
		private float _sliceTotalMagnitude;
		private Vector2[] _sliceUvs;

		public override void Initialize()
		{
			base.Initialize();
			_slice = new List<Vector3>();
			foreach (Transform tr in Slice.transform)
			{
				_slice.Add(tr.position);
			}
			_sliceCount = _slice.Count;

			_sliceTotalMagnitude = 0;
			for (int i = 0; i < _sliceCount - 1; i++)
			{
				_sliceTotalMagnitude += (_slice[i + 1] - _slice[i]).magnitude;
			}
			_sliceUvs = new Vector2[_sliceCount];
			_sliceUvs[0] = new Vector2(0, 0);
			for (int i = 0; i < _sliceCount - 1; i++)
			{
				_sliceUvs[i + 1] = new Vector2(0, _sliceUvs[i].y + (_slice[i + 1] - _slice[i]).magnitude / _sliceTotalMagnitude);
			}
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (feature.Points.Count < 1)
				return;

			foreach (var roadSegment in feature.Points)
			{
				_counter = roadSegment.Count;
				if (_counter <= 1)
					continue;

				var vl = new List<Vector3>(_sliceCount * _counter);
				var edges = new List<Vector3>(_counter);
				var uvs = new List<Vector2>(_sliceCount * _counter);
				int co = 0;

				for (int j = 0; j < _counter; j++)
				{
					var prev = Constants.Math.Vector3Zero;
					var current = Constants.Math.Vector3Zero;
					var next = Constants.Math.Vector3Zero;

					current = roadSegment[j];
					Vector3 dirCurrent, dir1, dir2, norm2;
					//float miter = 0f;
					if (j > 0 && j < (_counter - 1))
					{
						dir1 = (roadSegment[j] - roadSegment[j - 1]).normalized;
						//norm1 = new Vector3(-dir1.z, 0, dir1.x);
						dir2 = (roadSegment[j + 1] - roadSegment[j]).normalized;
						norm2 = new Vector3(-dir2.z, 0, dir2.x);
						dirCurrent = (dir2 + dir1).normalized;
						//normCurrent = new Vector3(-dirCurrent.z, 0, dirCurrent.x);
						//var cosHalfAngle = normCurrent.x * norm2.x + normCurrent.y * norm2.y;
						//miter = (cosHalfAngle != 0 ? 1 / cosHalfAngle : 0) * tile.TileScale;
					}
					else if (j == 0) //first
					{
						dirCurrent = (roadSegment[j + 1] - roadSegment[j]).normalized;
						//normCurrent = new Vector3(-dirCurrent.z, 0, dirCurrent.x);
					}
					else //last
					{
						dirCurrent = (roadSegment[j] - roadSegment[j - 1]).normalized;
						//normCurrent = new Vector3(-dirCurrent.z, 0, dirCurrent.x);
					}
					var q = Quaternion.LookRotation(dirCurrent);

					co = _slice.Count;
					for (int i = 0; i < co; i++)
					{
						var p = q * _slice[i];
						vl.Add(p + current);
						if (i == co - 1) //last item capped
						{
							edges.Add(p + current);
						}
					}
				}

				if (md.Triangles.Count == 0)
				{
					md.Triangles.Add(new List<int>());
				}
				md.Vertices.Capacity = md.Vertices.Count + (vl.Count - _sliceCount) * 4;
				md.Normals.Capacity = md.Normals.Count + (vl.Count - _sliceCount) * 4;
				md.Triangles.Capacity = md.Triangles.Count + (vl.Count - _sliceCount) * 6;
				
				var quadCounter = vl.Count - _sliceCount;
				var uvDist = 0f;
				float edMag = 0f, h = 0f;
				co = 0;
				Vector3 norm;
				for (int i = 0; i < quadCounter; i++) //creating a quad for each point except last column and last in each row
				{
					if ((i + 1) % _sliceCount == 0) //no quad for last vertex in the end of the line
					{
						uvDist += edMag;
						continue;
					}

					var ed = vl[i + _sliceCount] - vl[i];
					edMag = ed.magnitude;
					co = md.Vertices.Count;
					norm = Vector3.Cross(vl[i] - vl[i + 1], vl[i + _sliceCount] - vl[i]).normalized;
					md.Vertices.Add(vl[i]);
					md.Vertices.Add(vl[i + 1]);
					md.Vertices.Add(vl[i + _sliceCount]);
					md.Vertices.Add(vl[i + _sliceCount + 1]);

					h = (vl[i + 1] - vl[i]).magnitude;

					md.UV[0].Add(new Vector2(uvDist, 0));
					md.UV[0].Add(new Vector2(uvDist, h));
					md.UV[0].Add(new Vector2(uvDist + edMag, 0));
					md.UV[0].Add(new Vector2(uvDist + edMag, h));

					md.Tangents.Add(new Vector4(ed.normalized.x, ed.normalized.y, ed.normalized.z, 1));
					md.Tangents.Add(new Vector4(ed.normalized.x, ed.normalized.y, ed.normalized.z, 1));
					md.Tangents.Add(new Vector4(ed.normalized.x, ed.normalized.y, ed.normalized.z, 1));
					md.Tangents.Add(new Vector4(ed.normalized.x, ed.normalized.y, ed.normalized.z, 1));

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
						md.UV[0].Add(new Vector2(edges[i].x, edges[i].z));
						md.Tangents.Add(new Vector4(1,0,0,1));
					}
				}
			}
		}
	}
}
