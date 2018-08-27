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
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Lumin;
using UnityEditor.Lumin.Utils;
using UnityEngine;

namespace UnityEditor.Experimental.XR.MagicLeap
{
    /// <summary>
    /// Editor option to build custom lib*.so files with Mabu.
    /// </summary>
    public class BuildMabuWindow : EditorWindow
    {
        private static BuildMabuWindow _instance;
        private bool _initialized = false;
        private string _outputDir;
        private string _generatedOutputDir;
        private string _outputDirDefault;
        private string _pluginFilePath;
        private string _mabuFileName;
        private string _libFileName;
        private string _argumentString;
        private bool _releaseBuild = false;
        private SDK _sdk;

        private string MLPluginPath { get; set; }
        private string GeneratedBinaryDirectoryName
        {
            get
            {
                if (_releaseBuild)
                {
                    return @"release_lumin_clang-3.8_aarch64";
                }
                return @"debug_lumin_clang-3.8_aarch64";
            }
        }

        private GUIStyle _warningStyle;
        private GUIStyle WarningStyle
        {
            get
            {
                // Set up Custom Styles if needed
                if (_warningStyle == null)
                {
                    _warningStyle = new GUIStyle(EditorStyles.helpBox);
                    _warningStyle.normal.textColor = Color.yellow;
                    _warningStyle.fontSize = 12;
                    _warningStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _warningStyle;
            }
            set { _warningStyle = value; }
        }

        private string GetBuildType
        {
            get
            {
                return _releaseBuild ? "release_ml1" : "ml1";
            }
        }

        /// <summary>
        /// Open dialog to build plugin.
        /// </summary>
        [MenuItem("Magic Leap/Build Native Plugin")]
        public static void Create()
        {
            if (!_instance)
            {
                _instance = (BuildMabuWindow)EditorWindow.GetWindow(typeof(BuildMabuWindow), true);
                _instance.MLPluginPath = Path.Combine(Application.dataPath, Path.Combine("Plugins", "ML"));
                _instance._sdk = SDK.Find(true);
                _instance.minSize = new Vector2(500, 500);
                _instance.maxSize = _instance.minSize;
                _instance.titleContent = new GUIContent("Build w/ Mabu");
                _instance._pluginFilePath = EditorPrefs.GetString("MabuPluginFilePath");
                _instance._outputDir = EditorPrefs.GetString("MabuOutputpath");
                _instance.Show();
            }
        }

        private void OnEnable()
        {
            _initialized = true;
        }

        private void GenerateButton(string label, Vector2 size, System.Action OnClick)
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(size.y));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            Rect buttonRect = EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(size.x));
            if (GUI.Button(buttonRect, GUIContent.none))
            {
                OnClick.Invoke();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(label, GUILayout.MaxWidth (size.x));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void OnDisable()
        {
            if (_initialized)
            {
                _instance = null;
                _initialized = false;
            }
        }

        private void OnGUI()
        {
            if (_initialized)
            {
                try
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(this.position.width - 20), GUILayout.Height(this.position.height - 20));
                        GUILayout.Space(20);
                        EditorGUILayout.LabelField("MLSDK path set in Build Settings:");
                        if (_sdk != null && !String.IsNullOrEmpty(_sdk.Path))
                        {
                            EditorGUILayout.LabelField(_sdk.Path);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Warning no MLSDK path found", WarningStyle);
                        }
                        GUILayout.Space(20);

                        EditorGUILayout.LabelField("Mabu build file:");
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(this.position.width - 20));
                            _pluginFilePath = EditorGUILayout.TextField(_pluginFilePath, GUILayout.Width(this.position.width - 75), GUILayout.Height(20));
                            GUILayout.Space(5);
                            GUI.SetNextControlName("BrowseSourceButton");
                            GenerateButton(" Browse", new Vector2(53, 17), () =>
                            {
                                string target = EditorUtility.OpenFilePanel("Build Source Directory", string.Empty, "mabu");
                                if (target != string.Empty)
                                {
                                    _pluginFilePath = target;
                                    _outputDirDefault = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/"));
                                    if (String.IsNullOrEmpty(_outputDir))
                                    {
                                        _outputDir = _outputDirDefault;
                                    }
                                }
                                GUI.FocusControl("BrowseSourceButton");
                                this.Repaint();
                            });
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.LabelField("Output Directory:");
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(this.position.width - 20));
                            string outputDir = EditorGUILayout.TextField(_outputDir, GUILayout.Width(this.position.width - 75), GUILayout.Height(20));
                            if (!String.IsNullOrEmpty(outputDir))
                            {
                                _outputDir = Path.GetFullPath(outputDir + "/");
                            }
                            GUILayout.Space(5);
                            GUI.SetNextControlName("BrowseOutputButton");
                            GenerateButton(" Browse", new Vector2(53, 17), () =>
                            {
                                if (!System.IO.Directory.Exists(_outputDir))
                                {
                                    _outputDir = _outputDirDefault != string.Empty ? _outputDirDefault : Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/"));
                                }
                                string target = EditorUtility.OpenFolderPanel("Build Output Directory", _outputDir, string.Empty);
                                if (target != string.Empty)
                                {
                                    _outputDir = target + "/";
                                }
                                if (_outputDir.Contains(Application.dataPath))
                                {
                                    EditorUtility.DisplayDialog("Invalid Output Directory", "Output Directory can not be inside of your project folder.\nSetting back to default value.", "OK");
                                    _outputDir = !String.IsNullOrEmpty(_outputDirDefault) ? _outputDirDefault : Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/"));
                                }
                                GUI.FocusControl("BrowseOutputButton");
                                this.Repaint();
                            });
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(10);
                        _releaseBuild = EditorGUILayout.Toggle("Release Build", _releaseBuild);
                    EditorGUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(this.position.width - 6));
                            GUILayout.FlexibleSpace();
                            GenerateButton("  Build Plugin", new Vector2(80, 17), BuildNativePlugin);
                        EditorGUILayout.EndHorizontal();
                        GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                }
                catch (System.InvalidOperationException) { /* Strange Exception deep in Unity GUI system when Build is in progress. */ }
            }
        }

        private void BuildNativePlugin()
        {
            EditorPrefs.SetString("MabuPluginFilePath", _pluginFilePath);
            EditorPrefs.SetString("MabuOutputpath", _outputDir);

            _mabuFileName = Path.GetFileName(_pluginFilePath);
            _libFileName = String.Format("lib{0}.so", Path.GetFileNameWithoutExtension(_pluginFilePath));
            _generatedOutputDir = Path.Combine(_outputDir, Path.GetFileNameWithoutExtension(_pluginFilePath));
            _argumentString = String.Format("{0} -t {1} --out \"{2}\"", _mabuFileName, GetBuildType, _generatedOutputDir);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = _sdk.Mabu.Path;
            startInfo.Arguments = _argumentString;
            startInfo.Verb = "runas";
            startInfo.WorkingDirectory = Path.GetDirectoryName(_pluginFilePath);
            startInfo.ErrorDialog = true;

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            string line = process.StandardOutput.ReadToEnd();
            UnityEngine.Debug.Log(line);
            string error = process.StandardError.ReadToEnd();
            if (error != string.Empty)
            {
                UnityEngine.Debug.LogError(error);
            }

            int ExitCode = process.ExitCode;
            if (ExitCode == 0)
            {
                if (CopyPlugin())
                {
                    AssetDatabase.Refresh();
                    SetCompatible();
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Plugin Compilation Failed");
            }
        }

        private bool CopyPlugin()
        {
            // Create path where the plug in should be
            String pluginLocation = Path.Combine(_generatedOutputDir, Path.Combine(GeneratedBinaryDirectoryName, _libFileName));
            //Is the lib*.so file there??
            if (File.Exists(pluginLocation))
            {
                // make sure we have a proper ML Plugins folder
                if (!Directory.Exists(MLPluginPath))
                {
                    Directory.CreateDirectory(MLPluginPath);
                }
                // copy generated lib*.so file to the project ML Plugins folder
                FileUtil.ReplaceFile(pluginLocation, Path.Combine(MLPluginPath, _libFileName));
            }
            else
            {
                UnityEngine.Debug.LogErrorFormat("Plugin copy failed, {0} not found.", _libFileName);
                return false;
            }
            return true;
        }

        private void SetCompatible ()
        {
            String pluginLocation = Path.Combine(MLPluginPath, _libFileName);
            int startIndex = pluginLocation.IndexOf("Assets");
            int endIndex = pluginLocation.Length - startIndex;
            String subString = pluginLocation.Substring(startIndex, endIndex);

            PluginImporter mabuNativePlugin = AssetImporter.GetAtPath(subString) as PluginImporter;
            if (mabuNativePlugin)
            {
                mabuNativePlugin.SetCompatibleWithEditor (false);
                mabuNativePlugin.SetCompatibleWithAnyPlatform (false);
                mabuNativePlugin.SetCompatibleWithPlatform (BuildTarget.Lumin, true);
                StackTraceLogType originalType = Application.GetStackTraceLogType(LogType.Log);
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                UnityEngine.Debug.Log(String.Format("Plugin {0} ready for use.", _libFileName));
                Application.SetStackTraceLogType(LogType.Log, originalType);
            }
            else
            {
                UnityEngine.Debug.LogErrorFormat("Plugin Compatibility failed for {0}", _libFileName);
            }
        }
    }
}