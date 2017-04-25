namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using TriangleNet;
    using TriangleNet.Geometry;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;

    /// <summary>
    /// Polygon modifier creates the polygon (vertex&triangles) using the original vertex list.
    /// Currently uses Triangle.Net for triangulation, which occasionally adds extra vertices to maintain a good triangulation so output vertex list might not be exactly same as the original vertex list.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
    public class PolygonMeshModifier : MeshModifier
    {
        public override ModifierType Type { get { return ModifierType.Preprocess; } }

        public override void Run(VectorFeatureUnity feature, MeshData md)
        {
            if (md.Vertices.Distinct().Count() < 3)
                return;

            var data = new List<int>();
            var _mesh = new TriangleNet.Mesh();
            var inp = new InputGeometry(md.Vertices.Count);
            for (int i = 0; i < md.Vertices.Count; i++)
            {
                var v = md.Vertices[i];
                inp.AddPoint(v.x, v.z);
                inp.AddSegment(i, (i + 1) % md.Vertices.Count);
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

            if (_mesh.Vertices.Count != md.Vertices.Count)
            {
                md.Vertices.Clear();
                using (var sequenceEnum = _mesh.Vertices.GetEnumerator())
                {
                    while (sequenceEnum.MoveNext())
                    {
                        md.Vertices.Add(new Vector3((float)sequenceEnum.Current.x, 0, (float)sequenceEnum.Current.y));
                    }
                }
            }
            md.Triangles.Add(data);
        }
    }
}
