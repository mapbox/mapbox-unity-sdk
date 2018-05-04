//-----------------------------------------------------------------------
// <copyright file="Anchor.cs" company="Google">
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

namespace GoogleARCore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using GoogleARCoreInternal;
    using UnityEngine;

    /// <summary>
    /// Attaches a GameObject to an ARCore {@link Trackable}.  The transform of the GameObject will be updated to
    /// maintain the semantics of the attachment relationship, which varies between sub-types of Trackable.
    /// </summary>
    public class Anchor : MonoBehaviour
    {
        private static Dictionary<IntPtr, Anchor> s_AnchorDict = new Dictionary<IntPtr, Anchor>();

        private IntPtr m_AnchorNativeHandle = IntPtr.Zero;

        private NativeSession m_NativeSession;

        private TrackingState m_LastFrameTrackingState = TrackingState.Stopped;

        /// <summary>
        /// Gets the tracking state of the anchor.
        /// </summary>
        public TrackingState TrackingState
        {
            get
            {
                // TODO (b/73256094): Remove isTracking when fixed.
                var nativeSession = LifecycleManager.Instance.NativeSession;
                var isTracking = LifecycleManager.Instance.SessionStatus == SessionStatus.Tracking;
                if (nativeSession != m_NativeSession)
                {
                    // Anchors from another session are considered stopped.
                    return TrackingState.Stopped;
                }
                else if (!isTracking)
                {
                    // If there are no new frames coming in we must manually return paused.
                    return TrackingState.Paused;
                }

                return m_NativeSession.AnchorApi.GetTrackingState(m_AnchorNativeHandle);
            }
        }

        //// @cond EXCLUDE_FROM_DOXYGEN

        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented",
        Justification = "Internal")]
        public static Anchor AnchorFactory(IntPtr anchorNativeHandle, NativeSession nativeApi, bool isCreate = true)
        {
            if (anchorNativeHandle == IntPtr.Zero)
            {
                return null;
            }

            Anchor result;
            if (s_AnchorDict.TryGetValue(anchorNativeHandle, out result))
            {
                // Release acquired handle and return cached result
                result.m_NativeSession.AnchorApi.Release(anchorNativeHandle);
                return result;
            }

            if (isCreate)
            {
               Anchor anchor = (new GameObject()).AddComponent<Anchor>();
               anchor.gameObject.name = "Anchor";
               anchor.m_AnchorNativeHandle = anchorNativeHandle;
               anchor.m_NativeSession = nativeApi;
               anchor.Update();

               s_AnchorDict.Add(anchorNativeHandle, anchor);
               return anchor;
            }

            return null;
        }

        //// @endcond

        private void Update()
        {
            if (m_AnchorNativeHandle == IntPtr.Zero)
            {
                Debug.LogError("Anchor components instantiated outside of ARCore are not supported. " +
                    "Please use a 'Create' method within ARCore to instantiate anchors.");
                return;
            }

            var pose = m_NativeSession.AnchorApi.GetPose(m_AnchorNativeHandle);
            transform.position = pose.position;
            transform.rotation = pose.rotation;

            TrackingState currentFrameTrackingState = TrackingState;
            if (m_LastFrameTrackingState != currentFrameTrackingState)
            {
                bool isAnchorTracking = currentFrameTrackingState == TrackingState.Tracking;
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(isAnchorTracking);
                }

                m_LastFrameTrackingState = currentFrameTrackingState;
            }
        }

        private void OnDestroy()
        {
            if (m_AnchorNativeHandle == IntPtr.Zero)
            {
                return;
            }

            s_AnchorDict.Remove(m_AnchorNativeHandle);
            m_NativeSession.AnchorApi.Release(m_AnchorNativeHandle);
        }
    }
}
