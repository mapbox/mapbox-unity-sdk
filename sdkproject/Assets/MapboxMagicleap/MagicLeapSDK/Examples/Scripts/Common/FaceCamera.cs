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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// This behavior rotates the transform to always look at the Main camera
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("Rotation Offset in Euler Angles")]
        private Vector3 _rotationOffset;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Update rotation to look at main camera
        /// </summary>
        void Update ()
        {
            transform.LookAt(Camera.main.transform);
            transform.rotation *= Quaternion.Euler(_rotationOffset);
        }
        #endregion
    }
}
