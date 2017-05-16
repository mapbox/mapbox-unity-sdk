namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;

    public class ModifierBase : ScriptableObject
    {
        [SerializeField]
        public bool Active = true;
    }
}
