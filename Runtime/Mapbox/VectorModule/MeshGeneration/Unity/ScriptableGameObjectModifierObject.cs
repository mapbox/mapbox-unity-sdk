using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.MeshGeneration.GameObjectModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.Unity
{
    public abstract class ScriptableGameObjectModifierObject : ScriptableObject, IGameObjectModifier
    {
        [SerializeField, HideInInspector] private bool m_Active = true;

        /// <summary>
        /// Whether this modifier takes part in its stack. See
        /// <see cref="ScriptableMeshModifierObject.Active"/> - the Inspector drew this toggle and
        /// nothing read it.
        /// </summary>
        public bool Active => m_Active;

        protected abstract GameObjectModifier _gameObjectModifierImplementation { get; }

        public abstract void ConstructModifier(UnityContext unityContext);
		
        public void Run(VectorEntity ve, IMapInformation mapInformation)
        {
            _gameObjectModifierImplementation.Run(ve, mapInformation);
        }

        public void OnPoolItem(VectorEntity vectorEntity)
        {
            _gameObjectModifierImplementation.OnPoolItem(vectorEntity);
        }

        public void Clear()
        {
            _gameObjectModifierImplementation.Clear();
        }

        public void ClearCaches()
        {
            _gameObjectModifierImplementation.ClearCaches();
        }

        public void Unregister(CanonicalTileId tileId)
        {
            _gameObjectModifierImplementation.Unregister(tileId);
        }

        public void Finalize(VectorEntity entity)
        {
            _gameObjectModifierImplementation.Finalize(entity);
        }
    }
}