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

namespace MagicLeap
{
    /// <summary>
    /// This class handles visibility on image tracking, displaying and hiding prefabs
    /// when images are detected or lost.
    /// </summary>
    [RequireComponent(typeof(MLImageTrackerBehavior))]
    public class ImageTrackingVisualizer : MonoBehaviour
    {
        #region Private Variables
        private MLImageTrackerBehavior _trackerBehavior;
        private bool _targetFound = false;

        [SerializeField, Tooltip("Text to update on ImageTracking changes.")]
        private Text _statusLabel;
        // Stores initial text
        private string _prefix;

        [SerializeField, Tooltip("Game Object showing the axis")]
        private GameObject _axis;
        [SerializeField, Tooltip("Game Object showing the tracking cube")]
        private GameObject _trackingCube;
        [SerializeField, Tooltip("Game Object showing the demo")]
        private GameObject _demo;

        private ImageTrackingExample.ViewMode _lastViewMode = ImageTrackingExample.ViewMode.All;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Validate inspector variables
        /// </summary>
        void Awake()
        {
            if (null == _axis)
            {
                Debug.LogError("Error ImageTrackingVisualizer._axis not set, disabling script.");
                enabled = false;
                return;
            }
            if (null == _trackingCube)
            {
                Debug.LogError("Error ImageTrackingVisualizer._trackingCube not set, disabling script.");
                enabled = false;
                return;
            }
            if (null == _demo)
            {
                Debug.LogError("Error ImageTrackingVisualizer._demo is not set, disabling script.");
                enabled = false;
                return;
            }
            if (null == _statusLabel)
            {
                Debug.LogError("Error ImageTrackingVisualizer._statusLabel is not set, disabling script.");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Initializes variables and register callbacks
        /// </summary>
        void Start()
        {
            _prefix = _statusLabel.text;
            _statusLabel.text = _prefix + "Target Lost";

            _trackerBehavior = GetComponent<MLImageTrackerBehavior>();
            _trackerBehavior.OnTargetFound += OnTargetFound;
            _trackerBehavior.OnTargetLost += OnTargetLost;

            RefreshViewMode();
        }

        /// <summary>
        /// Unregister calbacks
        /// </summary>
        void OnDestroy()
        {
            _trackerBehavior.OnTargetFound -= OnTargetFound;
            _trackerBehavior.OnTargetLost -= OnTargetLost;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Callback for when tracked image is found
        /// </summary>
        /// <param name="isReliable"> Contains if image found is reliable </param>
        private void OnTargetFound(bool isReliable)
        {
            _statusLabel.text = String.Format("{0}Target Found ({1})", _prefix, (isReliable ? "Reliable" : "Unreliable"));
            _targetFound = true;
            RefreshViewMode();
        }

        /// <summary>
        /// Callback for when image tracked is lost
        /// </summary>
        private void OnTargetLost()
        {
            _statusLabel.text = String.Format("{0}Target Lost", _prefix);
            _targetFound = false;
            RefreshViewMode();
        }

        /// <summary>
        /// De/Activate objects to be hidden/seen
        /// </summary>
        private void RefreshViewMode()
        {
            switch (_lastViewMode)
            {
                case ImageTrackingExample.ViewMode.All:
                    _axis.SetActive(_targetFound);
                    _trackingCube.SetActive(_targetFound);
                    _demo.SetActive(_targetFound);
                    break;
                case ImageTrackingExample.ViewMode.AxisOnly:
                    _axis.SetActive(_targetFound);
                    _trackingCube.SetActive(false);
                    _demo.SetActive(false);
                    break;
                case ImageTrackingExample.ViewMode.TrackingCubeOnly:
                    _axis.SetActive(false);
                    _trackingCube.SetActive(_targetFound);
                    _demo.SetActive(false);
                    break;
                case ImageTrackingExample.ViewMode.DemoOnly:
                    _axis.SetActive(false);
                    _trackingCube.SetActive(false);
                    _demo.SetActive(_targetFound);
                    break;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update which objects should be visible
        /// </summary>
        /// <param name="viewMode">Contains the mode to view</param>
        public void UpdateViewMode(ImageTrackingExample.ViewMode viewMode)
        {
            _lastViewMode = viewMode;
            RefreshViewMode();
        }
        #endregion
    }
}
