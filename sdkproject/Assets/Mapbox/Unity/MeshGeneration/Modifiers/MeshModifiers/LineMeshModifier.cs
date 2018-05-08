namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;

	/// <summary>
	/// Line Mesh Modifier creates line polygons from a list of vertices. It offsets the original vertices to both sides using Width parameter and triangulates them manually.
	/// It also creates tiled UV mapping using the line length.
	/// MergeStartEnd parameter connects both edges of the line segment and creates a closed loop which is useful for some cases like pavements around a building block.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Line Mesh Modifier")]
	public class LineMeshModifier : MeshModifier
	{
		[SerializeField]
		public float Width = 3.0f;
		private float _scaledWidth;
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

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
			_scaledWidth = tile != null ? Width * tile.TileScale : Width;
			ExtureLine(feature, md);
		}

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
						newTangents[0] = p3 - p2;
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

					newTangents[i] = p2 - p1;
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
