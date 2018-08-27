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
    /// Controller to modify the speeds of the offsets of the main and blend texture
    /// </summary>
    public class MaterialBlendOffsetController : MaterialController
    {
        #region Private Variables
        private Vector2 _mainTexOffset;
        private Vector2 _blendTexOffset;

        private const float MAX_SPEED =  1.0f;
        private const float BLENDTEX_SPEED_FACTOR = 0.25f;

        [SerializeField, Range(-MAX_SPEED, MAX_SPEED), Tooltip("Speed of the X Offset of the texture")]
        private float _xOffsetSpeed = 0.1f;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate material, store initial offset
        /// </summary>
        void Start ()
        {
            if (!_material.HasProperty("_MainTex"))
            {
                Debug.LogError("Error: MaterialBlendOffsetController._material does not have _MainTex (2D Texture), disabling script.");
                enabled = false;
                return;
            }
            if (!_material.HasProperty("_BlendTex"))
            {
                Debug.LogError("Error: MaterialBlendOffsetController._material does not have _BlendTex (2D Texture), disabling script.");
                enabled = false;
                return;
            }

            _mainTexOffset = _material.GetTextureOffset("_MainTex");
            _blendTexOffset = _material.GetTextureOffset("_BlendTex");
        }

        /// <summary>
        /// Increment offset over time
        /// </summary>
        void Update ()
        {
            _mainTexOffset.x += _xOffsetSpeed * Time.deltaTime;
            _blendTexOffset.x += _xOffsetSpeed * Time.deltaTime * BLENDTEX_SPEED_FACTOR;
            _material.SetTextureOffset("_MainTex", _mainTexOffset);
            _material.SetTextureOffset("_BlendTex", _blendTexOffset);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adjust the speed
        /// </summary>
        /// <param name="factor">Increment to the speed</param>
        public override void OnUpdateValue(float factor)
        {
            _xOffsetSpeed += factor * 0.5f;
            _xOffsetSpeed = Mathf.Clamp(_xOffsetSpeed, -MAX_SPEED, MAX_SPEED);
        }
        #endregion
    }
}
