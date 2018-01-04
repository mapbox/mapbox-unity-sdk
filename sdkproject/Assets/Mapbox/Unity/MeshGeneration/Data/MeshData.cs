namespace Mapbox.Unity.MeshGeneration.Data
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Utils;

	// TODO: Do we need this class? Why not just use `Mesh`?
	public class MeshData
	{
		public Vector3 PositionInTile;
		public List<int> Edges;
		public Vector2 MercatorCenter;
		public RectD TileRect;
		public List<Vector3> Vertices;
		public List<Vector3> Normals;
		public List<Vector4> Tangents;
		public List<List<int>> Triangles;
		public List<List<Vector2>> UV;

		public MeshData()
		{
			Edges = new List<int>();
			Vertices = new List<Vector3>();
			Normals = new List<Vector3>();
			Tangents = new List<Vector4>();
			Triangles = new List<List<int>>();
			UV = new List<List<Vector2>>();
			UV.Add(new List<Vector2>());
		}

		internal void Clear()
		{
			Edges.Clear();
			Vertices.Clear();
			Normals.Clear();
			Tangents.Clear();

			foreach (var item in Triangles)
			{
				item.Clear();
			}
			foreach (var item in UV)
			{
				item.Clear();
			}
		}
	}
}
