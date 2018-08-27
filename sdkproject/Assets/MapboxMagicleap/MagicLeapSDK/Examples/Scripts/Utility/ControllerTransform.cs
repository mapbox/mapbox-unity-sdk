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

using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class provides the functionality for the object's transform assigned
    /// to this script to match the 6dof data from input when using a Control
    /// and the 3dof data when using the Mobile App.
    /// </summary>
    [RequireComponent(typeof(ControllerConnectionHandler))]
    public class ControllerTransform : MonoBehaviour
    {
        #region Private Variables
        private ControllerConnectionHandler _controllerConnectionHandler;

        private Camera _camera;

        // MCA-specific variables
        private bool _isCalibrated = false;
        private Quaternion _calibrationOrientation = Quaternion.identity;
        private const float MCA_FORWARD_DISTANCE_FROM_CAMERA = 0.75f;
        private const float MCA_UP_DISTANCE_FROM_CAMERA = -0.1f;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initialize variables, callbacks and check null references.
        /// </summary>
        void Start()
        {
            _controllerConnectionHandler = GetComponent<ControllerConnectionHandler>();

            if (!_controllerConnectionHandler.enabled)
            {
                Debug.LogError("Error ControllerTransform starting MLInput, disabling script.");
                enabled = false;
                return;
            }

            _camera = Camera.main;

            MLInput.OnControllerButtonUp += HandleOnButtonUp;
        }

        /// <summary>
        /// Update controller input based feedback.
        /// </summary>
        void Update()
        {
            if (_controllerConnectionHandler.IsControllerValid())
            {
                MLInputController controller = _controllerConnectionHandler.ConnectedController;
                if (controller.Type == MLInputControllerType.Control)
                {
                    // For Control, raw input is enough
                    transform.position = controller.Position;
                    transform.rotation = controller.Orientation;
                }
                else if (controller.Type == MLInputControllerType.MobileApp)
                {
                    // For Mobile App, there is no positional data and orientation needs calibration
                    transform.position = _camera.transform.position + _camera.transform.forward * MCA_FORWARD_DISTANCE_FROM_CAMERA + Vector3.up * MCA_UP_DISTANCE_FROM_CAMERA;
                    if (_isCalibrated)
                    {
                        transform.rotation = _calibrationOrientation * controller.Orientation;
                    }
                    else
                    {
                        transform.LookAt(transform.position + Vector3.up, -Camera.main.transform.forward);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (MLInput.IsStarted)
            {
                MLInput.OnControllerButtonUp -= HandleOnButtonUp;
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// For Mobile App, this initiates/ends the recalibration when the home tap event is triggered
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void HandleOnButtonUp(byte controllerId, MLInputControllerButton button)
        {
            MLInputController controller = _controllerConnectionHandler.ConnectedController;
            if (controller != null && controller.Id == controllerId &&
                controller.Type == MLInputControllerType.MobileApp &&
                button == MLInputControllerButton.HomeTap)
            {
                if (!_isCalibrated)
                {
                    _calibrationOrientation = transform.rotation * Quaternion.Inverse(controller.Orientation);
                }
                _isCalibrated = !_isCalibrated;
            }
        }
        #endregion
    }
}
