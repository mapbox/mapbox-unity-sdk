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
    /// Encapsulates an ML raycast against the physical world from the headpose to the eye fixation point.
    /// </summary>
    public class WorldRaycastEyes : BaseRaycast
    {
        #region Private Variables
        private Camera _camera;
        #endregion

        #region Protected Properties
        /// <summary>
        /// Returns the position of current headpose.
        /// </summary>
        override protected Vector3 Position
        {
            get
            {
                return _camera.transform.position;
            }
        }

        /// <summary>
        /// Returns the direction of headpose to eye fixation point.
        /// </summary>
        override protected Vector3 Direction
        {
            get
            {
                return (MLEyes.FixationPoint - _camera.transform.position).normalized;
            }
        }

        /// <summary>
        /// Returns the up vector of current headpose.
        /// </summary>
        override protected Vector3 Up
        {
            get
            {
                return _camera.transform.up;
            }
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initialize variables.
        /// </summary>
        void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("Error WorldRaycastHead._camera is null, disabling script.");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Initializes component and start MLEyes.
        /// </summary>
        override protected void OnEnable()
        {
            base.OnEnable();

            MLResult result = MLEyes.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error WorldRaycastEyes starting MLEyes, disabling script.");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        override protected void OnDisable()
        {
            MLEyes.Stop();

            base.OnDisable();
        }
        #endregion
    }
}
