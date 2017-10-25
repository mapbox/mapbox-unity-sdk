namespace Mapbox.Unity.MeshGeneration.Data
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Utils;

	// TODO: Do we need this class? Why not just use `Mesh`?
	public class MeshData
	{
		public Vector3 PositionInTile { get; set; }
		public List<int> Edges { get; set; }
		public Vector2 MercatorCenter { get; set; }
		public RectD TileRect { get; set; }
		public List<Vector3> Vertices { get; set; }
		public List<Vector3> Normals { get; set; }
		public List<List<int>> Triangles { get; set; }
		public List<List<Vector2>> UV { get; set; }

		public MeshData()
		{
			Edges = new List<int>();
			Vertices = new List<Vector3>();
			Normals = new List<Vector3>();
			Triangles = new List<List<int>>();
			UV = new List<List<Vector2>>();
			UV.Add(new List<Vector2>());
		}

		internal void Clear()
		{
			Edges.Clear();
			Vertices.Clear();
			Normals.Clear();
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
