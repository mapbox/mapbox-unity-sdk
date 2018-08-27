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
    /// Encapsulates an ML raycast against the physical world from the controller position and orientation.
    /// </summary>
    public class WorldRaycastController : BaseRaycast
    {
        #region Public Variables
        [Tooltip("Reference to the controller object's transform.")]
        public Transform Controller;
        #endregion

        #region Protected Properties
        /// <summary>
        /// Returns the position of the controller.
        /// </summary>
        override protected Vector3 Position
        {
            get
            {
                return Controller.position;
            }
        }

        /// <summary>
        /// Returns the direction of the controller.
        /// </summary>
        override protected Vector3 Direction
        {
            get
            {
                return Controller.forward;
            }
        }

        /// <summary>
        /// returns the current up vector of the controller.
        /// </summary>
        override protected Vector3 Up
        {
            get
            {
                return Controller.up;
            }
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initialize variables.
        /// </summary>
        void Awake()
        {
            if (Controller == null)
            {
                Debug.LogError("Error WorldRaycastController.Controller is not set, disabling script.");
                enabled = false;
                return;
            }
        }
        #endregion
    }
}
