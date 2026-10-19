using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.Samples.LandmarkTour
{
    /// <summary>
    /// One entry in the tour: a place to fly to and the view to arrive in. The wide
    /// "whole of Europe" view is expressed as a destination too, so the overview button
    /// and the landmark buttons share one code path.
    /// </summary>
    [Serializable]
    public class LandmarkTourDestination
    {
        [Tooltip("Text shown on the button for this destination.")]
        public string DisplayName = "Landmark";

        [Tooltip("Destination center as comma separated latitude,longitude. Example: 48.8584,2.2945 for the Eiffel Tower.")]
        public string LatLng = "48.8584,2.2945";

        [Tooltip("Zoom to arrive at. Landmarks only load at 14 and above, so a close-up wants 16-18. The Europe overview wants 4-5.")]
        [Range(0f, 22f)]
        public float Zoom = 17f;

        [Tooltip("Camera tilt on arrival. 90 is straight down, 35-45 gives the oblique look that shows a landmark's height.")]
        [Range(15f, 90f)]
        public float Pitch = 40f;

        [Tooltip("Camera bearing on arrival, in degrees. 0 is north up.")]
        [Range(-180f, 180f)]
        public float Bearing;

        /// <summary>Parsed form of <see cref="LatLng"/>. Parsing is not cached — call it once and keep the result.</summary>
        public LatitudeLongitude GetLatitudeLongitude() => Conversions.StringToLatLon(LatLng);
    }
}
