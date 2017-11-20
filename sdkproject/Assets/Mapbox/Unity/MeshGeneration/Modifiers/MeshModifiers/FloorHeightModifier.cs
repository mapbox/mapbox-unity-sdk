namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Linq;
	using System;

	/// <summary>
	/// Height Modifier is responsible for the y axis placement of the feature. It pushes the original vertices upwards by "height" value and creates side walls around that new polygon down to "min_height" value.
	/// It also checkes for "ele" (elevation) value used for contour lines in Mapbox Terrain data. 
	/// Height Modifier also creates a continuous UV mapping for side walls.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Floor Height Modifier")]
	public class FloorHeightModifier : MeshModifier
	{
		[SerializeField]
		private int _maxEdgeSectionCount = 40;
		[SerializeField]
		private int _preferredEdgeSectionLength = 10;
		[SerializeField]
		private bool _centerSegments = true;


		[SerializeField]
		private bool _flatTops;
		[SerializeField]
		private float _height;
		[SerializeField]
		private bool _forceHeight;
		[SerializeField]
		[Range(0, 10)]
		private float _floorHeight = 0;
		private float _scaledFloorHeight = 0;
		[SerializeField]
		[Range(0, 10)]
		private float _firstFloorHeight = 0;
		private float _scaledFirstFloorHeight = 0;

		public BuildingModuleHolder Windows;
		public BuildingModuleHolder FirstFloor;

		public Material[] Material;

		private List<Vector3> edgeList;
		float dist = 0;
		float step = 0;
		float dif = 0;
		Vector3 start = Constants.Math.Vector3Zero;
		Vector3 dir = Constants.Math.Vector3Zero;
		Vector3 fs;
		Vector3 sc;
		float d;
		Vector3 v1;
		Vector3 v2;
		Vector3 norm;
		GameObject go;
		MeshFilter mf;
		MeshRenderer mr;
		List<Vector3> verts;
		List<Vector3> norms;
		List<Vector2> uvs;
		List<List<int>> tris;
		Mesh mesh;

		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		public override void Initialize()
		{
			base.Initialize();
			Windows.Initialize();
			FirstFloor.Initialize();
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			_scaledFloorHeight = tile.TileScale * _floorHeight;
			_scaledFirstFloorHeight = tile.TileScale * _firstFloorHeight;

			if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
				return;
			float hf = _height;
			if (feature.Properties.ContainsKey("height"))
			{
				if (float.TryParse(feature.Properties["height"].ToString(), out hf))
				{
					hf *= tile.TileScale;
				}
			}
			for (int i = 0; i < md.Vertices.Count; i++)
			{
				md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + hf, md.Vertices[i].z);
			}
			
			edgeList = new List<Vector3>();
			dist = 0;
			step = 0;
			dif = 0;
			start = Constants.Math.Vector3Zero;
			dir = Constants.Math.Vector3Zero;
			for (int i = 0; i < md.Edges.Count; i += 2)
			{
				fs = md.Vertices[md.Edges[i]];
				sc = md.Vertices[md.Edges[i + 1]];

				dist = Vector3.Distance(fs, sc);
				step = Math.Min(_maxEdgeSectionCount, dist / (_preferredEdgeSectionLength * tile.TileScale));

				start = fs;
				edgeList.Add(start);
				dir = (sc - fs).normalized;
				if (_centerSegments && step > 1)
				{
					dif = dist - ((int)step * (_preferredEdgeSectionLength * tile.TileScale));
					//prevent new point being to close to existing corner
					if (dif > 2 * tile.TileScale)
					{
						//first step, original point or another close point if sections are centered
						start = fs + (dir * (dif / 2));
						//to compansate step-1 below, so if there's more than 2m to corner, go one more step
						step++;
					}
					//edgeList.Add(start);

					if (step > 1)
					{
						for (int s = 1; s < step - 1; s++)
						{
							var da = start + dir * s * (_preferredEdgeSectionLength * tile.TileScale);
							edgeList.Add(da);
							edgeList.Add(da);
						}
					}
				}
				edgeList.Add(sc);
			}

			d = 0f;
			norm = Constants.Math.Vector3Zero;

			go = new GameObject();
			mf = go.AddComponent<MeshFilter>();
			mr = go.AddComponent<MeshRenderer>();

			verts = new List<Vector3>();
			norms = new List<Vector3>();
			uvs = new List<Vector2>();
			tris = new List<List<int>>();
			tris.Add(new List<int>());
			tris.Add(new List<int>());
			tris.Add(new List<int>());
			tris.Add(new List<int>());
			var indf = 0;
			var floor = (_scaledFloorHeight > 0) ? (hf - _scaledFirstFloorHeight) / _scaledFloorHeight : hf;
			
			for (int i = 0; i < edgeList.Count - 1; i += 2)
			{
				v1 = edgeList[i];
				v2 = edgeList[i + 1];
				d = (v2 - v1).magnitude;

				norm = new Vector3(-(v1.z - v2.z), 0, (v1.x - v2.x)).normalized;
				if (norm == Vector3.zero)
					continue;
				var qua = Quaternion.LookRotation(norm);

				//float xmul;
				//float ymul;
				var luck = UnityEngine.Random.value > 0.5;

				if (d > _preferredEdgeSectionLength * tile.TileScale)
				{
					indf = CreateSegment(tile, Windows.SegmentData, indf, floor, qua);
				}
				else
				{
					if (Windows.AlternativeData != null)
					{
						indf = CreateSegment(tile, Windows.AlternativeData, indf, floor, qua);
					}
					else
					{
						indf = CreateFlatWall(indf, floor);
					}
				}

				if (verts.Count >= 60000)
				{
					CreateObject(tile);
					indf = 0;
				}

				if (d > _preferredEdgeSectionLength * tile.TileScale)
				{
					indf = CreateFirstFloor(hf, FirstFloor.SegmentData, indf, qua);
				}
				else
				{
					if (Windows.AlternativeData != null)
					{
						indf = CreateFirstFloor(hf, FirstFloor.AlternativeData, indf, qua);
					}
					else
					{
						indf = CreateFlatWall(indf, floor);
					}
					//xmul = (d / FirstFloor.AlternativeData.Size.x) * 1.01f;
					//ymul = ((hf % _floorHeight) + _firstFloorHeight) / FirstFloor.AlternativeData.Size.y;

					//for (int k = 0; k < FirstFloor.AlternativeData.Vertices.Length; k++)
					//{
					//	var tra = qua * new Vector3(FirstFloor.AlternativeData.Vertices[k].x * xmul, FirstFloor.AlternativeData.Vertices[k].y * ymul, FirstFloor.AlternativeData.Vertices[k].z * zmul);
					//	verts.Add(new Vector3(tra.x + v1.x, tra.y, tra.z + v1.z));
					//}

					//for (int k = 0; k < FirstFloor.AlternativeData.Normals.Length; k++)
					//{
					//	norms.Add(qua * FirstFloor.AlternativeData.Normals[k]);
					//}

					//for (int k = 0; k < FirstFloor.AlternativeData.Uv.Length; k++)
					//{
					//	uvs.Add(FirstFloor.AlternativeData.Uv[k]);
					//}

					//for (int k = 0; k < FirstFloor.AlternativeData.Triangles.Length; k++)
					//{
					//	for (int l = 0; l < FirstFloor.AlternativeData.Triangles[k].Length; l++)
					//	{
					//		tris[k].Add(indf + FirstFloor.AlternativeData.Triangles[k][l]);
					//	}
					//}

					//indf += FirstFloor.AlternativeData.Vertices.Length;

					//verts.Add(new Vector3(v1.x, v1.y - hf + (hf % _floorHeight) + _firstFloorHeight, v1.z));
					//verts.Add(new Vector3(v2.x, v2.y - hf + (hf % _floorHeight) + _firstFloorHeight, v2.z));
					//verts.Add(new Vector3(v1.x, v1.y - hf, v1.z));
					//verts.Add(new Vector3(v2.x, v2.y - hf, v2.z));
					//norms.Add(norm);
					//norms.Add(norm);
					//norms.Add(norm);
					//norms.Add(norm);

					//uvs.Add(new Vector2(0.2f, 0.3f));
					//uvs.Add(new Vector2(0.2f, 0.3f));
					//uvs.Add(new Vector2(0.2f, 0.3f));
					//uvs.Add(new Vector2(0.2f, 0.3f));

					//tris.Add(indf);
					//tris.Add(indf + 1);
					//tris.Add(indf + 2);

					//tris.Add(indf + 1);
					//tris.Add(indf + 3);
					//tris.Add(indf + 2);

					//indf += 4;
				}
			}

			mesh = new Mesh();
			mesh.SetVertices(verts);
			mesh.SetNormals(norms);
			mesh.SetUVs(0, uvs);
			mesh.subMeshCount = tris.Count;
			for (int a = 0; a < tris.Count; a++)
			{
				mesh.SetTriangles(tris[a], a);
			}
			mf.mesh = mesh;
			mr.materials = Material;
			go.transform.SetParent(tile.transform);
			go.transform.position = tile.transform.position;

			go = new GameObject();
			mf = go.AddComponent<MeshFilter>();
			mr = go.AddComponent<MeshRenderer>();
			verts.Clear();
			norms.Clear();
			uvs.Clear();
			tris.Clear();
			indf = 0;
		}

		private int CreateFirstFloor(float hf, SegmentData segment, int indf, Quaternion qua)
		{
			var xmul = (d / segment.Size.x) * 1.01f;
			var ymul = ((hf % _scaledFloorHeight) + _scaledFirstFloorHeight) / segment.Size.y;
			for (int k = 0; k < segment.Vertices.Length; k++)
			{
				//var tra = qua * Windows.Vertices[k] * 10;
				var tra = qua * new Vector3(segment.Vertices[k].x * xmul, segment.Vertices[k].y * ymul, segment.Vertices[k].z * xmul);
				verts.Add(new Vector3(tra.x + v1.x, tra.y, tra.z + v1.z));
			}

			for (int k = 0; k < segment.Normals.Length; k++)
			{
				norms.Add(qua * segment.Normals[k]);
			}

			for (int k = 0; k < segment.Uv.Length; k++)
			{
				uvs.Add(segment.Uv[k]);
			}

			for (int k = 0; k < segment.Triangles.Length; k++)
			{
				if (segment.Triangles[k] == null)
					continue;
				for (int l = 0; l < segment.Triangles[k].Length; l++)
				{
					tris[k].Add(indf + segment.Triangles[k][l]);
				}
			}

			indf += segment.Vertices.Length;
			return indf;
		}

		private int CreateFlatWall(int indf, float floor)
		{
			verts.Add(v1);
			verts.Add(v2);
			uvs.Add(new Vector2(0.2f, 0.3f));
			uvs.Add(new Vector2(0.2f, 0.3f));
			norms.Add(norm);
			norms.Add(norm);
			indf += 2;

			for (int f = 1; f <= floor; f++)
			{
				verts.Add(verts[verts.Count - 2]);
				verts.Add(verts[verts.Count - 2]);
				verts.Add(new Vector3(v1.x, v1.y - (f * _scaledFloorHeight), v1.z));
				verts.Add(new Vector3(v2.x, v2.y - (f * _scaledFloorHeight), v2.z));

				norms.Add(norm);
				norms.Add(norm);
				norms.Add(norm);
				norms.Add(norm);

				uvs.Add(new Vector2(0.2f, 0.3f));
				uvs.Add(new Vector2(0.2f, 0.3f));
				uvs.Add(new Vector2(0.2f, 0.3f));
				uvs.Add(new Vector2(0.2f, 0.3f));

				tris[0].Add(indf);
				tris[0].Add(indf + 1);
				tris[0].Add(indf + 2);

				tris[0].Add(indf + 1);
				tris[0].Add(indf + 3);
				tris[0].Add(indf + 2);

				indf += 4;
			}

			return indf;
		}

		private int CreateSegment(UnityTile tile, SegmentData segment, int indf, float floor, Quaternion qua)
		{
			float xmul = (d / segment.Size.x) * 1.01f;
			float ymul = _scaledFloorHeight / segment.Size.y;
			for (int f = 1; f <= floor; f++)
			{
				if (verts.Count >= 60000)
				{
					CreateObject(tile);
					indf = 0;
				}

				for (int k = 0; k < segment.Vertices.Length; k++)
				{
					//var tra = qua * Windows.Vertices[k] * 10;
					var tra = qua * new Vector3(segment.Vertices[k].x * xmul, segment.Vertices[k].y * ymul, segment.Vertices[k].z * xmul);
					verts.Add(new Vector3(tra.x + v1.x, tra.y + (v1.y - (f * _scaledFloorHeight)), tra.z + v1.z));
				}

				for (int k = 0; k < segment.Normals.Length; k++)
				{
					norms.Add(qua * segment.Normals[k]);
				}

				for (int k = 0; k < segment.Uv.Length; k++)
				{
					uvs.Add(segment.Uv[k]);
				}

				for (int k = 0; k < segment.Triangles.Length; k++)
				{
					if (segment.Triangles[k] == null)
						continue;
					for (int l = 0; l < segment.Triangles[k].Length; l++)
					{
						tris[k].Add(indf + segment.Triangles[k][l]);
					}
				}

				indf += segment.Vertices.Length;
			}

			return indf;
		}

		private void CreateObject(UnityTile tile)
		{
			mesh = new Mesh();
			mesh.SetVertices(verts);
			mesh.SetNormals(norms);
			mesh.SetUVs(0, uvs);
			
			mesh.subMeshCount = tris.Count;
			for (int a = 0; a < tris.Count; a++)
			{
				mesh.SetTriangles(tris[a], a);
			}
			mesh.RecalculateTangents();
			mf.mesh = mesh;
			mr.materials = Material;
			go.transform.SetParent(tile.transform);
			go.transform.position = tile.transform.position;

			go = new GameObject();
			mf = go.AddComponent<MeshFilter>();
			mr = go.AddComponent<MeshRenderer>();
			verts.Clear();
			norms.Clear();
			uvs.Clear();
			tris.Clear();
			tris.Add(new List<int>());
			tris.Add(new List<int>());
			tris.Add(new List<int>());
			tris.Add(new List<int>());
		}
	}
}
