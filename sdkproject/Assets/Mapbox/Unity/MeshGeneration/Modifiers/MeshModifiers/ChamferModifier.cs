namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using System.Collections.Generic;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    
    /// <summary>
    /// Chamfer modifiers adds an extra vertex and a line segmet at each corner, making corners and line smoother.
    /// Generally used for smoother building meshes and should be used before Polygon Mesh Modifier.
    /// </summary>
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Chamfer Modifier")]
    public class ChamferModifier : MeshModifier
    {
        [SerializeField]
        private float _size;

        public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
        {
            if (md.Vertices.Count == 0)
                return;

            var final = new List<Vector3>();

            for (int i = 0; i < md.Vertices.Count; i++)
            {
                if (i > 0)
                {
                    var dif = (md.Vertices[i - 1] - md.Vertices[i]);
                    if (dif.magnitude > _size * 2)
                    {
                        dif = dif.normalized * _size;
                        final.Add(md.Vertices[i] + dif);
                    }
                    else
                    {
                        final.Add(md.Vertices[i]);
                    }
                }

                if (i < md.Vertices.Count - 1)
                {
                    var dif = (md.Vertices[i + 1] - md.Vertices[i]);
                    if (dif.magnitude > _size * 2)
                    {
                        dif = dif.normalized * _size;
                        final.Add(md.Vertices[i] + dif);
                    }
                    else
                    {
                        final.Add(md.Vertices[i]);
                    }
                }
            }

            md.Vertices = final;
        }
    }
}
