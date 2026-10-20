using System;
using System.IO;
using UnityEngine;
#if UNITY_RECORDER && UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Wraps Unity Recorder for benchmark video capture.
    /// Consolidates recording code previously duplicated across TileProviderBenchmark,
    /// SequenceControllerBehaviour, and SceneTests.
    /// Requires Unity Recorder package (conditional on UNITY_RECORDER define).
    /// </summary>
    public class BenchmarkVideoRecorder : MonoBehaviour
    {
        [Tooltip("Output directory for video files. Defaults to Application.persistentDataPath/benchmarks")]
        public string OutputDirectory;

        [Tooltip("Video width in pixels")]
        public int Width = 1920;

        [Tooltip("Video height in pixels")]
        public int Height = 1080;

        [Tooltip("Recording frame rate")]
        public float FrameRate = 60f;

#if UNITY_RECORDER && UNITY_EDITOR
        [Tooltip("Video quality / bitrate mode")]
        public VideoBitrateMode BitrateMode = VideoBitrateMode.High;

        private RecorderController _controller;
#endif

        private string _currentOutputPath;

        /// <summary>Full path to the recorded video file (without extension).</summary>
        public string CurrentOutputPath => _currentOutputPath;

        public bool IsRecording
        {
            get
            {
#if UNITY_RECORDER && UNITY_EDITOR
                return _controller != null && _controller.IsRecording();
#else
                return false;
#endif
            }
        }

        private string GetOutputDirectory()
        {
            if (!string.IsNullOrEmpty(OutputDirectory))
                return OutputDirectory;
            return Path.Combine(Application.persistentDataPath, "benchmarks");
        }

        public void StartRecording(string runId)
        {
#if UNITY_RECORDER && UNITY_EDITOR
            var outputDir = GetOutputDirectory();
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            _currentOutputPath = Path.Combine(outputDir, runId);

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = FrameRate;

            var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = "BenchmarkRecorder";
            videoRecorder.Enabled = true;
            videoRecorder.VideoBitRateMode = BitrateMode;
            videoRecorder.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = Width,
                OutputHeight = Height,
            };
            videoRecorder.AudioInputSettings.PreserveAudio = false;
            videoRecorder.OutputFile = _currentOutputPath;

            controllerSettings.AddRecorderSettings(videoRecorder);

            _controller = new RecorderController(controllerSettings);
            _controller.PrepareRecording();
            _controller.StartRecording();
            Debug.Log($"[BenchmarkVideoRecorder] Recording started: {_currentOutputPath}");
#else
            Debug.LogWarning("[BenchmarkVideoRecorder] Unity Recorder package not available. Video recording disabled.");
#endif
        }

        public void StopRecording()
        {
#if UNITY_RECORDER && UNITY_EDITOR
            if (_controller != null && _controller.IsRecording())
            {
                _controller.StopRecording();
                Debug.Log($"[BenchmarkVideoRecorder] Recording saved: {_currentOutputPath}");
                _controller = null;
            }
#endif
        }
    }
}
