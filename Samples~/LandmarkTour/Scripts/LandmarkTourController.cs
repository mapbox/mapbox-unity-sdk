using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mapbox.Samples.LandmarkTour
{
    /// <summary>
    /// Builds the tour panel and flies the map when a button is pressed.
    ///
    /// One button per entry in <see cref="Destinations"/> plus one for
    /// <see cref="Overview"/> are cloned from a single prefab at Start and stacked down the
    /// panel. Generating them keeps the destination list the one place to edit: adding a
    /// landmark is a list entry, not a scene edit, and the panel cannot drift out of sync
    /// with the destinations it is supposed to represent.
    /// </summary>
    public class LandmarkTourController : MonoBehaviour
    {
        [Tooltip("Flight component that moves the map. Found in the scene if left empty.")]
        public MapFlightController Flight;

        [Tooltip("Parent for the generated buttons. Buttons are stacked from its top edge downwards.")]
        public RectTransform ButtonContainer;

        [Tooltip("Button prefab cloned once per destination. Its child Text is used for the label.")]
        public Button ButtonPrefab;

        [Tooltip("Vertical gap between buttons, in canvas units.")]
        public float ButtonSpacing = 6f;

        [Tooltip("The wide view the scene opens on, and what the overview button returns to.")]
        public LandmarkTourDestination Overview = new LandmarkTourDestination
        {
            DisplayName = "All of Europe",
            LatLng = "48.5,6.0",
            Zoom = 4.5f,
            Pitch = 90f,
            Bearing = 0f
        };

        [Tooltip("Landmarks to offer. Each needs a zoom of 14 or higher, or no landmark mesh will load.")]
        public List<LandmarkTourDestination> Destinations = new List<LandmarkTourDestination>();

        private void Awake()
        {
            if (Flight == null) Flight = FindObjectOfType<MapFlightController>();
        }

        private void Start()
        {
            if (ButtonPrefab == null || ButtonContainer == null)
            {
                Debug.LogError($"{nameof(LandmarkTourController)} needs both Button Prefab and Button Container assigned. No tour buttons were created.", this);
                return;
            }

            var offset = 0f;
            foreach (var destination in Destinations)
            {
                if (destination == null) continue;
                offset += CreateButton(destination, offset);
            }

            // The overview sits below the landmarks, after a gap, since it is the way back
            // rather than another place to visit.
            if (Overview != null) CreateButton(Overview, offset + ButtonSpacing * 2f);
        }

        /// <summary>Flies to a destination. Public so a hand-wired button can call it too.</summary>
        public void GoTo(LandmarkTourDestination destination)
        {
            if (destination == null || Flight == null) return;
            Flight.FlyTo(destination.GetLatitudeLongitude(), destination.Zoom, destination.Pitch, destination.Bearing);
        }

        /// <summary>Creates one button at the given offset from the container's top, and returns the vertical space it consumed.</summary>
        private float CreateButton(LandmarkTourDestination destination, float offsetFromTop)
        {
            var button = Instantiate(ButtonPrefab, ButtonContainer);
            button.name = destination.DisplayName;

            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = destination.DisplayName;

            // Stretched to the container's width and pinned to its top edge, so the panel
            // can be resized in the Inspector without the buttons needing to know.
            var rect = button.GetComponent<RectTransform>();
            var height = rect.sizeDelta.y;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, -offsetFromTop);

            // Captured by value so each listener keeps its own destination.
            var target = destination;
            button.onClick.AddListener(() => GoTo(target));

            return height + ButtonSpacing;
        }
    }
}
