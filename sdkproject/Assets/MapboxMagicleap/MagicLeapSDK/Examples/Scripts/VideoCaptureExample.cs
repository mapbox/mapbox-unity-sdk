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
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;
using System.Collections.Generic;

namespace MagicLeap
{
    /// <summary>
    /// This class handles video recording and loading based on controller
    /// input.
    /// </summary>
    [RequireComponent(typeof(PrivilegeRequester))]
    public class VideoCaptureExample : MonoBehaviour
    {
        [System.Serializable]
        private class VideoCaptureEvent : UnityEvent<string>
        {}

        #region Private Variables
        [SerializeField, Tooltip("The maximum amount of time the camera can be recording for (in seconds.)")]
        private float _maxRecordingTime = 10.0f;

        [Header("Events")]
        [SerializeField, Tooltip("Event called when recording starts")]
        private UnityEvent OnVideoCaptureStarted;

        [SerializeField, Tooltip("Event called when recording stops")]
        private VideoCaptureEvent OnVideoCaptureEnded;

        private const string _validFileFormat = ".mp4";

        private const float _minRecordingTime = 1.0f;

        // Is the camera currently recording
        private bool _isCapturing;

        // The file path to the active capture
        private string _captureFilePath;

        private bool _isCameraConnected = false;

        private float _captureStartTime;

        private PrivilegeRequester _privilegeRequester;

        private bool _hasStarted = false;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate that _maxRecordingTime is not less than minimum possible.
        /// </summary>
        private void OnValidate()
        {
            if (_maxRecordingTime < _minRecordingTime)
            {
                Debug.LogWarning(string.Format("You can not have a MaxRecordingTime less than {0}, setting back to minimum allowed!", _minRecordingTime));
                _maxRecordingTime = _minRecordingTime;
            }
        }

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
            _privilegeRequester.Privileges = new[] { MLRuntimeRequestPrivilegeId.CameraCapture , MLRuntimeRequestPrivilegeId.AudioCaptureMic};

            _privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;
        }

        void Update()
        {
           if (_isCapturing)
           {
                // If the recording has gone longer than the max time
                if (Time.time - _captureStartTime > _maxRecordingTime)
                {
                    EndCapture();
                }
            }
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
                DisableMLCamera();
            }
        }

        /// <summary>
        /// Cannot make the assumption that a privilege is still granted after
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
        /// Also stops any video recording if active.
        /// </summary>
        public void DisableMLCamera()
        {
            if(_isCapturing)
            {
                EndCapture();
            }
            MLCamera.Disconnect();
            _isCameraConnected = false;
            MLCamera.Stop();
        }

        /// <summary>
        /// Start capturing video.
        /// </summary>
        public void StartCapture()
        {
            string fileName = System.DateTime.Now.ToString("MM_dd_yyyy__HH_mm_ss") + _validFileFormat;
            StartCapture(fileName);
        }

        /// <summary>
        /// Start capturing video to input filename.
        /// </summary>
        /// <param name="fileName">File path to write the video to.</param>
        public void StartCapture(string fileName)
        {
            if(!_isCapturing && MLCamera.IsStarted && _isCameraConnected)
            {
                // Check file fileName extensions
                string extension = System.IO.Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(extension) || !extension.Equals(_validFileFormat, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogErrorFormat("Invalid fileName extension '{0}' passed into Capture({1}).\n" +
                        "Videos must be saved in {2} format.", extension, fileName, _validFileFormat);
                    return;
                }

                string pathName = System.IO.Path.Combine(Application.persistentDataPath, fileName);

                MLResult result = MLCamera.StartVideoCapture(pathName);
                if (result.IsOk)
                {
                    _isCapturing = true;
                    _captureStartTime = Time.time;
                    _captureFilePath = pathName;
                    OnVideoCaptureStarted.Invoke();
                }
                else
                {
                    Debug.LogErrorFormat("Failure: Could not start video capture for {0}. Error Code: {1}",
                        fileName, MLCamera.GetErrorCode().ToString());
                }
            }
            else
            {
                Debug.LogErrorFormat("Failure: Could not start video capture for {0} because '{1}' is already recording!",
                    fileName, _captureFilePath);
            }
        }

        /// <summary>
        /// Stop capturing video.
        /// </summary>
        public void EndCapture()
        {
            if(_isCapturing)
            {
                MLResult result = MLCamera.StopVideoCapture();
                if (result.IsOk)
                {
                    _isCapturing = false;
                    _captureStartTime = 0;
                    OnVideoCaptureEnded.Invoke(_captureFilePath);
                    _captureFilePath = null;
                }
                else
                {
                    Debug.LogErrorFormat("Failure: Could not end video capture. Error Code: {0}",
                        MLCamera.GetErrorCode().ToString());
                }
            }
            else
            {
                Debug.LogError("Failure: Could not EndCapture() because the camera is not recording.");
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
            EnableCapture();
        }

        /// <summary>
        /// Handles the event for button down. Starts or stops recording.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnButtonDown(byte controllerId, MLInputControllerButton button)
        {
            if (MLInputControllerButton.Bumper == button)
            {
                if (!_isCapturing)
                {
                    StartCapture();
                }
                else if(_isCapturing && Time.time - _captureStartTime > _minRecordingTime)
                {
                    EndCapture();
                }
            }
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Once privileges have been granted, enable the camera and callbacks.
        /// </summary>
        private void EnableCapture()
        {
            if (!_hasStarted)
            {
                MLResult result = MLInput.Start();
                if (!result.IsOk)
                {
                    Debug.LogError("Failed to start MLInput on VideoCapture component. Disabling the script.");
                    enabled = false;
                    return;
                }

                if (!EnableMLCamera())
                {
                    Debug.LogError("MLCamera failed to connect. Disabling VideoCapture component.");
                    enabled = false;
                    return;
                }

                MLInput.OnControllerButtonDown += OnButtonDown;

                _hasStarted = true;
            }
        }
        #endregion
    }
}
