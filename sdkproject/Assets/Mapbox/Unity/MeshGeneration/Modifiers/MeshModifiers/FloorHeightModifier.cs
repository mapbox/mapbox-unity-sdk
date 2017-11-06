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
		[SerializeField]
		[Range(0, 10)]
		private float _firstFloorHeight = 0;

		public BuildingModuleHolder Windows;
		public BuildingModuleHolder FirstFloor;

		public Material[] Material;

		private Material material;
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
		List<int> tris;
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
			material = Material[UnityEngine.Random.Range(0, Material.Length)];
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
			tris = new List<int>();
			var indf = 0;
			var floor = (_floorHeight > 0) ? (hf - _firstFloorHeight) / _floorHeight : hf;
			var zmul = -1f;
			for (int i = 0; i < edgeList.Count - 1; i += 2)
			{
				v1 = edgeList[i];
				v2 = edgeList[i + 1];
				d = (v2 - v1).magnitude;

				norm = new Vector3(-(v1.z - v2.z), 0, (v1.x - v2.x)).normalized;
				if (norm == Vector3.zero)
					continue;
				var qua = Quaternion.LookRotation(norm);

				float xmul;
				float ymul;
				var luck = UnityEngine.Random.value > 0.5;

				if (d > 3 * tile.TileScale)
				{
					xmul = (d / Windows.SegmentData.Size.x) * 1.01f;
					ymul = _floorHeight / Windows.SegmentData.Size.y;
					if (zmul == -1)
					{
						zmul = xmul;
					}
					for (int f = 1; f <= floor; f++)
					{
						if (verts.Count >= 60000)
						{
							mesh = new Mesh();
							mesh.SetVertices(verts);
							mesh.SetNormals(norms);
							mesh.SetUVs(0, uvs);
							mesh.SetTriangles(tris, 0);
							mf.mesh = mesh;
							mr.material = material;
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

						for (int k = 0; k < Windows.SegmentData.Vertices.Length; k++)
						{
							//var tra = qua * Windows.Vertices[k] * 10;
							var tra = qua * new Vector3(Windows.SegmentData.Vertices[k].x * xmul, Windows.SegmentData.Vertices[k].y * ymul, Windows.SegmentData.Vertices[k].z * zmul);
							verts.Add(new Vector3(tra.x + v1.x, tra.y + (v1.y - (f * _floorHeight)), tra.z + v1.z));
						}

						for (int k = 0; k < Windows.SegmentData.Normals.Length; k++)
						{
							norms.Add(qua * Windows.SegmentData.Normals[k]);
						}

						for (int k = 0; k < Windows.SegmentData.Uv.Length; k++)
						{
							uvs.Add(Windows.SegmentData.Uv[k]);
						}

						for (int k = 0; k < Windows.SegmentData.Triangles.Length; k++)
						{
							tris.Add(indf + Windows.SegmentData.Triangles[k]);
						}

						indf += Windows.SegmentData.Vertices.Length;
					}
				}
				else
				{
					xmul = (d / Windows.AlternativeData.Size.x) * 1.01f;
					ymul = _floorHeight / Windows.AlternativeData.Size.y;
					if (zmul == -1)
					{
						zmul = xmul;
					}

					if (Windows.AlternativeData != null)
					{
						for (int f = 1; f <= floor; f++)
						{
							if (verts.Count >= 60000)
							{
								mesh = new Mesh();
								mesh.SetVertices(verts);
								mesh.SetNormals(norms);
								mesh.SetUVs(0, uvs);
								mesh.SetTriangles(tris, 0);
								mf.mesh = mesh;
								mr.material = material;
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

							for (int k = 0; k < Windows.AlternativeData.Vertices.Length; k++)
							{
								var tra = qua * new Vector3(Windows.AlternativeData.Vertices[k].x * xmul, Windows.AlternativeData.Vertices[k].y * ymul, Windows.AlternativeData.Vertices[k].z * zmul);
								verts.Add(new Vector3(tra.x + v1.x, tra.y + (v1.y - (f * _floorHeight)), tra.z + v1.z));
							}

							for (int k = 0; k < Windows.AlternativeData.Normals.Length; k++)
							{
								norms.Add(qua * Windows.AlternativeData.Normals[k]);
							}

							for (int k = 0; k < Windows.AlternativeData.Uv.Length; k++)
							{
								uvs.Add(Windows.AlternativeData.Uv[k]);
							}

							for (int k = 0; k < Windows.AlternativeData.Triangles.Length; k++)
							{
								tris.Add(indf + Windows.AlternativeData.Triangles[k]);
							}

							indf += Windows.AlternativeData.Vertices.Length;
						}
					}
					else
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
							verts.Add(new Vector3(v1.x, v1.y - (f * _floorHeight), v1.z));
							verts.Add(new Vector3(v2.x, v2.y - (f * _floorHeight), v2.z));

							norms.Add(norm);
							norms.Add(norm);
							norms.Add(norm);
							norms.Add(norm);

							uvs.Add(new Vector2(0.2f, 0.3f));
							uvs.Add(new Vector2(0.2f, 0.3f));
							uvs.Add(new Vector2(0.2f, 0.3f));
							uvs.Add(new Vector2(0.2f, 0.3f));

							tris.Add(indf);
							tris.Add(indf + 1);
							tris.Add(indf + 2);

							tris.Add(indf + 1);
							tris.Add(indf + 3);
							tris.Add(indf + 2);

							indf += 4;
						}
					}
				}

				if (verts.Count >= 60000)
				{
					mesh = new Mesh();
					mesh.SetVertices(verts);
					mesh.SetNormals(norms);
					mesh.SetUVs(0, uvs);
					mesh.SetTriangles(tris, 0);
					mf.mesh = mesh;
					mr.material = material;
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

				if (d > 3 * tile.TileScale)
				{
					xmul = (d / FirstFloor.SegmentData.Size.x) * 1.01f;
					ymul = ((hf % _floorHeight) + _firstFloorHeight) / FirstFloor.SegmentData.Size.y;

					for (int k = 0; k < FirstFloor.SegmentData.Vertices.Length; k++)
					{
						//var tra = qua * Windows.Vertices[k] * 10;
						var tra = qua * new Vector3(FirstFloor.SegmentData.Vertices[k].x * xmul, FirstFloor.SegmentData.Vertices[k].y * ymul, FirstFloor.SegmentData.Vertices[k].z * zmul);
						verts.Add(new Vector3(tra.x + v1.x, tra.y, tra.z + v1.z));
					}

					for (int k = 0; k < FirstFloor.SegmentData.Normals.Length; k++)
					{
						norms.Add(qua * FirstFloor.SegmentData.Normals[k]);
					}

					for (int k = 0; k < FirstFloor.SegmentData.Uv.Length; k++)
					{
						uvs.Add(FirstFloor.SegmentData.Uv[k]);
					}

					for (int k = 0; k < FirstFloor.SegmentData.Triangles.Length; k++)
					{
						tris.Add(indf + FirstFloor.SegmentData.Triangles[k]);
					}

					indf += FirstFloor.SegmentData.Vertices.Length;
				}
				else
				{
					xmul = (d / FirstFloor.AlternativeData.Size.x) * 1.01f;
					ymul = ((hf % _floorHeight) + _firstFloorHeight) / FirstFloor.AlternativeData.Size.y;

					for (int k = 0; k < FirstFloor.AlternativeData.Vertices.Length; k++)
					{
						var tra = qua * new Vector3(FirstFloor.AlternativeData.Vertices[k].x * xmul, FirstFloor.AlternativeData.Vertices[k].y * ymul, FirstFloor.AlternativeData.Vertices[k].z * zmul);
						verts.Add(new Vector3(tra.x + v1.x, tra.y, tra.z + v1.z));
					}

					for (int k = 0; k < FirstFloor.AlternativeData.Normals.Length; k++)
					{
						norms.Add(qua * FirstFloor.AlternativeData.Normals[k]);
					}

					for (int k = 0; k < FirstFloor.AlternativeData.Uv.Length; k++)
					{
						uvs.Add(FirstFloor.AlternativeData.Uv[k]);
					}

					for (int k = 0; k < FirstFloor.AlternativeData.Triangles.Length; k++)
					{
						tris.Add(indf + FirstFloor.AlternativeData.Triangles[k]);
					}

					indf += FirstFloor.AlternativeData.Vertices.Length;

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
			mesh.SetTriangles(tris, 0);
			mf.mesh = mesh;
			mr.material = material;
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
	}
}
