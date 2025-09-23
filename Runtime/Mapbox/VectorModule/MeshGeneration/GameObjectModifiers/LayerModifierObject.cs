using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities.Attributes;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/LayerMask Modifier")]
    public class LayerModifierObject : ScriptableGameObjectModifierObject
    {
        [GameObjectLayer]public int layer;
        private LayerModifier _prefabModifierImplementation;
        protected override GameObjectModifier _gameObjectModifierImplementation => _prefabModifierImplementation;

        public override void ConstructModifier(UnityContext unityContext)
        {
            _prefabModifierImplementation = new LayerModifier(layer);
        }
    }
}