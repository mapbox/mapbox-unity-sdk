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

namespace UnityEngine.XR.MagicLeap
{
    /// <summary>
    /// World and Virtual raycast combination from Head
    /// </summary>
    public class ComboRaycastHead : WorldRaycastHead
    {
        #region Private Variables
        [SerializeField, Tooltip("The layer(s) that will be used for hit detection.")]
        private LayerMask _hitLayerMask;

        // Note: Generated mesh may include noise (bumps). This bias is meant to cover
        // the possible deltas between that and the perception stack results.
        private const float _bias = 0.04f;
        #endregion

        #region Event Handlers
        /// <summary>
        /// Callback handler called when raycast call has a result
        /// </summary>
        /// <param name="state"> The state of the raycast result.</param>
        /// <param name="point"> Position of the hit.</param>
        /// <param name="normal"> Normal of the surface hit.</param>
        /// <param name="confidence"> Confidence value on hit.</param>
        protected override void HandleOnReceiveRaycast(MLWorldRays.MLWorldRaycastResultState state, Vector3 point, Vector3 normal, float confidence)
        {
            RaycastHit result = GetWorldRaycastResult(state, point, normal, confidence);

            // If there was a hit on world raycast, change max distance to the hitpoint distance
            float maxDist = (result.distance > 0.0f) ? (result.distance + _bias) : Mathf.Infinity;

            // Virtual Raycast
            Ray ray = new Ray(_raycastParams.Position, _raycastParams.Direction);
            if (Physics.Raycast(ray, out result, maxDist, _hitLayerMask))
            {
                confidence = 1.0f;
            }

            OnRaycastResult.Invoke(state, result, confidence);

            _isReady = true;
        }
        #endregion
    }
}
