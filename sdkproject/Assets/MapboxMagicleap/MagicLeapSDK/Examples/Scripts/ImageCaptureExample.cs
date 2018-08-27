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
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;
using System.Collections.Generic;

namespace MagicLeap
{
    [RequireComponent(typeof(PrivilegeRequester))]
    public class ImageCaptureExample : MonoBehaviour
    {
        [System.Serializable]
        private class ImageCaptureEvent : UnityEvent<Texture2D>
        {}

        #region Private Variables
        [SerializeField]
        private ImageCaptureEvent OnImageReceivedEvent;

        private bool _isCameraConnected = false;
        private bool _isCapturing = false;

        private bool _hasStarted = false;

        private PrivilegeRequester _privilegeRequester;
        #endregion

        #region Unity Methods

        // Using Awake so that Privileges is set before PrivilegeRequester Start
        void Awake()
        {
            _privilegeRequester = GetComponent<PrivilegeRequester>();
            if (_privilegeRequester == null)
            {
                Debug.LogError("Missing PrivilegeRequester component");
                enabled = false;
                return;
            }

            // Could have also been set via the editor.
            _privilegeRequester.Privileges = new[] { MLRuntimeRequestPrivilegeId.CameraCapture };

            _privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;
        }

        /// <summary>
        /// Stop the camera, unregister callbacks, and stop input and privileges APIs.
        /// </summary>
        void OnDisable()
        {
            if (MLInput.IsStarted)
            {
                MLInput.OnControllerButtonDown -= OnButtonDown;
                MLInput.Stop();
            }

            if (_isCameraConnected)
            {
                MLCamera.OnRawImageAvailable -= OnCaptureRawImageComplete;
                _isCapturing = false;
                DisableMLCamera();
            }
        }

        /// <summary>
        /// Cannot make the assumption that a reality privilege is still granted after
        /// returning from pause. Return the application to the state where it
        /// requests privileges needed and clear out the list of already granted
        /// privileges. Also, disable the camera and unregister callbacks.
        /// </summary>
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                if (_isCameraConnected)
                {
                    MLCamera.OnRawImageAvailable -= OnCaptureRawImageComplete;
                    _isCapturing = false;
                    DisableMLCamera();
                }

                MLInput.OnControllerButtonDown -= OnButtonDown;

                _hasStarted = false;
            }
        }

        void OnDestroy()
        {
            if (_privilegeRequester != null)
            {
                _privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Connects the MLCamera component and instantiates a new instance
        /// if it was never created.
        /// </summary>
        /// <remarks>
        /// TODO: Handle privilege denied for public call?
        /// </remarks>
        public bool EnableMLCamera()
        {
            MLResult result = MLCamera.Start();
            if (result.IsOk)
            {
                result = MLCamera.Connect();
                _isCameraConnected = result.IsOk;
            }
            return _isCameraConnected;
        }

        /// <summary>
        /// Disconnects the MLCamera if it was ever created or connected.
        /// </summary>
        public void DisableMLCamera()
        {
            MLCamera.Disconnect();
            // Explicitly set to false here as the disconnect was attempted.
            _isCameraConnected = false;
            MLCamera.Stop();
        }

        /// <summary>
        /// Captures a still image using the device's camera and returns
        /// the data path where it is saved.
        /// </summary>
        /// <param name="fileName">The name of the file to be saved to.</param>
        public void TriggerAsyncCapture()
        {
            if (MLCamera.IsStarted && _isCameraConnected)
            {
                MLResult result = MLCamera.CaptureRawImageAsync();
                if (result.IsOk)
                {
                    _isCapturing = true;
                }
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Responds to privilege requester result.
        /// </summary>
        /// <param name="result"/>
        void HandlePrivilegesDone(MLResult result)
        {
            if (!result.IsOk)
            {
                Debug.LogError("Failed to get all requested privileges. MLResult: " + result);
                // TODO: Cleanup?
                enabled = false;
                return;
            }

            Debug.Log("Succeeded in requesting all privileges");
            StartCapture();
        }

        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnButtonDown(byte controllerId, MLInputControllerButton button)
        {
            if (MLInputControllerButton.Bumper == button && !_isCapturing)
            {
                TriggerAsyncCapture();
            }
        }

        /// <summary>
        /// Handles the event of a new image getting captured.
        /// </summary>
        /// <param name="imageData">The raw data of the image.</param>
        private void OnCaptureRawImageComplete(byte[] imageData)
        {
            _isCapturing = false;

            // Initialize to 8x8 texture so there is no discrepency
            // between uninitalized captures and error texture
            Texture2D texture = new Texture2D(8, 8);
            bool status = texture.LoadImage(imageData);

            if (status && (texture.width != 8 && texture.height != 8))
            {
                OnImageReceivedEvent.Invoke(texture);
            }
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Once privileges have been granted, enable the camera and callbacks.
        /// </summary>
        private void StartCapture()
        {
            if (!_hasStarted)
            {
                MLResult result = MLInput.Start();
                if (!result.IsOk)
                {
                    Debug.LogError("Failed to start MLInput on ImageCapture component. Disabling the script.");
                    enabled = false;
                    return;
                }

                if (!EnableMLCamera())
                {
                    Debug.LogError("MLCamera failed to connect. Disabling ImageCapture component.");
                    enabled = false;
                    return;
                }

                MLInput.OnControllerButtonDown += OnButtonDown;
                MLCamera.OnRawImageAvailable += OnCaptureRawImageComplete;

                _hasStarted = true;
            }
        }
        #endregion
    }
}
