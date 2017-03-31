namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using System.Collections.Generic;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/UV Modifier")]
    public class UvModifier : MeshModifier
    {
        public override ModifierType Type { get { return ModifierType.Preprocess; } }
        public bool UseSatelliteRoof = false;

        public override void Run(VectorFeatureUnity feature, MeshData md)
        {
            var uv = new List<Vector2>();
            foreach (var c in md.Vertices)
            {
                if (UseSatelliteRoof)
                {
                    var fromBottomLeft = new Vector2((float)((c.x + md.TileRect.Size.x / 2) / md.TileRect.Size.x),
                        (float)((c.z + md.TileRect.Size.x / 2) / md.TileRect.Size.x));
                    uv.Add(fromBottomLeft);
                }
                else
                {
                    uv.Add(new Vector2(c.x, c.z));
                }
            }
            md.UV[0].AddRange(uv);
        }
    }
}
