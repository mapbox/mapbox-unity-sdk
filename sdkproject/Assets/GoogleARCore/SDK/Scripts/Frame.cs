//-----------------------------------------------------------------------
// <copyright file="Frame.cs" company="Google">
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
    using GoogleARCoreInternal;
    using UnityEngine;

    /// <summary>
    /// Provides a snapshot of the state of ARCore at a specific timestamp associated with the current frame.  Frame
    /// holds information about ARCore's state including tracking status, the pose of the camera relative to the world,
    /// estimated lighting parameters, and information on updates to objects (like Planes or Point Clouds) that ARCore
    /// is tracking.
    /// </summary>
    public class Frame
    {
        //// @cond EXCLUDE_FROM_DOXYGEN

        private static List<TrackableHit> s_TmpTrackableHitList = new List<TrackableHit>();

        //// @endcond

        /// <summary>
        /// Gets the pose of the ARCore device for the frame in Unity world coordinates.
        /// </summary>
        public static Pose Pose
        {
            get
            {
                var nativeSession = LifecycleManager.Instance.NativeSession;
                if (nativeSession == null)
                {
                    return Pose.identity;
                }

                var cameraHandle = nativeSession.FrameApi.AcquireCamera();
                Pose result = nativeSession.CameraApi.GetPose(cameraHandle);
                nativeSession.CameraApi.Release(cameraHandle);
                return result;
            }
        }

        /// <summary>
        /// Gets the current light estimate for this frame.
        /// </summary>
        public static LightEstimate LightEstimate
        {
            get
            {
                // TODO (b/73256094): Remove isTracking when fixed.
                var nativeSession = LifecycleManager.Instance.NativeSession;
                var isTracking = LifecycleManager.Instance.IsTracking;
                if (nativeSession == null || !isTracking)
                {
                    return new LightEstimate(LightEstimateState.NotValid, 0.0f, Color.black);
                }

                return nativeSession.FrameApi.GetLightEstimate();
            }
        }

        /// <summary>
        /// Performs a raycast against physical objects being tracked by ARCore.
        /// Output the closest hit from the camera.
        /// Note that the Unity's screen coordinate (0, 0)
        /// starts from bottom left.
        /// </summary>
        /// <param name="x">Horizontal touch position in Unity's screen coordiante.</param>
        /// <param name="y">Vertical touch position in Unity's screen coordiante.</param>
        /// <param name="filter">A filter bitmask where each {@link TrackableHitFlag} which is set represents a category
        /// of raycast hits the method call should consider valid.</param>
        /// <param name="hitResult">A {@link TrackableHit} that will be set if the raycast is successful.</param>
        /// <returns><c>true</c> if the raycast had a hit, otherwise <c>false</c>.</returns>
        public static bool Raycast(float x, float y, TrackableHitFlags filter,
            out TrackableHit hitResult)
        {
            hitResult = new TrackableHit();
            var nativeSession = LifecycleManager.Instance.NativeSession;
            if (nativeSession == null)
            {
                return false;
            }

            // Note that the Unity's screen coordinate (0, 0) starts from bottom left.
            bool foundHit = nativeSession.HitTestApi.Raycast(nativeSession.FrameHandle, x, Screen.height - y, filter,
                s_TmpTrackableHitList, true);

            if (foundHit && s_TmpTrackableHitList.Count != 0)
            {
                hitResult = s_TmpTrackableHitList[0];
            }

            return foundHit;
        }

        /// <summary>
        /// Performs a raycast against physical objects being tracked by ARCore.
        /// Output all hits from the camera.
        /// Note that the Unity's screen coordinate (0, 0)
        /// starts from bottom left.
        /// </summary>
        /// <param name="x">Horizontal touch position in Unity's screen coordiante.</param>
        /// <param name="y">Vertical touch position in Unity's screen coordiante.</param>
        /// <param name="filter">A filter bitmask where each {@link TrackableHitFlag} which is set represents a category
        /// of raycast hits the method call should consider valid.</param>
        /// <param name="hitResults">A list of {@link TrackableHit} that will be set if the raycast is successful.</param>
        /// <returns><c>true</c> if the raycast had a hit, otherwise <c>false</c>.</returns>
        public static bool RaycastAll(float x, float y, TrackableHitFlags filter, List<TrackableHit> hitResults)
        {
            hitResults.Clear();
            var nativeSession = LifecycleManager.Instance.NativeSession;
            if (nativeSession == null)
            {
                return false;
            }

            return nativeSession.HitTestApi.Raycast(nativeSession.FrameHandle, x, Screen.height - y, filter, hitResults, true);
        }

        /// <summary>
        /// Container for state related to the ARCore camera image metadata for the Frame.
        /// </summary>
        public static class CameraMetadata
        {
            /// <summary>
            /// Get camera image metadata value. The querying value type needs to match the returned type.
            /// The type could be checked in CameraMetadata.cs.
            /// </summary>
            /// <param name="metadataTag">Metadata type.</param>
            /// <param name="outMetadataList">Result list of the requested values.</param>
            /// <returns><c>true</c> if getting metadata value successfully, otherwise <c>false</c>.</returns>
            public static bool TryGetValues(CameraMetadataTag metadataTag, List<CameraMetadataValue> outMetadataList)
            {
                outMetadataList.Clear();
                var nativeSession = LifecycleManager.Instance.NativeSession;
                if (nativeSession == null)
                {
                    return false;
                }

                var metadataHandle = nativeSession.FrameApi.AcquireImageMetadata();
                var isSuccess = nativeSession.CameraMetadataApi.TryGetValues(metadataHandle, metadataTag, outMetadataList);
                nativeSession.CameraMetadataApi.Release(metadataHandle);
                return isSuccess;
            }

            /// <summary>
            /// Get all available tags in the current frame's metadata.
            /// </summary>
            /// <param name="outMetadataTags">Result list of the tags.</param>
            /// <returns><c>true</c> if getting tags successfully, otherwise <c>false</c>.</returns>
            public static bool GetAllCameraMetadataTags(List<CameraMetadataTag> outMetadataTags)
            {
                outMetadataTags.Clear();
                var nativeSession = LifecycleManager.Instance.NativeSession;
                if (nativeSession == null)
                {
                    return false;
                }

                var metadataHandle = nativeSession.FrameApi.AcquireImageMetadata();
                var isSuccess = nativeSession.CameraMetadataApi.GetAllCameraMetadataTags(metadataHandle,
                    outMetadataTags);
                nativeSession.CameraMetadataApi.Release(metadataHandle);
                return isSuccess;
            }
        }

        /// <summary>
        /// Container for state related to the ARCore point cloud for the Frame.
        /// </summary>
        public static class PointCloud
        {
            /// <summary>
            /// Gets a value indicating whether new point cloud data became available in the current frame.
            /// </summary>
            /// <returns><c>true</c> if new point cloud data became available in the current frame, otherwise
            /// <c>false</c>.</returns>
            public static bool IsUpdatedThisFrame
            {
                get
                {
                    var nativeSession = LifecycleManager.Instance.NativeSession;
                    if (nativeSession == null)
                    {
                        return false;
                    }

                    return nativeSession.IsPointCloudNew;
                }
            }

            /// <summary>
            /// Gets the count of point cloud points in the frame.
            /// </summary>
            public static int PointCount
            {
                get
                {
                    // TODO (b/73256094): Remove isTracking when fixed.
                    var nativeSession = LifecycleManager.Instance.NativeSession;
                    var isTracking = LifecycleManager.Instance.IsTracking;
                    if (nativeSession == null || !isTracking)
                    {
                        return 0;
                    }

                     return nativeSession.PointCloudApi.GetNumberOfPoints(nativeSession.PointCloudHandle);
                }
            }

            /// <summary>
            /// Gets a point from the point cloud at a given index.
            /// The point returned will be a Vector4 in the form <x,y,z,c> where the first three dimensions describe
            /// the position of the point in the world and the last represents a confidence estimation in the range [0, 1).
            /// </summary>
            /// <param name="index">The index of the point cloud point to get.</param>
            /// <returns>The point from the point cloud at <c>index</c> along with its confidence.</returns>
            public static Vector4 GetPoint(int index)
            {
                var nativeSession = LifecycleManager.Instance.NativeSession;
                if (nativeSession == null)
                {
                    return Vector4.zero;
                }

                return nativeSession.PointCloudApi.GetPoint(nativeSession.PointCloudHandle, index);
            }

            /// <summary>
            /// Copies the point cloud into the supplied parameter <c>points</c>.
            /// Each point will be a Vector4 in the form <x,y,z,c> where the first three dimensions describe the position
            /// of the point in the world and the last represents a confidence estimation in the range [0, 1).
            /// </summary>
            /// <param name="points">A list that will be filled with point cloud points by this method call.</param>
            public static void CopyPoints(List<Vector4> points)
            {
                points.Clear();
                var nativeSession = LifecycleManager.Instance.NativeSession;
                if (nativeSession == null)
                {
                    return;
                }

                nativeSession.PointCloudApi.CopyPoints(nativeSession.PointCloudHandle, points);
            }
        }

        /// <summary>
        /// Container for state related to the ARCore camera for the frame.
        /// </summary>
        public static class CameraImage
        {
            /// <summary>
            /// Gets a texture used from the device's rear camera.
            /// </summary>
            public static Texture Texture
            {
                get
                {
                    return ARCoreAndroidLifecycleManager.Instance.BackgroundTexture;
                }
            }

            /// <summary>
            /// Gets UVs that map the orienation and aspect ratio of <c>Frame.CameraImage.Texture</c> that of the
            /// device's display.
            /// </summary>
            public static DisplayUvCoords DisplayUvCoords
            {
                get
                {
                    ApiDisplayUvCoords displayUvCoords = new ApiDisplayUvCoords(new Vector2(0, 1),
                        new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0));

                    var nativeSession = LifecycleManager.Instance.NativeSession;
                    if (nativeSession == null || Texture == null)
                    {
                        return displayUvCoords.ToDisplayUvCoords();
                    }

                    nativeSession.FrameApi.TransformDisplayUvCoords(ref displayUvCoords);
                    return displayUvCoords.ToDisplayUvCoords();
                }
            }

            /// <summary>
            /// Attempts to acquire the camera image for CPU access.
            /// </summary>
            /// <returns>A <c>CameraImageBytes</c> struct with <c>IsAvailable</c> property set to <c>true</c> if
            /// successful and <c>false</c> if the image could not be acquired.</returns>
            public static GoogleARCore.CameraImageBytes AcquireCameraImageBytes()
            {
                var nativeSession = LifecycleManager.Instance.NativeSession;
                if (nativeSession == null)
                {
                    return new CameraImageBytes(IntPtr.Zero);
                }

                return nativeSession.FrameApi.AcquireCameraImageBytes();
            }

            /// <summary>
            /// Gets the projection matrix for the frame.
            /// </summary>
            /// <param name="nearClipping">The near clipping plane for the projection matrix.</param>
            /// <param name="farClipping">The far clipping plane for the projection matrix.</param>
            /// <returns>The projection matrix for the frame.</returns>
            public static Matrix4x4 GetCameraProjectionMatrix(float nearClipping, float farClipping)
            {
                var nativeSession = LifecycleManager.Instance.NativeSession;
                if (nativeSession == null || Texture == null)
                {
                    return Matrix4x4.identity;
                }

                var cameraHandle = nativeSession.FrameApi.AcquireCamera();
                var result = nativeSession.CameraApi.GetProjectionMatrix(cameraHandle, nearClipping, farClipping);
                nativeSession.CameraApi.Release(cameraHandle);
                return result;
            }
        }
    }
}
