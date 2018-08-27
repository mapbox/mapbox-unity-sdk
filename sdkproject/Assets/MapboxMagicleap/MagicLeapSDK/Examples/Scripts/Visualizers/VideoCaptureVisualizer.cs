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

using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class handles visualization of the video and the UI with the status
    /// of the recording.
    /// </summary>
    public class VideoCaptureVisualizer : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("The screen to show the video capture preview")]
        private GameObject _screen;
        private Renderer _screenRenderer;
        private MLMediaPlayer _mediaPlayer;

        [Header("Visuals")]
        [SerializeField, Tooltip("Text to show instructions for capturing video")]
        private UnityEngine.UI.Text _previewText;

        [SerializeField, Tooltip("Object that will show up when recording")]
        private GameObject _recordingIndicator;

        // time delay between video preparation and enabling screen preview
        private const float SCREEN_PREVIEW_DELAY = 0.6f;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Check for all required variables to be initialized.
        /// </summary>
        void Start()
        {
            if(_screen == null)
            {
                Debug.LogError("The VideoCaptureVisualizer component does not have it's _screen reference assigned. Disabling script.");
                enabled = false;
                return;
            }

            if(_previewText == null)
            {
                Debug.LogError("The VideoCaptureVisualizer component does not have it's _previewText reference assigned. Disabling script.");
                enabled = false;
                return;
            }

            if (_recordingIndicator == null)
            {
                Debug.LogError("The VideoCaptureVisualizer component does not have it's _recordingIndicator reference assigned. Disabling script.");
                enabled = false;
                return;
            }

            _mediaPlayer = _screen.AddComponent<MLMediaPlayer>();
            _mediaPlayer.OnVideoPrepared += HandleVideoPrepared;

            _screenRenderer = _screen.GetComponent<Renderer>();
        }

        private void OnDestroy()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.OnVideoPrepared -= HandleVideoPrepared;
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles video capture being started.
        /// </summary>
        public void OnCaptureStarted()
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }

            // Manage canvas visuals
            _recordingIndicator.SetActive(true);
            _previewText.text = "Press the bumper to stop capturing a video.";

            // Disable the preview
            _screenRenderer.enabled = false;
        }

        /// <summary>
        /// Handles video capture ending.
        /// </summary>
        /// <param name="path">file path to load captured video to.</param>
        public void OnCaptureEnded(string path)
        {
            // Manage canvas visuals
            _recordingIndicator.SetActive(false);
            _previewText.text = "Press the bumper to start capturing a video.";

            // Load the captured video
            _mediaPlayer.VideoSource = path;
            MLResult result = _mediaPlayer.PrepareVideo();
            if (!result.IsOk)
            {
                Debug.LogError(result);
            }
        }

        /// <summary>
        /// Executed when video has successfully loaded
        /// </summary>
        private void HandleVideoPrepared()
        {
            _mediaPlayer.IsLooping = true;

            StartCoroutine(EnablePreview());
        }
        #endregion

        #region Private Methods
        private IEnumerator EnablePreview()
        {
            // delay is needed for Media Player to load the video after preparing it
            // otherwise, the last frame from the prevous capture will show up
            yield return new WaitForSeconds(SCREEN_PREVIEW_DELAY);
            _screenRenderer.enabled = true;
        }
        #endregion
    }
}
