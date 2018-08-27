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
    /// Controller specifically to change _ForegroundColor property
    /// of a material from a gradient palette
    /// </summary>
    public class MaterialForegroundColorController : MaterialController
    {
        #region Private Variables
        [SerializeField, Tooltip("Foreground Color Palette")]
        private Gradient _gradient;
        private float _t = 0;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate material
        /// </summary>
        void Start()
        {
            if (!_material.HasProperty("_ForegroundColor"))
            {
                Debug.LogError("Error: MaterialForegroundColorController._material does not have _ForegroundColor (Color), disabling script.");
                enabled = false;
                return;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adjust the foreground color using the gradient
        /// </summary>
        /// <param name="factor">Increment to the index</param>
        public override void OnUpdateValue(float factor)
        {
            _t = Mathf.Repeat(_t + factor, 1.0f);
            Color color = _gradient.Evaluate(_t);
            _material.SetColor("_ForegroundColor", color);
        }
        #endregion
    }
}
