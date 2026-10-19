using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.MapInput;
using UnityEngine;

namespace Mapbox.Samples.LandmarkTour
{
    /// <summary>
    /// Keeps the camera's clip planes proportional to how far the camera sits from the map.
    ///
    /// A tour scene spans a far wider range of camera distances than a normal map scene.
    /// With the shipped CameraDistanceCurve and a world scale of 1000 the camera is
    /// roughly 5000 units above the map at zoom 4 and well under a unit at zoom 17, so no
    /// fixed near/far pair serves both ends — the landmark samples ship near 0.01 / far 5,
    /// which clips the entire continental view away. Scaling both planes with the distance
    /// keeps a constant depth ratio, so precision is the same at every zoom.
    ///
    /// This is driven off <see cref="IMapInformation.ViewChanged"/> rather than Update for
    /// a specific reason: the tile provider frustum-culls against the camera's far plane,
    /// and ViewChanged fires inside SetInformation, before ChangeView redraws the map. So
    /// the planes are always correct for the tile cover computed from the same view change.
    /// Subscribing at Initialized covers the first cover too, since MapboxMapBehaviour
    /// raises Initialized before it calls LoadMapView.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCameraClipPlaneDriver : MonoBehaviour
    {
        [Tooltip("Map whose view drives the clip planes. Found in the scene if left empty.")]
        public MapBehaviourCore MapBehaviour;

        [Tooltip("Camera behaviour whose distance curve is used to work out how far the camera will be. Found in the scene if left empty; without it the distance falls back to two tile widths.")]
        public Moving3dCameraBehaviour CameraBehaviour;

        [Tooltip("Near plane as a fraction of the camera's distance from the map. With the far ratio this sets the depth range; 0.01 against a far ratio of 8 gives an 800:1 range, which is comfortable for a 24-bit depth buffer.")]
        public float NearPlaneRatio = 0.01f;

        [Tooltip("Far plane as a multiple of the camera's distance from the map. Needs to reach the far edge of a pitched view; 8 covers roughly four tile widths at any zoom.")]
        public float FarPlaneRatio = 8f;

        private Camera _camera;
        private IMapInformation _information;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (MapBehaviour == null) MapBehaviour = FindObjectOfType<MapBehaviourCore>();
            if (CameraBehaviour == null) CameraBehaviour = FindObjectOfType<Moving3dCameraBehaviour>();

            if (MapBehaviour == null)
            {
                Debug.LogError($"{nameof(MapCameraClipPlaneDriver)} found no {nameof(MapBehaviourCore)} in the scene. Assign the Map Behaviour field; clip planes will keep their authored values.", this);
                enabled = false;
                return;
            }

            MapBehaviour.Initialized += OnMapInitialized;
        }

        private void OnDestroy()
        {
            if (MapBehaviour != null) MapBehaviour.Initialized -= OnMapInitialized;
            if (_information != null) _information.ViewChanged -= Apply;
        }

        private void OnMapInitialized(MapboxMap map)
        {
            if (_information != null) _information.ViewChanged -= Apply;

            _information = map.MapInformation;
            _information.ViewChanged += Apply;
            Apply(_information);
        }

        private void Apply(IMapInformation information)
        {
            var distance = GetCameraDistance(information);
            if (distance <= 0f || !float.IsFinite(distance)) return;

            var near = distance * NearPlaneRatio;
            var far = distance * FarPlaneRatio;
            if (near <= 0f || far <= near) return;

            _camera.nearClipPlane = near;
            _camera.farClipPlane = far;
        }

        /// <summary>
        /// Where the camera will be once this view is applied. Read from the curve rather
        /// than from the transform, because the camera has not been moved yet at the point
        /// ViewChanged fires.
        /// </summary>
        private float GetCameraDistance(IMapInformation information)
        {
            var core = CameraBehaviour != null ? CameraBehaviour.Core : null;
            if (core?.CameraCurve != null && information.Scale > 0f)
            {
                return core.DistanceScale * core.CameraCurve.Evaluate(information.Zoom) / information.Scale;
            }

            // Same order of magnitude as the shipped curve, which is the earth's
            // circumference halved per zoom level against a distance scale of 2.
            return 2f * Conversions.TileSizeInUnitySpace(information.AbsoluteZoom, information.Scale);
        }
    }
}
