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
using UnityEngine.Serialization;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This represents the controller sprite icon connectivity status.
    /// Red: MLInput error.
    /// Green: Controller connected.
    /// Yellow: Controller disconnected.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(ControllerConnectionHandler))]
    public class ControllerStatusIndicator : MonoBehaviour
    {
        #region Private Variables
        private ControllerConnectionHandler _controllerConnectionHandler;

        [SerializeField, Tooltip("Controller Icon")]
        private Sprite _controllerIcon;

        [SerializeField, Tooltip("Mobile App Icon")]
        private Sprite _mobileAppIcon;

        private SpriteRenderer _spriteRenderer;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

            if (_controllerIcon == null)
            {
                Debug.LogError("Error ControllerStatusIndicator._controllerIcon not set, disabling script.");
                return;
            }
            if (_mobileAppIcon == null)
            {
                Debug.LogError("Error ControllerStatusIndicator._mobileAppIcon not set, disabling script.");
                return;
            }

            _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();
            _controllerConnectionHandler.OnControllerConnected += HandleOnControllerConnected;
            UpdateIcon();
        }

        /// <summary>
        /// Set the color of the sprite renderer based on the controller class connectivity status.
        /// </summary>
        void Update()
        {
            if (_controllerConnectionHandler.enabled)
            {
                if (_controllerConnectionHandler.IsControllerValid())
                {
                    _spriteRenderer.color = Color.green;
                }
                else
                {
                    _spriteRenderer.color = Color.yellow;
                }
            }
            else
            {
                _spriteRenderer.color = Color.red;
            }
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        void OnDestroy()
        {
            _controllerConnectionHandler.OnControllerConnected -= HandleOnControllerConnected;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the event for controller connected.
        /// Assign controller to connected controller if desired hand matches
        /// with new connected controller.
        /// </summary>
        /// <param name="controller">(Unused) New valid controller</param>
        protected void HandleOnControllerConnected(MLInputController controller)
        {
            UpdateIcon();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Update the icon depending on the controller connected
        /// </summary>
        private void UpdateIcon()
        {
            if (_controllerConnectionHandler.IsControllerValid())
            {
                switch (_controllerConnectionHandler.ConnectedController.Type)
                {
                    case MLInputControllerType.Control:
                        _spriteRenderer.sprite = _controllerIcon;
                        break;
                    case MLInputControllerType.MobileApp:
                        _spriteRenderer.sprite = _mobileAppIcon;
                        break;
                }
            }
        }
        #endregion
    }
}
