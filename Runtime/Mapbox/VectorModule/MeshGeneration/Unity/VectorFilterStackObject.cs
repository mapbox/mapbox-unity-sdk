using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.Filters;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.Unity
{
    [CreateAssetMenu(menuName = "Mapbox/Filters/Vector Filter Stack")]
    public class VectorFilterStackObject : ScriptableObject
    {
        public List<FilterBaseObject> Filters = new List<FilterBaseObject>(10);
        
        public LayerFilterCombinerOperationType Type;

        public VectorFilterStack GetCombiner()
        {
            return new VectorFilterStack()
            {
                // Inactive filters are dropped here rather than skipped per feature: the Inspector's
                // enable toggle used to be drawn and never read, so unticking a filter left it
                // filtering.
                Filters = Filters.Where(x => x != null && x.Active).Select(x => x.Filter).ToList(),
                Type = this.Type
            };
        }
    }
    
    [Serializable]
    public class VectorFilterStack : ILayerFeatureFilterComparer
    {
        public List<ILayerFeatureFilterComparer> Filters;
        public LayerFilterCombinerOperationType Type;

        public VectorFilterStack()
        {
            
        }

        /// <summary>
        /// Initializes every filter in the stack. This was an empty body, and the only code that
        /// actually initialized filters was <c>ModifierStack.Initialize</c>, reaching in through
        /// this object's <c>Filters</c> list - so a filter stack used without a modifier stack
        /// initialized nothing, and the reach-in dereferenced <c>Filters</c> without a null check.
        /// </summary>
        public void Initialize()
        {
            if (Filters == null) return;

            foreach (var filter in Filters)
            {
                filter?.Initialize();
            }
        }

        public bool Try(VectorFeatureUnity feature)
        {
            if (Filters == null || Filters.Count == 0)
                return true;

            switch (Type)
            {
                case LayerFilterCombinerOperationType.Any:
                    return Filters.Any(m => m.Try(feature));
                case LayerFilterCombinerOperationType.All:
                    return Filters.All(m => m.Try(feature));
                case LayerFilterCombinerOperationType.None:
                    return !Filters.Any(m => m.Try(feature));
                default:
                    return false;
            }
        }
    }
}