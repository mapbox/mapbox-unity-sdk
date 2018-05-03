﻿//-----------------------------------------------------------------------
// <copyright file="InstantPreviewManager.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCoreInternal
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SpatialTracking;

    /// <summary>
    /// Contains methods for managing communication to the Instant Preview 
    /// plugin.
    /// </summary>
    public static class InstantPreviewManager
    {
        /// <summary>
        /// Name of the Instant Preview plugin library.
        /// </summary>
        public const string InstantPreviewNativeApi = "instant_preview_unity_plugin";

        // Guid is taken from meta file and should never change.
        private const string k_ApkGuid = "cf7b10762fe921e40a18151a6c92a8a6";
        private const string k_NoDevicesFoundAdbResult = "error: no devices/emulators found";
        private const float k_MaxTolerableAspectRatioDifference = 0.1f;
        private const string k_MismatchedAspectRatioWarningFormatString =
            "The aspect ratio of your game window is different from the aspect ratio of your Instant Preview camera " +
            "texture. Please resize your game window's aspect ratio to match, or your preview will be distorted. The " +
            "camera texture resolution is {0}, {1}.";

        private static readonly WaitForEndOfFrame k_WaitForEndOfFrame = new WaitForEndOfFrame();

        /// <summary>
        /// Coroutine method that communicates to the Instant Preview plugin 
        /// every frame.
        /// 
        /// If not running in the editor, this does nothing.
        /// </summary>
        /// <returns>Enumerator for a coroutine that updates Instant Preview
        /// every frame.</returns>
        public static IEnumerator InitializeIfNeeded()
        {
            // Terminates if not running in editor.
            if (!Application.isEditor)
            {
                yield break;
            }

            // User may have explicitly disabled Instant Preview.
            if (ARCoreProjectSettings.Instance != null && 
                !ARCoreProjectSettings.Instance.IsInstantPreviewEnabled)
            {
                yield break;
            }

            var adbPath = InstantPreviewManager.GetAdbPath();
            if (adbPath == null)
            {
                Debug.LogError("Instant Preview requires your Unity Android SDK path to be set. Please set it under " +
                               "Preferences/External Tools/Android. You may need to install the Android SDK first.");
                yield break;
            }
            else if (!File.Exists(adbPath))
            {
                Debug.LogErrorFormat("adb not found at \"{0}\". Please add adb to your SDK path and restart the Unity editor.", adbPath);
                yield break;
            }

            string localVersion;
            if (!StartServer(adbPath, out localVersion))
            {
                yield break;
            }

            yield return InstallApkAndRunIfConnected(adbPath, localVersion);

            yield return UpdateLoop();
        }

        /// <summary>
        /// Uploads the latest camera video frame received from Instant Preview
        /// to the specified texture. The texture might be recreated if it is
        /// not the right size or null.
        /// </summary>
        /// <param name="backgroundTexture">Texture variable to store the latest
        /// Instant Preview video frame.</param>
        /// <returns>True if InstantPreview updated the background texture, 
        /// false if it did not and the texture still needs updating.</returns>
        public static bool UpdateBackgroundTextureIfNeeded(ref Texture2D backgroundTexture)
        {
            if (!Application.isEditor)
            {
                return false;
            }

            IntPtr pixelBytes;
            int width;
            int height;
            if (NativeApi.LockCameraTexture(out pixelBytes, out width, out height))
            {
                if (backgroundTexture == null || width != backgroundTexture.width ||
                    height != backgroundTexture.height)
                {
                    backgroundTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
                }

                backgroundTexture.LoadRawTextureData(pixelBytes, width * height * 4);
                backgroundTexture.Apply();

                NativeApi.UnlockCameraTexture();
            }

            return true;
        }

        private static IEnumerator UpdateLoop()
        {
            // Creates a target texture to capture the preview window onto.
            // Some video encoders prefer the dimensions to be a multiple of 16.
            var targetWidth = RoundUpToNearestMultipleOf16(Screen.width);
            var targetHeight = RoundUpToNearestMultipleOf16(Screen.height);
            var screenTexture = new RenderTexture(targetWidth, targetHeight, 0);

            var renderEventFunc = NativeApi.GetRenderEventFunc();
            var shouldConvertToBrgra = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11;
            var targetTexture = screenTexture;
            RenderTexture bgrTexture = null;
            if (shouldConvertToBrgra)
            {
                bgrTexture = new RenderTexture(screenTexture.width, screenTexture.height, 0, RenderTextureFormat.BGRA32);
                targetTexture = bgrTexture;
            }

            var loggedAspectRatioWarning = false;

            // Begins update loop. The coroutine will cease when the 
            // ARCoreSession component it's called from is destroyed.
            for (;;)
            {
                yield return k_WaitForEndOfFrame;
                NativeApi.Update();
                InstantPreviewInput.Update();
                AddInstantPreviewTrackedPoseDriverWhenNeeded();

                Graphics.Blit(null, screenTexture);

                if (shouldConvertToBrgra)
                {
                    Graphics.Blit(screenTexture, bgrTexture);
                }

                var cameraTexture = Frame.CameraImage.Texture;
                if (!loggedAspectRatioWarning && cameraTexture != null)
                {
                    var sourceWidth = cameraTexture.width;
                    var sourceHeight = cameraTexture.height;
                    var sourceAspectRatio = (float)sourceWidth / sourceHeight;
                    var destinationWidth = Screen.width;
                    var destinationHeight = Screen.height;
                    var destinationAspectRatio = (float)destinationWidth / destinationHeight;

                    if (Mathf.Abs(sourceAspectRatio - destinationAspectRatio) >
                        k_MaxTolerableAspectRatioDifference)
                    {
                        Debug.LogWarning(string.Format(k_MismatchedAspectRatioWarningFormatString, sourceWidth,
                                                       sourceHeight));
                        loggedAspectRatioWarning = true;
                    }
                }

                NativeApi.SendFrame(targetTexture.GetNativeTexturePtr());
                GL.IssuePluginEvent(renderEventFunc, 69);
            }
        }

        private static void AddInstantPreviewTrackedPoseDriverWhenNeeded()
        {
            foreach (var poseDriver in Component.FindObjectsOfType<TrackedPoseDriver>())
            {
                poseDriver.enabled = false;
                var gameObject = poseDriver.gameObject;
                var hasInstantPreviewTrackedPoseDriver =
                    gameObject.GetComponent<InstantPreviewTrackedPoseDriver>() != null;
                if (!hasInstantPreviewTrackedPoseDriver)
                {
                    gameObject.AddComponent<InstantPreviewTrackedPoseDriver>();
                }
            }
        }

        private static string GetAdbPath()
        {
            string sdkRoot = null;
#if UNITY_EDITOR
            // Gets adb path and starts instant preview server.
            sdkRoot = UnityEditor.EditorPrefs.GetString("AndroidSdkRoot");
#endif // UNITY_EDITOR

            if (string.IsNullOrEmpty(sdkRoot))
            {
                return null;
            }

            // Gets adb path from known directory.
            var adbPath = Path.Combine(Path.GetFullPath(sdkRoot), "platform-tools" + Path.DirectorySeparatorChar + "adb");

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                adbPath = Path.ChangeExtension(adbPath, "exe");
            }

            return adbPath;
        }

        /// <summary>
        /// Tries to install and run the Instant Preview android app.
        /// </summary>
        /// <param name="adbPath">Path to adb to use for installing.</param>
        /// <param name="localVersion">Local version of Instant Preview plugin to compare installed APK against.</param>
        /// <returns>Enumerator for coroutine that handles installation if necessary.</returns>
        private static IEnumerator InstallApkAndRunIfConnected(string adbPath, string localVersion)
        {
            string apkPath = null;

#if UNITY_EDITOR
            apkPath = UnityEditor.AssetDatabase.GUIDToAssetPath(k_ApkGuid);
#endif // !UNITY_EDITOR

            // Early outs if set to install but the apk can't be found.
            if (!File.Exists(apkPath))
            {
                Debug.LogError(
                    string.Format("Trying to install Instant Preview apk but reference to InstantPreview.apk is " +
                                  "broken. Couldn't find an asset with .meta file guid={0}", k_ApkGuid));
                yield break;
            }

            Result result = new Result();

            Thread checkAdbThread = new Thread((object obj) =>
            {
                Result res = (Result)obj;
                string output;
                string errors;

                // Gets version of installed apk.
                RunAdbCommand(adbPath, "shell dumpsys package com.google.ar.core.instantpreview | grep versionName",
                              out output, out errors);
                string installedVersion = null;
                if (!string.IsNullOrEmpty(output) && string.IsNullOrEmpty(errors))
                {
                    installedVersion = output.Substring(output.IndexOf('=') + 1);
                }

                // Early outs if no device is connected.
                if (string.Compare(errors, k_NoDevicesFoundAdbResult) == 0)
                {
                    return;
                }

                // Prints errors and exits on failure.
                if (!string.IsNullOrEmpty(errors))
                {
                    Debug.LogError(errors);
                    return;
                }

                if (installedVersion == null)
                {
                    Debug.Log(string.Format(
                        "Instant Preview: app not found on device, attempting to install it from {0}.",
                        apkPath));
                }
                else if (installedVersion != localVersion)
                {
                    Debug.Log(string.Format(
                        "Instant Preview: installed version \"{0}\" does not match local version \"{1}\", attempting upgrade.",
                        installedVersion, localVersion));
                }

                res.ShouldPromptForInstall = installedVersion != localVersion;
            });
            checkAdbThread.Start(result);

            while (!checkAdbThread.Join(0))
            {
                yield return 0;
            }

            if (result.ShouldPromptForInstall)
            {
                if (PromptToInstall())
                {
                    Thread installThread = new Thread(() =>
                    {
                        string output;
                        string errors;

                        RunAdbCommand(adbPath,
                            string.Format("uninstall com.google.ar.core.instantpreview", apkPath),
                            out output, out errors);

                        RunAdbCommand(adbPath,
                            string.Format("install \"{0}\"", apkPath),
                            out output, out errors);

                        // Prints any output from trying to install.
                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.Log(output);
                        }

                        if (!string.IsNullOrEmpty(errors))
                        {
                            if (string.Equals(errors, "Success"))
                            {
                                Debug.Log("Successfully installed Instant Preview app.");
                            }
                            else
                            {
                                Debug.LogError(errors);
                            }
                        }
                    });
                    installThread.Start();

                    while (!installThread.Join(0))
                    {
                        yield return 0;
                    }
                }
                else
                {
                    yield break;
                }
            }

            if (!NativeApi.IsConnected())
            {
                new Thread(() =>
                {
                    string output;
                    string errors;
                    RunAdbCommand(adbPath,
                        "shell am start -n com.google.ar.core.instantpreview/.InstantPreviewActivity",
                        out output, out errors);
                }).Start();
            }
        }

        private static bool PromptToInstall()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.DisplayDialog("Instant Preview",
                        "To instantly reflect your changes on device, the " +
                        "Instant Preview app will be installed on your " +
                        "connected phone.\n\nTo permanently disable Instant Preview, " +
                        "uncheck the InstantPreviewEnabled checkbox in Edit/Project Settings/ARCore Instant Preview Enabled.", "Okay", "Don't Install This Time");
