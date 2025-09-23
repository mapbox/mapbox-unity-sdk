using System;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    [Serializable]
    public class LayerModifier : GameObjectModifier
    {
        private int _layer;

        public LayerModifier(int layer)
        {
            _layer = layer;
        }

        public override void Run(VectorEntity ve, IMapInformation mapInformation)
        {
            ve.GameObject.layer = _layer;
        }
    }
}