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
using System;
using System.IO;
using System.Collections;

namespace MagicLeap
{
    /// <summary>
    /// This class demonstrates using the MLMediaPlayer API
    /// </summary>
    public class MediaPlayerExample : MonoBehaviour
    {
        #region Private Variables
        private const float STARTING_VOLUME = 0.75f;
        private const int SEEK_TOLERANCE = 1000; // in milliseconds

        [SerializeField, Tooltip("MeshRenderer to display media")]
        private MeshRenderer _screen;

        [SerializeField, Tooltip("Pause/Play Button")]
        private MediaPlayerToggle _pausePlayButton;

        [SerializeField, Tooltip("Play Material")]
        private Material _playMaterial;
        [SerializeField, Tooltip("Pause Material")]
        private Material _pauseMaterial;

        [SerializeField, Tooltip("Rewind Button")]
        private MediaPlayerButton _rewindButton;

        [SerializeField, Tooltip("Number of ms to rewind")]
        private int _rewindMS = -10000;

        [SerializeField, Tooltip("Forward Button")]
        private MediaPlayerButton _forwardButton;

        [SerializeField, Tooltip("Number of ms to forward")]
        private int _forwardMS = 10000;

        [SerializeField, Tooltip("Timeline Slider")]
        private MediaPlayerSlider _timelineSlider;

        [SerializeField, Tooltip("Buffer Bar")]
        private Transform _bufferBar;

        [SerializeField, Tooltip("Volume Slider")]
        private MediaPlayerSlider _volumeSlider;

        [SerializeField, Tooltip("Text Mesh for Elapsed Time")]
        private TextMesh _elapsedTime;

        // For online videos, web URLs are accepted
        // For local videos, the asset should be placed in Assets/StreamingAssets/
        //   and the url should be relative to Assets/StreamingAssets/
        [SerializeField, Tooltip("URL of Video to be played")]
        private string _url;

        // DRM-free videos should leave this blank
        [SerializeField, Tooltip("Optional URL of DRM video license server")]
        private string _licenseUrl;

        [SerializeField, Tooltip("Status Text (can be empty)")]
        private TextMesh _statusText;

        private MLMediaPlayer _mediaPlayer;
        private Button _lastButtonHit;
        private bool _isSeeking = false;
        private int _lastTimeSoughtMs;
        #endregion // Private Variables

