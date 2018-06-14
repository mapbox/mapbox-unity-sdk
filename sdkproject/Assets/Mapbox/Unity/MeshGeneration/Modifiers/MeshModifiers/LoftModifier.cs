namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;
	
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
				int co = 0;

				for (int j = 0; j < _counter; j++)
				{
					var current = Constants.Math.Vector3Zero;

					current = roadSegment[j];
					Vector3 dirCurrent, dir1, dir2;
					if (j > 0 && j < (_counter - 1))
					{
						dir1 = (roadSegment[j] - roadSegment[j - 1]).normalized;
						dir2 = (roadSegment[j + 1] - roadSegment[j]).normalized;
						dirCurrent = (dir2 + dir1).normalized;
					}
					else if (j == 0) //first
					{
						dirCurrent = (roadSegment[j + 1] - roadSegment[j]).normalized;
					}
					else //last
					{
						dirCurrent = (roadSegment[j] - roadSegment[j - 1]).normalized;
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
				
				var uvDist = 0f;
				float edMag = 0f, h = 0f;
				co = 0;
				Vector3 norm;

				for (int i = 0; i < _counter - 1; i++)
				{
					for (int j = 0; j < _sliceCount - 1; j++)
					{
						var ind = i * _sliceCount + j;
						var ed = vl[ind + _sliceCount] - vl[ind];
						edMag = ed.magnitude;
						co = md.Vertices.Count;
						norm = Vector3.Cross(vl[ind] - vl[ind + 1], vl[ind + _sliceCount] - vl[ind]).normalized;
						md.Vertices.Add(vl[ind]);
						md.Vertices.Add(vl[ind + 1]);
						md.Vertices.Add(vl[ind + _sliceCount]);
						md.Vertices.Add(vl[ind + _sliceCount + 1]);

						//h = (vl[ind + 1] - vl[ind]).magnitude;
						h = (float)j / _sliceCount;

						md.UV[0].Add(new Vector2(uvDist, ((float)j - 1) / _sliceCount));
						md.UV[0].Add(new Vector2(uvDist, h));
						md.UV[0].Add(new Vector2(uvDist + edMag, ((float)j - 1) / _sliceCount));
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
					uvDist += edMag;
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
