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
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Updates the transform an color on the Hit Position and Normal from the assigned object.
    /// </summary>
    public class RaycastVisualizer : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("The reference to the class to handle results from.")]
        private BaseRaycast _raycast;

        [SerializeField, Tooltip("The default distance for the cursor when a hit is not detected.")]
        private float _defaultDistance = 9.0f;

        [SerializeField, Tooltip("When enabled the cursor will scale down once a certain minimum distance is hit.")]
        private bool _scaleWhenClose = true;

        // Stores default color
        private Color _color;

        // Stores result of raycast
        private bool _hit = false;

        // Stores Renderer component
        private Renderer _render;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gettor for _hit.
        /// </summary>
        public bool Hit
        {
            get
            {
                return _hit;
            }
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initializes variables and makes sure needed components exist.
        /// </summary>
        void Awake()
        {
            // Check if the Layer is set to Default and disable any child colliders.
            if (gameObject.layer == LayerMask.NameToLayer("Default"))
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();

                // Disable any active colliders.
                foreach (Collider collider in colliders)
                {
                    collider.enabled = false;
                }

                // Warn user if any colliders had to be disabled.
                if (colliders.Length > 0)
                {
                    Debug.LogWarning("Colliders have been disabled on this RaycastVisualizer.\nIf this is undesirable, change this object's layer to something other than Default.");
                }
            }

            if (_raycast == null)
            {
                Debug.LogError("Error RaycastVisualizer._raycast is not set, disabling script.");
                enabled = false;
                return;
            }

            _render = GetComponent<Renderer>();
            if (_render == null)
            {
                Debug.LogError("Error RaycastVisualizer._render is not set, disabling script.");
                enabled = false;
                return;
            }
            _color = _render.material.color;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Callback handler called when raycast has a result.
        /// Updates the transform an color on the Hit Position and Normal from the assigned object.
        /// </summary>
        /// <param name="state"> The state of the raycast result.</param>
        /// <param name="result"> The hit results (point, normal, distance).</param>
        /// <param name="confidence"> Confidence value of hit. 0 no hit, 1 sure hit.</param>
        public void OnRaycastHit(MLWorldRays.MLWorldRaycastResultState state, RaycastHit result, float confidence)
        {
            if (state != MLWorldRays.MLWorldRaycastResultState.RequestFailed && state != MLWorldRays.MLWorldRaycastResultState.NoCollision)
            {
                // Update the cursor position and normal.
                transform.position = result.point;
                transform.LookAt(result.normal + result.point);
                transform.localScale = Vector3.one;

                // Set the color to yellow if the hit is unobserved.
                _render.material.color = (state == MLWorldRays.MLWorldRaycastResultState.HitObserved)? _color : Color.yellow;

                if (_scaleWhenClose)
                {
                    // Check the hit distance.
                    if (result.distance < 1.0f)
                    {
                        // Apply a downward scale to the cursor.
                        transform.localScale = new Vector3(result.distance, result.distance, result.distance);
                    }
                }

                _hit = true;
            }
            else
            {
                // Update the cursor position and normal.
                transform.position = (_raycast.RayOrigin + (_raycast.RayDirection * _defaultDistance));
                transform.LookAt(_raycast.RayOrigin);
                transform.localScale = Vector3.one;

                _render.material.color = Color.red;

                _hit = false;
            }
        }
        #endregion
    }
}