        #region Unity Methods
        private void Awake()
        {
            if (_screen == null)
            {
                Debug.LogError("Error MediaPlayerExample._screen is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_pausePlayButton == null)
            {
                Debug.LogError("Error MediaPlayerExample._pausePlay is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_playMaterial == null)
            {
                Debug.LogError("Error MediaPlayerExample._playMaterial is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_pauseMaterial == null)
            {
                Debug.LogError("Error MediaPlayerExample._pauseMaterial is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_rewindButton == null)
            {
                Debug.LogError("Error MediaPlayerExample._rewindButton is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_forwardButton == null)
            {
                Debug.LogError("Error MediaPlayerExample._forwardButton is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_timelineSlider == null)
            {
                Debug.LogError("Error MediaPlayerExample._timelineSlider is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_bufferBar == null)
            {
                Debug.LogError("Error MediaPlayerExample._bufferBar is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_volumeSlider == null)
            {
                Debug.LogError("Error MediaPlayerExample._volumeSlider is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_elapsedTime == null)
            {
                Debug.LogError("Error MediaPlayerExample._elapsedTime is not set, disabling script.");
                enabled = false;
                return;
            }

            _mediaPlayer = _screen.gameObject.AddComponent<MLMediaPlayer>();

            _mediaPlayer.OnPause += HandlePause;
            _mediaPlayer.OnPlay += HandlePlay;
            _mediaPlayer.OnStop += HandleStop;
            _mediaPlayer.OnEnded += HandleEnded;
            _mediaPlayer.OnSeekStarted += HandleSeekStarted;
            _mediaPlayer.OnSeekCompleted += HandleSeekCompleted;
            _mediaPlayer.OnBufferingUpdate += HandleBufferUpdate;
            _mediaPlayer.OnError += HandleError;
            _mediaPlayer.OnInfo += HandleInfo;
            _mediaPlayer.OnVideoPrepared += HandleVideoPrepared;

            _pausePlayButton.OnToggle += PlayPause;
            _rewindButton.OnControllerTriggerDown += Rewind;
            _forwardButton.OnControllerTriggerDown += FastForward;
            _timelineSlider.OnValueChanged += Seek;
            _volumeSlider.OnValueChanged += SetVolume;
        }

        private void Start()
        {
            _mediaPlayer.VideoSource = _url;
            _mediaPlayer.LicenseServer = _licenseUrl;
            MLResult result = _mediaPlayer.PrepareVideo();
            if (!result.IsOk)
            {
                _statusText.text = result.ToString();
            }

            EnableUI(false);
            _timelineSlider.Value = 0;
        }

        private void OnDestroy()
        {
            _mediaPlayer.OnPause -= HandlePause;
            _mediaPlayer.OnPlay -= HandlePlay;
            _mediaPlayer.OnStop -= HandleStop;
            _mediaPlayer.OnEnded -= HandleEnded;
            _mediaPlayer.OnSeekStarted -= HandleSeekStarted;
            _mediaPlayer.OnSeekCompleted -= HandleSeekCompleted;
            _mediaPlayer.OnBufferingUpdate -= HandleBufferUpdate;
            _mediaPlayer.OnError -= HandleError;
            _mediaPlayer.OnInfo -= HandleInfo;
            _mediaPlayer.OnVideoPrepared -= HandleVideoPrepared;

            _pausePlayButton.OnToggle -= PlayPause;
            _rewindButton.OnControllerTriggerDown -= Rewind;
            _forwardButton.OnControllerTriggerDown -= FastForward;
            _timelineSlider.OnValueChanged -= Seek;
            _volumeSlider.OnValueChanged -= SetVolume;
        }

        private void Update()
        {
            if (_mediaPlayer.IsPlaying && !_isSeeking)
            {
                _timelineSlider.Value = _mediaPlayer.AnimationPosition;
                UpdateElapsedTime(_mediaPlayer.GetElapsedTimeMs());
            }
        }
        #endregion // Unity Methods

        #region Private Methods
        /// <summary>
        /// Function to update the elapsed time text
        /// </summary>
        /// <param name="elapsedTimeMs">Elapsed time in milliseconds</param>
        private void UpdateElapsedTime(long elapsedTimeMs)
        {
            TimeSpan timeSpan = new TimeSpan(elapsedTimeMs * TimeSpan.TicksPerMillisecond);
            _elapsedTime.text = String.Format("{0}:{1}:{2}",
                timeSpan.Hours.ToString(), timeSpan.Minutes.ToString("00"), timeSpan.Seconds.ToString("00"));
        }

        /// <summary>
        /// Enable all UI elements
        /// </summary>
        /// <param name="enabled">True if the UI should be enabled, false if disabled</param>
        private void EnableUI(bool enabled)
        {
            _forwardButton.enabled = enabled;
            _pausePlayButton.enabled = enabled;
            _rewindButton.enabled = enabled;
            _timelineSlider.enabled = enabled;
            _volumeSlider.enabled = enabled;
            if (!enabled)
            {
                _elapsedTime.text = "--:--:--";
            }
        }
        #endregion // Private Methods

        #region Event Handlers
        /// <summary>
        /// Event Handler when Media Player has reached the end of the media
        /// </summary>
        private void HandleEnded()
        {
            _pausePlayButton.State = false;
        }

        /// <summary>
        /// Event Handler when the Media Player is stopped
        /// </summary>
        private void HandleStop()
        {
            _pausePlayButton.State = false;
            _timelineSlider.enabled = false;
            _elapsedTime.text = "--:--:--";
        }

        /// <summary>
        /// Event Handler when the Media Player starts Playing
        /// </summary>
        /// <param name="durationMs">Total Duration of the media being played</param>
        private void HandlePlay(int durationMs)
        {
            _pausePlayButton.Material = _pauseMaterial;
        }

        /// <summary>
        /// Event Handler when the Media Player is paused
        /// </summary>
        private void HandlePause()
        {
            _pausePlayButton.Material = _playMaterial;
        }

        /// <summary>
        /// Event Handler when a Seek() operation is initiated. For non-local media,
        /// this means it has started buffering.
        /// </summary>
        /// <param name="percent">Percent of whole duration (0.0f to 1.0f)</param>
        private void HandleSeekStarted(float percent)
        {
            _lastTimeSoughtMs = Mathf.RoundToInt(percent * _mediaPlayer.GetDurationMs());
            _isSeeking = true;

            _timelineSlider.Value = percent;
            UpdateElapsedTime(_lastTimeSoughtMs);
        }

        /// <summary>
        /// Event Handler when a Seek() operation is completed. For non-local media, this
        /// means it has buffered enough content to resume playing. Another Seek() operation
        /// may begin while a previous one is still buffering. This event is called for
        /// every completed Seek() operation, even if others are pending.
        /// </summary>
        /// <param name="percent">Percent of whole duration (0.0f to 1.0f)</param>
        private void HandleSeekCompleted(float percent)
        {
            int timeSoughtMs = Mathf.RoundToInt(percent * _mediaPlayer.GetDurationMs());

            // Determine if we are done seeking by computing
            // whether the time delta between the last completed
            // seek and this seek falls within tolerance.
            if (Mathf.Abs(timeSoughtMs - _lastTimeSoughtMs) < SEEK_TOLERANCE)
            {
                _isSeeking = false;
            }
        }

        /// <summary>
        /// Event handler when buffer gets updated. This is only called when the video is streaming.
        /// </summary>
        /// <param name="percent">Percent of the whole duration, [0, 1]</param>
        private void HandleBufferUpdate(float percent)
        {
            Vector3 barScale = _bufferBar.localScale;
            barScale.x = percent;
            _bufferBar.localScale = barScale;
        }

        /// <summary>
        /// Event Handler when an error occurs
        /// </summary>
        /// <param name="error">The MLMediaPlayerResult</param>
        /// <param name="errorString">String version of the error</param>
        private void HandleError(MLMediaPlayerResult error, string errorString)
        {
            if (_statusText != null)
            {
                _statusText.text = errorString;
            }
        }

        /// <summary>
        /// Event Handler for miscellaneous informational events
        /// </summary>
        /// <param name="info">The event that occurred</param>
        /// <param name="extra">The data associated with the event (if any), otherwise, 0</param>
        private void HandleInfo(MLMediaPlayerInfo info, int extra)
        {
            if (info == MLMediaPlayerInfo.NetworkBandwidth)
            {
                // source media is not local
                // the parameter extra would contain bandwidth in kbps
            }
        }

        /// <summary>
        /// Event Handler for when a video has been prepared and is ready to begin playback
        /// </summary>
        private void HandleVideoPrepared()
        {
            _volumeSlider.Value = STARTING_VOLUME;
            EnableUI(true);
        }

        /// <summary>
        /// Handler when Play/Pause Toggle is triggered.
        /// See HandlePlay() and HandlePause() for more info
        /// </summary>
        /// <param name="shouldPlay">True when resuming, false when should pause</param>
        private void PlayPause(bool shouldPlay)
        {
            if (_mediaPlayer != null)
            {
                if (!shouldPlay && _mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                }
                else if (shouldPlay && !_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Play();
                }
            }
        }

        /// <summary>
        /// Handler when Stop button has been triggered. See HandleStop() for more info.
        /// </summary>
        private void Stop()
        {
            _mediaPlayer.Stop();
        }

        /// <summary>
        /// Handler when Rewind button has been triggered.
        /// Moves the play head backward.
        /// </summary>
        /// <param name="triggerReading">Unused parameter</param>
        private void Rewind(float triggerReading)
        {
            // Note: this calls the int version of seek.
            // This moves the playhead by an offset in ms
            _mediaPlayer.Seek(_rewindMS);
        }

        /// <summary>
        /// Handler when Forward button has been triggered.
        /// Moves the play head forward.
        /// </summary>
        /// <param name="triggerReading">Unused parameter</param>
        private void FastForward(float triggerReading)
        {
            // Note: this calls the int version of seek.
            // This moves the playhead by an offset in ms
            _mediaPlayer.Seek(_forwardMS);
        }

        /// <summary>
        /// Handler when Timeline Slider has changed value.
        /// Moves the play head to a specific percentage of the whole duration.
        /// </summary>
        /// <param name="sliderValue">Normalized slider value</param>
        private void Seek(float sliderValue)
        {
            if (Mathf.Approximately(sliderValue, _mediaPlayer.AnimationPosition))
            {
                return;
            }

            _mediaPlayer.Seek(sliderValue);
        }

        /// <summary>
        /// Handler when Volume Sider has changed value.
        /// </summary>
        /// <param name="sliderValue">Normalized slider value</param>
        private void SetVolume(float sliderValue)
        {
            _mediaPlayer.SetVolume(sliderValue);
        }
        #endregion // Event Handlers
    }
}

