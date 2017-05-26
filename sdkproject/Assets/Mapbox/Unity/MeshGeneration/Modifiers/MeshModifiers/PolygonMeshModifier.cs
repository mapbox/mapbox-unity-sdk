namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using TriangleNet;
    using TriangleNet.Geometry;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    using TriangleNet.Meshing;
    using System;
    using TriangleNet.Smoothing;

    /// <summary>
    /// Polygon modifier creates the polygon (vertex&triangles) using the original vertex list.
    /// Currently uses Triangle.Net for triangulation, which occasionally adds extra vertices to maintain a good triangulation so output vertex list might not be exactly same as the original vertex list.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
    public class PolygonMeshModifier : MeshModifier
    {
        public override ModifierType Type { get { return ModifierType.Preprocess; } }
        private ConstraintOptions options;
        private QualityOptions quality;

        public void OnEnable()
        {
            options = new ConstraintOptions() { ConformingDelaunay = true };
            quality = new QualityOptions() { MinimumAngle = 25.0 };
            quality.MaximumArea = 100;
        }

        public bool IsClockwise(IList<Vector3> vertices)
        {
            double sum = 0.0;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 v1 = vertices[i];
                Vector3 v2 = vertices[(i + 1) % vertices.Count]; // % is the modulo operator
                sum += (v2.x - v1.x) * (v2.z + v1.z);
            }
            return sum > 0.0;
        }

        public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
        {
            if (feature.Points[0].Count() < 3)
                return;

            var data = new List<int>();
            var polygon = new Polygon();
            Vertex firstVert = null;
            Vertex nextVert = null;
            Vertex currentVert = null;

            foreach (var sub in feature.Points)
            {
                if (IsClockwise(sub))
                {
                    nextVert = null;
                    var wist = new List<Vector3>();
                    for (int i = 0; i < sub.Count; i++)
                    {
                        if (nextVert == null)
                        {
                            currentVert = new Vertex(sub[i].x, sub[i].y, sub[i].z);
                            nextVert = new Vertex(sub[i + 1].x, sub[i].y, sub[i + 1].z);
                        }
                        else
                        {
                            currentVert = nextVert;
                            if (i == sub.Count - 1)
                            {
                                nextVert = firstVert;
                            }
                            else
                            {
                                nextVert = new Vertex(sub[i + 1].x, sub[i + 1].y, sub[i + 1].z);
                            }
                        }

                        if (i == 0)
                            firstVert = currentVert;

                        wist.Add(sub[i]);
                        polygon.Add(currentVert);
                        polygon.Add(new Segment(currentVert, nextVert));
                    }
                }
                else
                {
                    var cont = new List<Vertex>();
                    var wist = new List<Vector3>();
                    for (int i = 0; i < sub.Count; i++)
                    {
                        wist.Add(sub[i]);
                        cont.Add(new Vertex(sub[i].x, sub[i].y, sub[i].z));
                    }
                    polygon.Add(new Contour(cont), true);
                }
            }

            var mesh = polygon.Triangulate();
            //smoother mesh with smaller triangles and extra vertices in the middle
            //var mesh = (TriangleNet.Mesh)polygon.Triangulate(options, quality);

            foreach (var tri in mesh.Triangles)
            {
                data.Add(tri.GetVertexID(0));
                data.Add(tri.GetVertexID(2));
                data.Add(tri.GetVertexID(1));
            }

            foreach (var edge in mesh.Edges)
            {
                if (edge.Label == 0)
                    continue;

                md.Edges.Add(edge.P0);
                md.Edges.Add(edge.P1);
            }

            md.Vertices.Clear();
            using (var sequenceEnum = mesh.Vertices.GetEnumerator())
            {
                while (sequenceEnum.MoveNext())
                {
                    md.Vertices.Add(new Vector3((float)sequenceEnum.Current.x, (float)sequenceEnum.Current.z, (float)sequenceEnum.Current.y));
                }
            }
            md.Triangles.Add(data);
        }
    }
}
