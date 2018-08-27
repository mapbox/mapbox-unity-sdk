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

namespace MagicLeap
{
    /// <summary>
    /// This class implements the functionality for the object with this component
    /// to follow an input transform.
    /// </summary>
    public class TransformFollower : MonoBehaviour
    {
        #region Public Variables
        [Tooltip("Object to follow when mode is Object")]
        public Transform ObjectToFollow;

        [Tooltip("Following should respect(local) or ignore(world) hirarchy")]
        public bool LocalOrWorldSpace = true;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Updates the transform of the object.
        /// </summary>
        void Update()
        {
            if (ObjectToFollow != null)
            {
                if (LocalOrWorldSpace)
                {
                    transform.localPosition = ObjectToFollow.localPosition;
                    transform.localRotation = ObjectToFollow.localRotation;
                }
                else
                {
                    transform.position = ObjectToFollow.position;
                    transform.rotation = ObjectToFollow.rotation;
                }
            }
        }
        #endregion
    }
}
