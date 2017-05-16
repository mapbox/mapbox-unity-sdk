namespace Mapbox.Unity.MeshGeneration.Data
{
    using System.Collections.Generic;
    using UnityEngine;
    using Utils;

    public class MeshData
    {
        public Vector2 MercatorCenter { get; set; }
        public RectD TileRect { get; set; }
        public List<Vector3> Vertices { get; set; }
        public List<Vector3> Normals { get; set; }
        public List<List<int>> Triangles { get; set; }
        public List<List<Vector2>> UV { get; set; }

        public MeshData()
        {
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Triangles = new List<List<int>>();
            UV = new List<List<Vector2>>();
            UV.Add(new List<Vector2>());
        }
    }
}
