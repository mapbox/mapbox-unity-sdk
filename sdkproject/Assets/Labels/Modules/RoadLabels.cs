using System.Collections;
using System.Security.Cryptography.X509Certificates;
using Mapbox.Examples;
using Mapbox.Unity.Utilities;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Unity.MeshGeneration.Components;
    using Mapbox.Unity.MeshGeneration.Interfaces;
    using System.Collections.Generic;
    using Mapbox.Unity.Map;
    using System;

    [CreateAssetMenu(menuName = "Mapbox/Modifiers/Road Label Modifier")]
    public class RoadLabels : GameObjectModifier
    {
        public Action<VectorEntity, UnityTile> LabelAdded = (ve, tile) => { };
        public Action<VectorEntity> LabelRemoved = (ve) => { };

        public override void Initialize()
        {
        }

        public override void Run(VectorEntity ve, UnityTile tile)
        {
            LabelAdded(ve, tile);  
        }

        public override void OnPoolItem(VectorEntity vectorEntity)
        {
            LabelRemoved(vectorEntity);
        }
    }
}