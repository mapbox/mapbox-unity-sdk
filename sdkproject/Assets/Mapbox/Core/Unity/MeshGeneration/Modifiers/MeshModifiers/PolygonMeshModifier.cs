namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using TriangleNet;
    using TriangleNet.Geometry;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
    public class PolygonMeshModifier : MeshModifier
    {
        public override ModifierType Type { get { return ModifierType.Preprocess; } }

        public override void Run(VectorFeatureUnity feature, MeshData md)
        {
            if (md.Vertices.Distinct().Count() < 3)
                return;

            var verts = CreateRoofTriangulation(md.Vertices);
            md.Triangles.Add(verts);
        }

        private List<int> CreateRoofTriangulation(List<Vector3> corners)
        {
            var data = new List<int>();
            var _mesh = new TriangleNet.Mesh();
            var inp = new InputGeometry(corners.Count);
            for (int i = 0; i < corners.Count; i++)
            {
                var v = corners[i];
                inp.AddPoint(v.x, v.z);
                inp.AddSegment(i, (i + 1) % corners.Count);
            }
            _mesh.Behavior.Algorithm = TriangulationAlgorithm.SweepLine;
            _mesh.Behavior.Quality = true;
            _mesh.Triangulate(inp);

            foreach (var tri in _mesh.Triangles)
            {
                data.Add(tri.P1);
                data.Add(tri.P0);
                data.Add(tri.P2);
            }
            return data;
        }
    }
}
