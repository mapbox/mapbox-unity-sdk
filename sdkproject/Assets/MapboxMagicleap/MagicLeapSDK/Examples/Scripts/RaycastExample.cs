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
    /// This example demonstrates using the magic leap raycast functionality to calculate intersection with the physical space.
    /// It demonstrates casting rays from the users headpose, controller, and eyes position and orientation.
    ///
    /// This example uses several raycast visualizers which represent this intersection with the physical space.
    /// </summary>
    public class RaycastExample : MonoBehaviour
    {
        public enum RaycastMode
        {
            Controller,
            Head,
            Eyes
        }

        #region Private Variables
        [SerializeField, Tooltip("The headpose canvas for example status text.")]
        private Text _statusLabel;

        [SerializeField, Tooltip("The headpose canvas for example instruction text.")]
        private Text _calibrationInstructionsLabel;

        [SerializeField, Tooltip("Raycast from controller.")]
        private WorldRaycastController _raycastController;

        [SerializeField, Tooltip("Raycast from headpose.")]
        private WorldRaycastHead _raycastHead;

        [SerializeField, Tooltip("Raycast from eyegaze.")]
        private WorldRaycastEyes _raycastEyes;

        private RaycastMode _raycastMode = RaycastMode.Controller;
        private int _modeCount = System.Enum.GetNames(typeof(RaycastMode)).Length;

        private float _confidence = 0.0f;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate all required components and sets event handlers.
        /// </summary>
        void Awake()
        {
            MLResult result = MLInput.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error RaycastExample starting MLInput, disabling script.");
                enabled = false;
                return;
            }

            if (_statusLabel == null)
            {
                Debug.LogError("Error RaycastExample._statusLabel is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_raycastController == null)
            {
                Debug.LogError("Error RaycastExample._raycastController is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_raycastHead == null)
            {
                Debug.LogError("Error RaycastExample._raycastHead is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_raycastEyes == null)
            {
                Debug.LogError("Error RaycastExample._raycastEyes is not set, disabling script.");
                enabled = false;
                return;
            }

#if !UNITY_EDITOR // Removing calibration step from ML Remote Host builds.
            _calibrationInstructionsLabel.text += "Home Button Tap:\n * Calibrate controller to static model.\n * Toggle back to calibration step.";
#endif

            MLInput.OnControllerButtonDown += OnButtonDown;
            UpdateRaycastMode();
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        void OnDestroy()
        {
            MLInput.OnControllerButtonDown -= OnButtonDown;
            MLInput.Stop();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates type of raycast and enables correct cursor.
        /// </summary>
        private void UpdateRaycastMode()
        {
            // Default all objects to inactive and then set active to the appropriate ones.
            _raycastController.gameObject.SetActive(false);
            _raycastController.Controller.gameObject.SetActive(false);

            _raycastHead.gameObject.SetActive(false);
            _raycastEyes.gameObject.SetActive(false);

            switch (_raycastMode)
            {
                case RaycastMode.Controller:
                {
                    _raycastController.gameObject.SetActive(true);
                    _raycastController.Controller.gameObject.SetActive(true);
                    break;
                }
                case RaycastMode.Head:
                {
                    _raycastHead.gameObject.SetActive(true);
                    break;
                }
                case RaycastMode.Eyes:
                {
                    _raycastEyes.gameObject.SetActive(true);
                    break;
                }
            }
        }

        /// <summary>
        /// Updates Status Label with latest data.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusLabel.text = string.Format("Raycast Mode: {0}\nRaycast Hit Confidence: {1}", _raycastMode.ToString(), _confidence.ToString());
            if(_raycastMode == RaycastMode.Eyes && MLEyes.IsStarted)
            {
                _statusLabel.text += string.Format("\n\nEye Calibration Status: {0}", MLEyes.CalibrationStatus.ToString());
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the event for button down and cycles the raycast mode.
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnButtonDown(byte controller_id, MLInputControllerButton button)
        {
            if (button == MLInputControllerButton.Bumper)
            {
                _raycastMode = (RaycastMode)((int)(_raycastMode + 1) % _modeCount);
                UpdateRaycastMode();
                UpdateStatusText();
            }
        }

        /// <summary>
        /// Callback handler called when raycast has a result.
        /// Updates the confidence value to the new confidence value.
        /// </summary>
        /// <param name="state"> The state of the raycast result.</param>
        /// <param name="result">The hit results (point, normal, distance).</param>
        /// <param name="confidence">Confidence value of hit. 0 no hit, 1 sure hit.</param>
        public void OnRaycastHit(MLWorldRays.MLWorldRaycastResultState state, RaycastHit result, float confidence)
        {
            _confidence = confidence;
            UpdateStatusText();
        }
        #endregion
    }
}
