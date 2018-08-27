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
    /// Class for tracking a specific Keypose and handling confidence value
    /// based sprite renderer color changes.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class KeyPoseVisualizer : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("KeyPose to track.")]
        private MLHandKeyPose _keyPoseToTrack;

        [Space, SerializeField, Tooltip("Flag to specify if left hand should be tracked.")]
        private bool _trackLeftHand = true;

        [SerializeField, Tooltip("Flag to specify id right hand should be tracked.")]
        private bool _trackRightHand = true;

        private SpriteRenderer _spriteRenderer;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initialize variables.
        /// </summary>
        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Update color of sprite renderer material based on confidence of the KeyPose.
        /// </summary>
        void Update()
        {
            if (!MLHands.IsStarted)
            {
                _spriteRenderer.material.color = Color.red;
                return;
            }

            float confidenceLeft = _trackLeftHand ? GetKeyPoseConfidence(MLHands.Left) : 0.0f;
            float confidenceRight = _trackRightHand ? GetKeyPoseConfidence(MLHands.Right) : 0.0f;
            float confidenceValue = Mathf.Max(confidenceLeft, confidenceRight);

            Color currentColor = Color.white;

            if (confidenceValue > 0.0f)
            {
                currentColor.r = 1.0f - confidenceValue;
                currentColor.g = 1.0f;
                currentColor.b = 1.0f - confidenceValue;
            }

            _spriteRenderer.material.color = currentColor;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Get the confidence value for the hand being tracked.
        /// </summary>
        /// <param name="hand">Hand to check the confidence value on. </param>
        /// <returns></returns>
        private float GetKeyPoseConfidence(MLHand hand)
        {
            if (hand != null)
            {
                if (hand.KeyPose == _keyPoseToTrack)
                {
                    return hand.KeyPoseConfidence;
                }
            }
            return 0.0f;
        }
        #endregion
    }
}
