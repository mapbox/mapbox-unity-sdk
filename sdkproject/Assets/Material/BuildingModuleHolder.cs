using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingModuleHolder : MonoBehaviour
{
	public GameObject Segment;
	public GameObject SegmentAlternative;
	public SegmentData SegmentData;
	public SegmentData AlternativeData;

	public void Initialize()
	{
		SegmentData = new SegmentData();
		

		var m = Segment.GetComponent<MeshFilter>().sharedMesh;
		SegmentData.Vertices = m.vertices;
		SegmentData.Normals = m.normals;
		SegmentData.Triangles = new int[4][];
		for (int i = 0; i < m.subMeshCount; i++)
		{
			
			SegmentData.Triangles[i] = m.GetTriangles(i);
		}
		SegmentData.Uv = m.uv;

		float xmin = float.MaxValue, xmax = float.MinValue, ymin = float.MaxValue, ymax = float.MinValue;
		foreach (var item in SegmentData.Vertices)
		{
			if (item.x < xmin)
				xmin = item.x;
			if (item.x > xmax)
				xmax = item.x;
			if (item.y < ymin)
				ymin = item.y;
			if (item.y > ymax)
				ymax = item.y;
		}
		SegmentData.Size = new Vector2(xmax - xmin, ymax - ymin);

		if (SegmentAlternative != null)
		{
			AlternativeData = new SegmentData();
			m = SegmentAlternative.GetComponent<MeshFilter>().sharedMesh;
			AlternativeData.Vertices = m.vertices;
			AlternativeData.Normals = m.normals;
			AlternativeData.Triangles = new int[4][];
			for (int i = 0; i < m.subMeshCount; i++)
			{
				AlternativeData.Triangles[i] = m.GetTriangles(i);
			}
			AlternativeData.Uv = m.uv;


			xmin = float.MaxValue;
			xmax = float.MinValue;
			ymin = float.MaxValue;
			ymax = float.MinValue;
			foreach (var item in AlternativeData.Vertices)
			{
				if (item.x < xmin)
					xmin = item.x;
				if (item.x > xmax)
					xmax = item.x;
				if (item.y < ymin)
					ymin = item.y;
				if (item.y > ymax)
					ymax = item.y;
			}
			AlternativeData.Size = new Vector2(xmax - xmin, ymax - ymin);
		}
	}
}

public class SegmentData
{
	public Vector3[] Vertices;
	public Vector3[] Normals;
	public Vector2[] Uv;
	public int[][] Triangles;
	public Vector2 Size;
}
