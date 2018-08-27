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
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine.XR.MagicLeap;

/// <summary>
/// Editor option to print SDK version.
/// </summary>
public class MLVersionMenuItem
{
    /// <summary>
    /// Print SDK version to console.
    /// </summary>
    #if UNITY_EDITOR
    [MenuItem("Magic Leap/Print Version #&v")]
    public static void Print() {
        Debug.Log("SDK version: " + MLVersion.MLSDK_VERSION_NAME);
    }
    #endif
}
