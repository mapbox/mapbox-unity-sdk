namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    
    /// <summary>
    /// Line Mesh Modifier creates line polygons from a list of vertices. It offsets the original vertices to both sides using Width parameter and triangulates them manually.
    /// It also creates tiled UV mapping using the line length.
    /// MergeStartEnd parameter connects both edges of the line segment and creates a closed loop which is useful for some cases like pavements around a building block.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Line Mesh Modifier")]
    public class LineMeshModifier : MeshModifier
    {
        [SerializeField]
        private bool _mergeStartEnd;
        [SerializeField]
        private float Width;
        public override ModifierType Type { get { return ModifierType.Preprocess; } }

        public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
        {
            if (feature.Points.Count < 1)
                return;

            foreach (var roadSegment in feature.Points)
            {
                var count = roadSegment.Count;
                for (int i = 1; i < count*2; i++)
                {
                    md.Edges.Add(md.Vertices.Count + i);
                    md.Edges.Add(md.Vertices.Count + i - 1);
                }
                md.Edges.Add(md.Vertices.Count);
                md.Edges.Add(md.Vertices.Count + (count*2) - 1);

                var newVerticeList = new Vector3[count * 2];
                var uvList = new Vector2[count * 2];
                Vector3 norm;
                var lastUv = 0f;
                var p1 = Mapbox.Unity.Constants.Math.Vector3Zero;
                var p2 = Mapbox.Unity.Constants.Math.Vector3Zero;
                var p3 = Mapbox.Unity.Constants.Math.Vector3Zero;
                for (int i = 1; i < count; i++)
                {
                    p1 = roadSegment[i - 1];
                    p2 = roadSegment[i];
                    p3 = p2;
                    if (i + 1 < roadSegment.Count)
                        p3 = roadSegment[i + 1];

                    if (i == 1)
                    {
                        norm = GetNormal(p1, p1, p2) * Width; //road width
                        newVerticeList[0] = (p1 + norm);
                        newVerticeList[count * 2 - 1] = (p1 - norm);
                        uvList[0] = new Vector2(0, 0);
                        uvList[count * 2 - 1] = new Vector2(1, 0);
                    }
                    var dist = Vector3.Distance(p1, p2);
                    lastUv += dist;
                    norm = GetNormal(p1, p2, p3) * Width;
                    newVerticeList[i] = (p2 + norm);
                    newVerticeList[2 * count - 1 - i] = (p2 - norm);
                    
                    uvList[i] = new Vector2(0, lastUv);
                    uvList[2 * count - 1 - i] = new Vector2(1, lastUv);
                }

                //if (_mergeStartEnd)
                //{
                //    //brnkhy -2 because first and last items are same
                //    p1 = segment[count - 2];
                //    p2 = segment[0];
                //    p3 = segment[1];

                //    norm = GetNormal(p1, p2, p3) * Width;
                //    newVerticeList[count - 1] = p2 + norm;
                //    newVerticeList[0] = p2 + norm;
                //    newVerticeList[count] = p2 - norm;
                //    newVerticeList[2 * count - 1] = p2 - norm;
                //}

                var pcount = md.Vertices.Count;
                md.Vertices.AddRange(newVerticeList);
                md.UV[0].AddRange(uvList);
                var lineTri = new List<int>();
                var n = count;

                for (int i = 0; i < n - 1; i++)
                {
                    lineTri.Add(pcount + i);
                    lineTri.Add(pcount + i + 1);
                    lineTri.Add(pcount + 2 * n - 1 - i);
                                
                    lineTri.Add(pcount + i + 1);
                    lineTri.Add(pcount + 2 * n - i - 2);
                    lineTri.Add(pcount + 2 * n - i - 1);
                }

                if (md.Triangles.Count < 1)
                    md.Triangles.Add(new List<int>());
                md.Triangles[0].AddRange(lineTri);
            }            
        }

        private Vector3 GetNormal(Vector3 p1, Vector3 newPos, Vector3 p2)
        {
            if (newPos == p1 || newPos == p2)
            {
                var n = (p2 - p1).normalized;
                return new Vector3(-n.z, 0, n.x);
            }

            var b = (p2 - newPos).normalized + newPos;
            var a = (p1 - newPos).normalized + newPos;
            var t = (b - a).normalized;

            if (t == Mapbox.Unity.Constants.Math.Vector3Zero)
            {
                var n = (p2 - p1).normalized;
                return new Vector3(-n.z, 0, n.x);
            }

            return new Vector3(-t.z, 0, t.x);
        }
    }
}
