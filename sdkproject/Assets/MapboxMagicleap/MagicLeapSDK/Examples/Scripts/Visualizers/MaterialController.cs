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
using UnityEngine.UI;

namespace MagicLeap
{
    /// <summary>
    /// Parent for the specific material controller
    /// </summary>
    public abstract class MaterialController : MonoBehaviour
    {
        #region Protected Variables
        [SerializeField, Tooltip("Material to be manipulated for all instances")]
        protected Material _material;

        [SerializeField, Tooltip("Helper text")]
        protected Text _statusText;
        [SerializeField, Tooltip("Text to show when viewing an object with this controller")]
        private string _textOnView;
        #endregion

        #region Properties
        public Material ReferenceMaterial
        {
            get
            {
                return _material;
            }
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate variables
        /// </summary>
        void Awake()
        {
            if (null == _material)
            {
                Debug.LogError("MaterialController._material not set, disabling script", this);
                enabled = false;
                return;
            }
            if (null == _statusText)
            {
                Debug.LogError("MaterialController._statusText not set, disabling script.", this);
                enabled = false;
                return;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridable method called when the user holds the bumper while radially scrolling and looking at a plane
        /// </summary>
        /// <param name="value">The change in radial scroll angle. Possible multiplied by a constant factor.</param>
        public abstract void OnUpdateValue(float value);

        /// <summary>
        /// Inform user on what they're looking at
        /// </summary>
        public void UpdateTextOnView()
        {
            _statusText.text = _textOnView;
        }
        #endregion
    }
}
