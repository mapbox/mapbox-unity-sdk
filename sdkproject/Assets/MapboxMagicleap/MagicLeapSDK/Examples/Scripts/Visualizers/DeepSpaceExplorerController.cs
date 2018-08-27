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
    /// This class makes it easier to set the radius of the orbit of the Deep Space Explorer
    /// </summary>
    public class DeepSpaceExplorerController : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("Radius of the orbit of the rockets")]
        private Transform _xOffset;
        #endregion

        #region Properties
        public float OrbitRadius
        {
            set
            {
                _xOffset.localPosition = new Vector3(value, 0, 0);
            }
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate input variables
        /// </summary>
        void Start ()
        {
            if (null == _xOffset)
            {
                Debug.LogError("DeepSpaceExplorerController._xOffset not set, disabling script");
                enabled = false;
                return;
            }
        }
        #endregion
    }
}
