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
    using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;

    /// <summary>
    /// Polygon modifier creates the polygon (vertex&triangles) using the original vertex list.
    /// Currently uses Triangle.Net for triangulation, which occasionally adds extra vertices to maintain a good triangulation so output vertex list might not be exactly same as the original vertex list.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Polygon Mesh Modifier")]
    public class PolygonMeshModifier : MeshModifier
    {
        public override ModifierType Type { get { return ModifierType.Preprocess; } }
        private int counter = 0;

        public void OnEnable()
        {
            counter = 0;
        }

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
            List<List<Vector3>> set = new List<List<Vector3>>();
            Data data = null;
            List<int> rest = null;
            var st = 0;
            md.Triangles.Add(new List<int>());
            foreach (var sub in feature.Points)
            {
                if (IsClockwise(sub))
                {
                    if (md.Vertices.Count > 0)
                    {
                        data = EarcutLibrary.Flatten(set);
                        rest = EarcutLibrary.Earcut(data.Vertices, data.Holes, data.Dim);
                        md.Triangles[0].AddRange(rest.Select(x => x + st).ToList());
                        st = md.Vertices.Count;

                        set.Clear();
                        set.Add(sub);
                        var c = md.Vertices.Count;
                        for (int i = 0; i < sub.Count; i++)
                        {
                            md.Edges.Add(c + ((i + 1) % sub.Count));
                            md.Edges.Add(c + i);
                            md.Vertices.Add(sub[i]);
                            md.Normals.Add(Constants.Math.Vector3Up);
                        }
                    }
                    else
                    {
                        set.Add(sub);
                        var c = md.Vertices.Count;
                        for (int i = 0; i < sub.Count; i++)
                        {
                            md.Edges.Add(c + ((i + 1) % sub.Count));
                            md.Edges.Add(c + i);
                            md.Vertices.Add(sub[i]);
                            md.Normals.Add(Constants.Math.Vector3Up);
                        }
                    }
                }
                else
                {
                    set.Add(sub);
                    var c = md.Vertices.Count;
                    for (int i = 0; i < sub.Count; i++)
                    {
                        md.Edges.Add(c + ((i + 1) % sub.Count));
                        md.Edges.Add(c + i);
                        md.Vertices.Add(sub[i]);
                        md.Normals.Add(Constants.Math.Vector3Up);
                    }
                }
            }

            data = EarcutLibrary.Flatten(set);
            rest = EarcutLibrary.Earcut(data.Vertices, data.Holes, data.Dim);
            md.Triangles[0].AddRange(rest.Select(x => x + st).ToList());
        }
    }
}
