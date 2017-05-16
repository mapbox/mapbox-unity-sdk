using System;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Smooth Line Modifier")]
    public class SmoothLineModifier : MeshModifier
    {
        public override ModifierType Type { get { return ModifierType.Preprocess; } }

        public int _maxEdgeSectionCount = 40;
        public int _preferredEdgeSectionLength = 10;

        public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
        {
            for (int i = 0; i < feature.Points.Count; i++)
            {
                var nl = new List<Vector3>();
                for (int j = 1; j < feature.Points[i].Count; j++)
                {
                    nl.Add(feature.Points[i][j - 1]);
                    var dist = Vector3.Distance(feature.Points[i][j - 1], feature.Points[i][j]);
                    var step = Math.Min(_maxEdgeSectionCount, dist / _preferredEdgeSectionLength);
                    if (step > 1)
                    {
                        var counter = 1;
                        while (counter < step)
                        {
                            var nv = Vector3.Lerp(feature.Points[i][j - 1], feature.Points[i][j], Mathf.Min(1, counter / step));
                            nl.Add(nv);
                            counter++;
                        }
                    }
                    nl.Add(feature.Points[i][j]);
                }
                feature.Points[i] = nl;
            }
        }
    }
}
