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

        void Start()
        {
            // draw directions path at start
            MapController.WorldCreated += (s, e) =>
            {
                _root = e.transform;
                Query();
            };
        }

        public void Query()
        {
            var waypoints = new List<Vector2d>();
            foreach (var wp in Waypoints)
            {
                waypoints.Add(wp.transform.GetGeoPosition(MapController.ReferenceTileRect.Center, _root.localScale.x));
            }

            Directions.Query(waypoints);
        }
    }
}