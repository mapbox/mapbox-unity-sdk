namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using Mapbox.Unity.MeshGeneration.Data;
    
    public enum ModifierType
    {
        Preprocess,
        Postprocess
    }

    public class MeshModifier : ModifierBase
    {
        public virtual ModifierType Type { get { return ModifierType.Preprocess; } }

        public virtual void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
        {

        }
    }
}