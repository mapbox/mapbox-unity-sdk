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
    /// Controller specifically to modify material that has a _Threshold property
    /// </summary>
    public class MaterialThresholdController : MaterialController
    {
        #region Unity Methods
        /// <summary>
        /// Validate material
        /// </summary>
        void Start()
        {
            if (!_material.HasProperty("_Threshold"))
            {
                Debug.LogError("Error: MaterialThresholdController._material does not have _Threshold (Float), disabling script.");
                enabled = false;
                return;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adjust the amount of foreground displayed
        /// </summary>
        /// <param name="factor">Amount of increment</param>
        public override void OnUpdateValue(float factor)
        {
            float threshold = _material.GetFloat("_Threshold");
            threshold += factor;
            threshold = Mathf.Clamp01(threshold);
            _material.SetFloat("_Threshold", threshold);
        }
        #endregion
    }
}
