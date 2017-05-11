namespace Mapbox.Examples.Drive
{
    using UnityEngine;
    using System.Collections.Generic;
    using Mapbox;
    using Mapbox.Unity.MeshGeneration.Factories;
    using Mapbox.Unity.MeshGeneration;
    using Mapbox.Utils;
    using Mapbox.Unity.Utilities;

    public class DirectionsHelper : MonoBehaviour
    {
        private Transform _root;
        public MapController MapController;
        public DirectionsFactory Directions;
        public List<Transform> Waypoints;

        void Awake()
        {
            // draw directions path at start
            MapController.WorldCreated += (s, e) =>
            {
                Query();
            };
        }

        public void Query()
        {
            Directions.Query(Waypoints);
        }
    }
}