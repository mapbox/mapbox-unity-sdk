namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using UnityEngine;

    public class ModifierBase : ScriptableObject
    {
        [SerializeField]
        public bool Active = true;

		protected WorldProperties _worldProperties;

		internal virtual void PreInitialize(WorldProperties wp)
		{
		}

		internal virtual void Initialize(WorldProperties wp)
		{
			_worldProperties = wp;
		}
	}
}
