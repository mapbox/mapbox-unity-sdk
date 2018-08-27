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
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using System.Collections.Generic;

namespace MagicLeap
{
    /// <summary>
    /// This provides an example of interacting with the image tracker visualizers using the controller
    /// </summary>
    [RequireComponent(typeof(PrivilegeRequester))]
    public class ImageTrackingExample : MonoBehaviour
    {
        #region Public Enum
        public enum ViewMode : int
        {
            All = 0,
            AxisOnly,
            TrackingCubeOnly,
            DemoOnly,
        }

        public GameObject[] TrackerBehaviours;
        #endregion

        #region Private Variables
        private ViewMode _viewMode = ViewMode.All;

        [SerializeField, Tooltip("Image Tracking Visualizers to control")]
        private ImageTrackingVisualizer [] _visualizers;

        [SerializeField, Tooltip("The headpose canvas for example status text.")]
        private Text _statusLabel;

        private PrivilegeRequester _privilegeRequester;

        private bool _hasStarted = false;
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
        /// Unregister callbacks and stop input API.
        /// </summary>
        void OnDestroy()
        {
            if (MLInput.IsStarted)
            {
                MLInput.OnControllerButtonDown -= HandleOnButtonDown;
                MLInput.Stop();
            }

            if (_privilegeRequester != null)
            {
                _privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
            }
        }

        /// <summary>
        /// Cannot make the assumption that a privilege is still granted after
        /// returning from pause. Return the application to the state where it
        /// requests privileges needed and clear out the list of already granted
        /// privileges. Also, unregister callbacks.
        /// </summary>
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                MLInput.OnControllerButtonDown -= HandleOnButtonDown;

                updateImageTrackerBehaviours(false);

                _hasStarted = false;
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Enable/Disable the correct objects depending on view options
        /// </summary>
        void UpdateVisualizers()
        {
            foreach (ImageTrackingVisualizer visualizer in _visualizers)
            {
                visualizer.UpdateViewMode(_viewMode);
            }
        }

        /// <summary>
        /// Control when to enable to image trackers based on
        /// if the correct privileges are given.
        /// </summary>
        void updateImageTrackerBehaviours(bool enabled)
        {
            foreach (GameObject obj in TrackerBehaviours)
            {
                obj.SetActive(enabled);
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
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void HandleOnButtonDown(byte controller_id, MLInputControllerButton button)
        {
            if (button == MLInputControllerButton.Bumper)
            {
                _viewMode = (ViewMode)((int)(_viewMode + 1) % Enum.GetNames(typeof(ViewMode)).Length);
                _statusLabel.text = string.Format("View Mode: {0}", _viewMode.ToString());
            }
            UpdateVisualizers();
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

                updateImageTrackerBehaviours(true);

                if (_visualizers.Length < 1)
                {
                    Debug.LogError("Error ImageTrackingExample._visualizers not set, disabling script.");
                    enabled = false;
                    return;
                }
                if (null == _statusLabel)
                {
                    Debug.LogError("Error ImageTrackingExample._statusLabel is not set, disabling script.");
                    enabled = false;
                    return;
                }

                MLInput.OnControllerButtonDown += HandleOnButtonDown;

                _hasStarted = true;
            }
        }
        #endregion
    }
}
