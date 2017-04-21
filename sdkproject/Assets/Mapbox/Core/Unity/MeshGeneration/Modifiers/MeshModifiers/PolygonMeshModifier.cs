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
                            currentVert = new Vertex(sub[i].x, sub[i].z, sub[i].y);
                            nextVert = new Vertex(sub[i + 1].x, sub[i + 1].z, sub[i].y);
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
                                nextVert = new Vertex(sub[i + 1].x, sub[i + 1].z, sub[i + 1].y);
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
                        cont.Add(new Vertex(sub[i].x, sub[i].z, sub[i].y));
                    }
                    polygon.Add(new Contour(cont), true);
                }
            }

            var mesh = polygon.Triangulate();

            foreach (var tri in mesh.Triangles)
            {
                data.Add(tri.GetVertexID(0));
                data.Add(tri.GetVertexID(2));
                data.Add(tri.GetVertexID(1));
            }

            if (mesh.Vertices.Count != md.Vertices.Count)
            {
                md.Vertices.Clear();
                using (var sequenceEnum = mesh.Vertices.GetEnumerator())
                {
                    while (sequenceEnum.MoveNext())
                    {
                        var h = 0f;
                        if (tile != null)
                        {
                            h = tile.QueryHeightData((float)((sequenceEnum.Current.x + tile.Rect.Size.x / 2) / tile.Rect.Size.x), (float)((sequenceEnum.Current.y + tile.Rect.Size.y / 2) / tile.Rect.Size.y));
                        }
                        md.Vertices.Add(new Vector3((float)sequenceEnum.Current.x, h, (float)sequenceEnum.Current.y));
                    }
                }
            }
            md.Triangles.Add(data);


        }
    }
}
