using System;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Snap Terrain Modifier")]
    public class SnapTerrainModifier : MeshModifier
    {
        public override ModifierType Type { get { return ModifierType.Preprocess; } }

        public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
        {
            if (md.Vertices.Count > 0)
            {
                for (int i = 0; i < md.Vertices.Count; i++)
                {
                    var h = tile.QueryHeightData((float)((md.Vertices[i].x + tile.Rect.Size.x / 2) / tile.Rect.Size.x), (float)((md.Vertices[i].z + tile.Rect.Size.y / 2) / tile.Rect.Size.y));
                    md.Vertices[i] += new Vector3(0, h, 0);
                }
            }
            else
            {
                foreach (var sub in feature.Points)
                {
                    for (int i = 0; i < sub.Count; i++)
                    {
                        var h = tile.QueryHeightData((float)((sub[i].x + tile.Rect.Size.x / 2) / tile.Rect.Size.x), (float)((sub[i].z + tile.Rect.Size.y / 2) / tile.Rect.Size.y));
                        sub[i] += new Vector3(0, h, 0);
                    }
                }
            }
        }
    }
}