#else
            return false;
#endif
        }

        private static void RunAdbCommand(string fileName, string arguments, out string output, out string errors)
        {
            using (var process = new System.Diagnostics.Process())
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo(fileName, arguments);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();
                process.OutputDataReceived += (sender, ef) => outputBuilder.Append(ef.Data);
                process.ErrorDataReceived += (sender, ef) => errorBuilder.Append(ef.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.Close();

                // Trims the output strings to make comparison easier.
                output = outputBuilder.ToString().Trim();
                errors = errorBuilder.ToString().Trim();
            }
        }

        private static bool StartServer(string adbPath, out string version)
        {
            // Tries to start server.
            const int k_InstantPreviewVersionStringMaxLength = 64;
            var versionStringBuilder = new StringBuilder(k_InstantPreviewVersionStringMaxLength);
            var started = NativeApi.InitializeInstantPreview(adbPath, versionStringBuilder,
                                                             versionStringBuilder.Capacity);
            if (!started)
            {
                Debug.LogErrorFormat("Couldn't start Instant Preview server with adb path: {0}.", adbPath);
                version = null;
                return false;
            }

            version = versionStringBuilder.ToString();
            Debug.Log("Instant Preview Version: " + version);
            return true;
        }

        private static int RoundUpToNearestMultipleOf16(int value)
        {
            return (value + 15) & ~15;
        }

        private struct NativeApi
        {
            [DllImport(InstantPreviewNativeApi)]
            public static extern bool InitializeInstantPreview(
                string adbPath, StringBuilder version, int versionStringLength);

            [DllImport(InstantPreviewNativeApi)]
            public static extern void Update();

            [DllImport(InstantPreviewNativeApi)]
            public static extern IntPtr GetRenderEventFunc();

            [DllImport(InstantPreviewNativeApi)]
            public static extern void SendFrame(IntPtr renderTexture);

            [DllImport(InstantPreviewNativeApi)]
            public static extern bool LockCameraTexture(out IntPtr pixelBytes, out int width,
                out int height);

            [DllImport(InstantPreviewNativeApi)]
            public static extern void UnlockCameraTexture();

            [DllImport(InstantPreviewNativeApi)]
            public static extern bool IsConnected();
        }

        private class Result
        {
            public bool ShouldPromptForInstall = false;
        }
    }
}
