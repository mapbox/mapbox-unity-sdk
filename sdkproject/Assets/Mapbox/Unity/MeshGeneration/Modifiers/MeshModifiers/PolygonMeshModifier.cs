namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;
	using System;

	/// <summary>
	/// Polygon modifier creates the polygon (vertex&triangles) using the original vertex list.
	/// Currently uses Triangle.Net for triangulation, which occasionally adds extra vertices to maintain a good triangulation so output vertex list might not be exactly same as the original vertex list.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
	public class PolygonMeshModifier : MeshGenerationBase
	{
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

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

		private int _counter, _secondCounter;
		private Vector3 _v1, _v2;

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if(Criteria!=null && Criteria.Count > 0)
			{
				foreach(var criterion in Criteria)
				{
					if(criterion.ShouldReplaceFeature(feature))
					{
						return;
					}
				}
			}

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
					md.Edges.Add(vertCount + ((j+ 1) % c2));
					md.Edges.Add(vertCount + j);
					md.Vertices.Add(sub[j]);
					md.Tangents.Add(Constants.Math.Vector3Forward);
					md.Normals.Add(Constants.Math.Vector3Up);
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
	}
}
