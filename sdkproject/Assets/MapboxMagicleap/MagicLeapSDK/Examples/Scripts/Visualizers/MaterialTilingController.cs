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
    /// Material Controller to modify tiling
    /// </summary>
    public class MaterialTilingController : MaterialController
    {
        #region Private Variables
        private Vector2 _texTiling;
        private const float MIN_TILING = 0.25f;
        private const float MAX_TILING = 4.0f;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate material, store initial tiling
        /// </summary>
        void Start()
        {
            if (!_material.HasProperty("_MainTex"))
            {
                Debug.LogError("Error: MaterialTilingController._material does not have _MainTex (2D Texture), disabling script.");
                enabled = false;
                return;
            }
            _texTiling = _material.GetTextureScale("_MainTex");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adjust the tiling
        /// </summary>
        /// <param name="factor">Amount of tiling added</param>
        public override void OnUpdateValue(float factor)
        {
            _texTiling += new Vector2(factor, factor);
            _texTiling.x = Mathf.Clamp(_texTiling.x, MIN_TILING, MAX_TILING);
            _texTiling.y = Mathf.Clamp(_texTiling.y, MIN_TILING, MAX_TILING);
            _material.SetTextureScale("_MainTex", _texTiling);
        }
        #endregion
    }
}
