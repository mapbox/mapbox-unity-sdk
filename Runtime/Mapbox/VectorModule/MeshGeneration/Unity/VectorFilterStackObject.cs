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

        /// <summary>
        /// Runs on every feature of every tile, so index loops rather than LINQ: `Any`/`All` with a
        /// lambda capturing the feature allocated a closure, a delegate and an enumerator per
        /// feature, and filtering is pure overhead on top of the mesh work it gates.
        /// <c>ComponentFilterStack</c> has always done it this way.
        /// </summary>
        public bool Try(VectorFeatureUnity feature)
        {
            var filters = Filters;
            if (filters == null)
                return true;

            var count = filters.Count;
            if (count == 0)
                return true;

            switch (Type)
            {
                case LayerFilterCombinerOperationType.Any:
                    for (var i = 0; i < count; i++)
                    {
                        if (filters[i].Try(feature)) return true;
                    }
                    return false;
                case LayerFilterCombinerOperationType.All:
                    for (var i = 0; i < count; i++)
                    {
                        if (!filters[i].Try(feature)) return false;
                    }
                    return true;
                case LayerFilterCombinerOperationType.None:
                    for (var i = 0; i < count; i++)
                    {
                        if (filters[i].Try(feature)) return false;
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}