namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;

	/// <summary>
	/// Polygon modifier creates the polygon (vertex&triangles) using the original vertex list.
	/// Currently uses Triangle.Net for triangulation, which occasionally adds extra vertices to maintain a good triangulation so output vertex list might not be exactly same as the original vertex list.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
	public class PolygonMeshModifier : MeshModifier
	{
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		public bool IsClockwise(IList<Vector3> vertices)
		{
			double sum = 0.0;
			for (int i = 0; i < vertices.Count; i++)
			{
				Vector3 v1 = vertices[i];
				Vector3 v2 = vertices[(i + 1) % vertices.Count];
				sum += (v2.x - v1.x) * (v2.z + v1.z);
			}
			return sum > 0.0;
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			var subset = new List<List<Vector3>>();
			Data flatData = null;
			List<int> result = null;
			var currentIndex = 0;

			List<int> triList = null;

			foreach (var sub in feature.Points)
			{
				//earcut is built to handle one polygon with multiple holes
				//point data can contain multiple polygons though, so we're handling them separately here
				if (IsClockwise(sub) && md.Vertices.Count > 0)
				{
					flatData = EarcutLibrary.Flatten(subset);
					result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);

					if (triList == null)
						triList = new List<int>(result.Count);

					for (int i = 0; i < result.Count; i++)
					{
						triList.Add(result[i] + currentIndex);
					}
					currentIndex = md.Vertices.Count;
					subset.Clear();
				}

				subset.Add(sub);
				var c = md.Vertices.Count;
				for (int i = 0; i < sub.Count; i++)
				{
					md.Edges.Add(c + ((i + 1) % sub.Count));
					md.Edges.Add(c + i);
					md.Vertices.Add(sub[i]);
					md.Normals.Add(Constants.Math.Vector3Up);
				}

			}

			flatData = EarcutLibrary.Flatten(subset);
			result = EarcutLibrary.Earcut(flatData.Vertices, flatData.Holes, flatData.Dim);
			if (triList == null)
				triList = new List<int>(result.Count);
			for (int i = 0; i < result.Count; i++)
			{
				triList.Add(result[i] + currentIndex);
			}

			md.Triangles.Add(triList);
		}
	}
}
