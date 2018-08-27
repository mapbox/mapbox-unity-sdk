// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2018 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class handles the functionality of updating the bounding box
    /// for the planes query params through input. This class also updates
    /// the UI text containing the latest useful info on the planes queries.
    /// </summary>
    [RequireComponent(typeof(Planes))]
    public class PlanesExample : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("Flag specifying if plane extents are bounded.")]
        private bool _bounded = false;

        [SerializeField, Space, Tooltip("Wireframe cube to represent bounds.")]
        private GameObject _boundsWireframeCube;

        [Space, SerializeField, Tooltip("Text to display number of planes.")]
        private Text _numberOfPlanesText;

        [SerializeField, Tooltip("Text to display if planes extents are bounded or boundless.")]
        private Text _boundedExtentsText;

        private Planes _planesComponent;

        private static readonly Vector3 _boundedExtentsSize = new Vector3(5.0f, 5.0f, 5.0f);
        // Distance close to sensor's maximum recognition distance.
        private static readonly Vector3 _boundlessExtentsSize = new Vector3(10.0f, 10.0f, 10.0f);

        private Camera _camera;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Check editor set variables for null references.
        /// </summary>
        void Awake()
        {
            if (_numberOfPlanesText == null)
            {
                Debug.LogError("Error PlanesExample._numberOfPlanesText is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_boundedExtentsText == null)
            {
                Debug.LogError("Error PlanesExample._boundedExtentsText is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_boundsWireframeCube == null)
            {
                Debug.LogError("Error PlanesExample._boundsWireframeCube is not set, disabling script.");
                enabled = false;
                return;
            }
            MLResult result = MLInput.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error PlanesExample starting MLInput, disabling script.");
                enabled = false;
                return;
            }

            MLInput.OnControllerButtonDown += OnButtonDown;

            _planesComponent = GetComponent<Planes>();
            _camera = Camera.main;
        }

        /// <summary>
        /// Start bounds based on _bounded state.
        /// </summary>
        void Start()
        {
            UpdateBounds();
        }

        /// <summary>
        /// Update position of the planes component to camera position.
        /// Planes query center is based on this position.
        /// </summary>
        void Update()
        {
            _planesComponent.gameObject.transform.position = _camera.transform.position;
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        void OnDestroy()
        {
            if (MLInput.IsStarted)
            {
                MLInput.OnControllerButtonDown -= OnButtonDown;
                MLInput.Stop();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Update plane query bounds extents based on if the current _bounded status is true(bounded)
        /// or false(boundless).
        /// </summary>
        private void UpdateBounds()
        {
            _planesComponent.transform.localScale = _bounded ? _boundedExtentsSize : _boundlessExtentsSize;
            _boundsWireframeCube.SetActive(_bounded);

            _boundedExtentsText.text = string.Format("Bounded Extents: ({0},{1},{2})",
                _planesComponent.transform.localScale.x,
                _planesComponent.transform.localScale.y,
                _planesComponent.transform.localScale.z);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Callback handler, changes text when new planes are received.
        /// </summary>
        /// <param name="planes"> Array of new planes. </param>
        public void OnPlanesUpdate(MLWorldPlane[] planes)
        {
            _numberOfPlanesText.text = string.Format("Number of Planes: {0}/{1}", planes.Length, _planesComponent.MaxPlaneCount);
        }

        /// <summary>
        /// Handles the event for button down. Changes from bounded to boundless and viceversa
        /// when pressing home button
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void OnButtonDown(byte controller_id, MLInputControllerButton button)
        {
            if (button == MLInputControllerButton.HomeTap)
            {
                _bounded = !_bounded;
                UpdateBounds();
            }
        }
        #endregion
    }
}
