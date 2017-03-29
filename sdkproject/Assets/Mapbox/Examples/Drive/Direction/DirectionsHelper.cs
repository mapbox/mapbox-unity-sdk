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
        public DirectionsFactory Directions;
        public List<Transform> Waypoints;

        void Start()
        {
            // draw directions path at start
            Query();
        }

        public void Query()
        {
            var waypoints = new List<GeoCoordinate>();
            foreach (var wp in Waypoints)
            {
                waypoints.Add(wp.transform.GetGeoPosition(MapController.ReferenceTileRect.center, MapController.WorldScaleFactor));
            }

            Directions.Query(waypoints);
        }
    }
}