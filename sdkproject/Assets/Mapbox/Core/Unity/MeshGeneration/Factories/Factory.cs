namespace Mapbox.Unity.MeshGeneration.Factories
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Platform;

    /// <summary>
    /// Corresponds to Mapbox Api end points like raster image, vector tile or directions. Makes the web calls and handles the response.
    /// </summary>
    public class Factory : ScriptableObject
    {
        //private IWorldParameter MapVisualization;
        protected IFileSource FileSource;

        public virtual void Initialize(IFileSource fileSource)
        {
            //MapVisualization = vis;
            FileSource = fileSource;
        }

        public virtual void Register(UnityTile tile)
        {

        }

        public virtual void Update()
        {

        }
    }
}
