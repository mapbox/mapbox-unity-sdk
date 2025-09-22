using System;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    [Serializable]
    public class TagModifier : GameObjectModifier
    {
        private string _tag;

        public TagModifier(string tag)
        {
            _tag = tag;
        }

        public override void Run(VectorEntity ve, IMapInformation mapInformation)
        {
            ve.GameObject.tag = _tag;
        }
    }
}