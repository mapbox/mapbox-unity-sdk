//-----------------------------------------------------------------------
// <copyright file="NativeSession.cs" company="Google">
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
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using GoogleARCore;
    using UnityEngine;

    internal class NativeSession
    {
        private float m_LastReleasedPointcloudTimestamp = 0.0f;

        private TrackableManager m_TrackableManager = null;

        private Dictionary<IntPtr, int> m_AcquiredHandleCounts = new Dictionary<IntPtr, int>(
            new IntPtrEqualityComparer());

        public NativeSession(IntPtr sessionHandle, IntPtr frameHandle)
        {
            SessionHandle = sessionHandle;
            FrameHandle = frameHandle;
            m_TrackableManager = new TrackableManager(this);

            AnchorApi = new AnchorApi(this);
            AugmentedImageApi = new AugmentedImageApi(this);
            AugmentedImageDatabaseApi = new AugmentedImageDatabaseApi(this);
            CameraApi = new CameraApi(this);
            CameraMetadataApi = new CameraMetadataApi(this);
            FrameApi = new FrameApi(this);
            HitTestApi = new HitTestApi(this);
            ImageApi = new ImageApi(this);
            LightEstimateApi = new LightEstimateApi(this);
            PlaneApi = new PlaneApi(this);
            PointApi = new PointApi(this);
            PointCloudApi = new PointCloudApi(this);
            PoseApi = new PoseApi(this);
            SessionApi = new SessionApi(this);
            SessionConfigApi = new SessionConfigApi(this);
            TrackableApi = new TrackableApi(this);
            TrackableListApi = new TrackableListApi(this);
        }

        public IntPtr SessionHandle { get; private set; }

        public IntPtr FrameHandle { get; private set; }

        public IntPtr PointCloudHandle { get; private set; }

        public bool IsPointCloudNew
        {
            get
            {
                // TODO (b/73256094): Remove when fixed.
                if (LifecycleManager.Instance.IsTracking)
                {
                    var previousLastTimestamp = m_LastReleasedPointcloudTimestamp;
                    m_LastReleasedPointcloudTimestamp = 0.0f;
                    return previousLastTimestamp != 0;
                }

                return PointCloudApi.GetTimestamp(PointCloudHandle) != m_LastReleasedPointcloudTimestamp;
            }
        }

        public AnchorApi AnchorApi { get; private set; }

        public AugmentedImageApi AugmentedImageApi { get; private set; }

        public AugmentedImageDatabaseApi AugmentedImageDatabaseApi { get; private set; }

        public CameraApi CameraApi { get; private set; }

        public CameraMetadataApi CameraMetadataApi { get; private set; }

        public FrameApi FrameApi { get; private set; }

        public HitTestApi HitTestApi { get; private set; }

        public ImageApi ImageApi { get; private set; }

        public LightEstimateApi LightEstimateApi { get; private set; }

        public PlaneApi PlaneApi { get; private set; }

        public PointApi PointApi { get; private set; }

        public PointCloudApi PointCloudApi { get; private set; }

        public PoseApi PoseApi { get; private set; }

        public SessionApi SessionApi { get; private set; }

        public SessionConfigApi SessionConfigApi { get; private set; }

        public TrackableApi TrackableApi { get; private set; }

        public TrackableListApi TrackableListApi { get; private set; }

        public void MarkHandleAcquired(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                Debug.LogError("MarkHandleAcquired::Attempted to mark a null handle acquired.");
                return;
            }

            int acquireCount;
            m_AcquiredHandleCounts.TryGetValue(handle, out acquireCount);
            m_AcquiredHandleCounts[handle] = ++acquireCount;
        }

        public void MarkHandleReleased(IntPtr handle)
        {
            int acquireCount;
            if (m_AcquiredHandleCounts.TryGetValue(handle, out acquireCount))
            {
                if (--acquireCount > 0)
                {
                    m_AcquiredHandleCounts[handle] = acquireCount;
                }
                else
                {
                    m_AcquiredHandleCounts.Remove(handle);
                }
            }
        }

        public bool IsHandleAcquired(IntPtr handle)
        {
            int acquireCount;
            m_AcquiredHandleCounts.TryGetValue(handle, out acquireCount);
            return acquireCount > 0;
        }

        public Trackable TrackableFactory(IntPtr nativeHandle)
        {
            return m_TrackableManager.TrackableFactory(nativeHandle);
        }

        public void GetTrackables<T>(List<T> trackables, TrackableQueryFilter filter) where T : Trackable
        {
            m_TrackableManager.GetTrackables<T>(trackables, filter);
        }

        public void OnUpdate(IntPtr frameHandle)
        {
            FrameHandle = frameHandle;

            if (ApiConstants.isBehaveAsIfOnAndroid)
            {
                // After first frame, release previous frame's point cloud.
                if (PointCloudHandle != IntPtr.Zero)
                {
                    m_LastReleasedPointcloudTimestamp = PointCloudApi.GetTimestamp(PointCloudHandle);
                    PointCloudApi.Release(PointCloudHandle);
                    PointCloudHandle = IntPtr.Zero;
                }

                // TODO (b/73256094): Remove when fixed.
                if (LifecycleManager.Instance.IsTracking)
                {
                    IntPtr pointCloudHandle;
                    FrameApi.TryAcquirePointCloudHandle(out pointCloudHandle);
                    PointCloudHandle = pointCloudHandle;
                }
            }
        }
    }
}
