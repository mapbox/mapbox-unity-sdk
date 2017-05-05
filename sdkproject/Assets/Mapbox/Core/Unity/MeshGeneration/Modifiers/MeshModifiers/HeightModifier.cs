namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using System.Collections.Generic;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;

    public enum ExtrusionType
    {
        Wall,
        FirstMidFloor,
        FirstMidTopFloor
    }

    /// <summary>
    /// Height Modifier is responsible for the y axis placement of the feature. It pushes the original vertices upwards by "height" value and creates side walls around that new polygon down to "min_height" value.
    /// It also checkes for "ele" (elevation) value used for contour lines in Mapbox Terrain data. 
    /// Height Modifier also creates a continuous UV mapping for side walls.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Height Modifier")]
    public class HeightModifier : MeshModifier
    {
        [SerializeField]
        private bool _flatTops;
        [SerializeField]
        private float _height;
        [SerializeField]
        private bool _forceHeight;

        public override ModifierType Type { get { return ModifierType.Preprocess; } }

        public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
        {
            if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
                return;

            var minHeight = 0f;
            float hf = _height;
            if (!_forceHeight)
            {
                if (feature.Properties.ContainsKey("height"))
                {
                    if (float.TryParse(feature.Properties["height"].ToString(), out hf))
                    {
                        if (feature.Properties.ContainsKey("min_height"))
                        {
                            minHeight = float.Parse(feature.Properties["min_height"].ToString());
                            hf -= minHeight;
                        }
                    }
                }
                if (feature.Properties.ContainsKey("ele"))
                {
                    if (float.TryParse(feature.Properties["ele"].ToString(), out hf))
                    {
                    }
                }
            }

            var max = md.Vertices[0].y;
            var min = md.Vertices[0].y;
            if (_flatTops)
            {
                for (int i = 0; i < md.Vertices.Count; i++)
                {
                    if (md.Vertices[i].y > max)
                        max = md.Vertices[i].y;
                    else if (md.Vertices[i].y < min)
                        min = md.Vertices[i].y;
                }
                for (int i = 0; i < md.Vertices.Count; i++)
                {
                    md.Vertices[i] = new Vector3(md.Vertices[i].x, max + minHeight + hf, md.Vertices[i].z);
                }
                hf += max - min;
            }
            else
            {
                for (int i = 0; i < md.Vertices.Count; i++)
                {
                    md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + minHeight + hf, md.Vertices[i].z);
                }
            }           

            var count = md.Vertices.Count;
            float d = 0f;
            Vector3 v1;
            Vector3 v2;
            int ind = 0;

            var wallTri = new List<int>();
            var wallUv = new List<Vector2>();

            for (int i = 0; i < md.Edges.Count; i+=2)
            {
                v1 = md.Vertices[md.Edges[i]];
                v2 = md.Vertices[md.Edges[i + 1]];
                ind = md.Vertices.Count;
                md.Vertices.Add(v1);
                md.Vertices.Add(v2);
                md.Vertices.Add(new Vector3(v1.x, v1.y - hf, v1.z));
                md.Vertices.Add(new Vector3(v2.x, v2.y - hf, v2.z));

                d = (v2 - v1).magnitude;

                wallUv.Add(new Vector2(0, 0));
                wallUv.Add(new Vector2(d, 0));
                wallUv.Add(new Vector2(0, -hf));
                wallUv.Add(new Vector2(d, -hf));

                wallTri.Add(ind);
                wallTri.Add(ind + 1);
                wallTri.Add(ind + 2);

                wallTri.Add(ind + 1);
                wallTri.Add(ind + 3);
                wallTri.Add(ind + 2);
            }

            md.Triangles.Add(wallTri);
            md.UV[0].AddRange(wallUv);

        }
    }
}
