using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.Unity
{
    public abstract class ScriptableMeshModifierObject : ScriptableObject, IMeshModifier
    {
        [SerializeField, HideInInspector] private bool m_Active = true;

        /// <summary>
        /// Whether this modifier takes part in its stack. The modifier-stack Inspector draws a
        /// toggle for it; nothing read it at runtime, so unticking a modifier left it running. Read
        /// when the stack is built, so an inactive modifier costs nothing per feature.
        /// </summary>
        public bool Active => m_Active;

        protected abstract MeshModifier _meshModifierImplementation { get; }

        public virtual void ConstructModifier(UnityContext unityContext)
        {
			
        }
		
        public virtual void Initialize()
        {
            _meshModifierImplementation.Initialize();
        }
		
        public void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
        {
            _meshModifierImplementation.Run(feature, md, mapInfo);
        }
    }
}