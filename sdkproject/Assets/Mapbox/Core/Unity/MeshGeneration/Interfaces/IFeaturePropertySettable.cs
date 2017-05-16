namespace Mapbox.Unity.MeshGeneration.Interfaces
{
    using System.Collections.Generic;

    public interface IFeaturePropertySettable
    {
        void Set(Dictionary<string, object> props);
    }
}
