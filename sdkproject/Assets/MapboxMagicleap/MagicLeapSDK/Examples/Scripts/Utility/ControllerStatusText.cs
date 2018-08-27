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
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This represents the controller text connectivity status.
    /// Red: MLInput error.
    /// Green: Controller connected.
    /// Yellow: Controller disconnected.
    /// </summary>
    [RequireComponent(typeof(Text), typeof(ControllerConnectionHandler))]
    public class ControllerStatusText : MonoBehaviour
    {
        #region Private Variables
        private ControllerConnectionHandler _controllerConnectionHandler;
        private Text _controllerStatusText;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            _controllerStatusText = gameObject.GetComponent<Text>();
            _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();
        }

        /// <summary>
        /// Updates text to specify the latest status of the controller.
        /// </summary>
        void Update()
        {
            if (_controllerConnectionHandler.enabled)
            {
                if (_controllerConnectionHandler.IsControllerValid())
                {
                    MLInputController controller = _controllerConnectionHandler.ConnectedController;
                    if (controller.Type == MLInputControllerType.Control)
                    {
                        _controllerStatusText.text = "Controller Connected";
                        _controllerStatusText.color = Color.green;
                    }
                    else if (controller.Type == MLInputControllerType.MobileApp)
                    {
                        _controllerStatusText.text = "MCA Connected";
                        _controllerStatusText.color = Color.green;
                    }
                    else
                    {
                        _controllerStatusText.text = "Unknown";
                        _controllerStatusText.color = Color.red;
                    }
                }
                else
                {
                    _controllerStatusText.text = "Disconnected";
                    _controllerStatusText.color = Color.yellow;
                }
            }
            else
            {
                _controllerStatusText.text = "Input Failed to Start";
                _controllerStatusText.color = Color.red;
            }
        }
        #endregion
    }
}
