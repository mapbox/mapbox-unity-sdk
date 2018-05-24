//-----------------------------------------------------------------------
// <copyright file="ARKitHelper.cs" company="Google">
//
// Copyright 2018 Google Inc. All Rights Reserved.
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

namespace GoogleARCore.Examples.CloudAnchor
{
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_IOS
    using UnityEngine.XR.iOS;
    using UnityARUserAnchorComponent = UnityEngine.XR.iOS.UnityARUserAnchorComponent;
#else
    using UnityARUserAnchorComponent = UnityEngine.Component;
#endif

    /// <summary>
    /// A helper class to interact with the ARKit plugin.
    /// </summary>
    public class ARKitHelper
    {
#if UNITY_IOS
        private List<ARHitTestResult> m_HitResultList = new List<ARHitTestResult>();
#endif

        /// <summary>
        /// Performs a Raycast against a plane.
        /// </summary>
        /// <param name="camera">The AR camera being used.</param>
        /// <param name="x">The x screen position.</param>
        /// <param name="y">The y screen position.</param>
        /// <param name="hitPose">The resulting hit pose if the method returns <c>true</c>.</param>
        /// <returns><c>true</c> if a plane was hit. Otherwise <c>false</c>.</returns>
        public bool RaycastPlane(Camera camera, float x, float y, out Pose hitPose)
        {
            hitPose = new Pose();
#if UNITY_IOS
            var session = UnityARSessionNativeInterface.GetARSessionNativeInterface();

            var viewportPoint = camera.ScreenToViewportPoint(new Vector2(x, y));
            ARPoint arPoint = new ARPoint
            {
                x = viewportPoint.x,
                y = viewportPoint.y
            };

            session.HitTest(arPoint, ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent, m_HitResultList);
            if (m_HitResultList.Count > 0)
            {
                int minDistanceIndex = 0;
                for (int i = 1; i < m_HitResultList.Count; i++)
                {
                    if (m_HitResultList[i].distance < m_HitResultList[minDistanceIndex].distance)
                    {
                        minDistanceIndex = i;
                    }
                }

                hitPose.position = UnityARMatrixOps.GetPosition(m_HitResultList[minDistanceIndex].worldTransform);

                // Point the hitPose rotation roughly away from the raycast/camera to match ARCore.
                hitPose.rotation.eulerAngles = new Vector3(0.0f, camera.transform.eulerAngles.y, 0.0f);

                return true;
            }
#endif
            return false;
        }

        /// <summary>
        /// Creates an ARKit user anchor.
        /// </summary>
        /// <param name="pose">The pose for the new anchor.</param>
        /// <returns>A newly created ARKit anchor.</returns>
        public UnityARUserAnchorComponent CreateAnchor(Pose pose)
        {
            var anchorGO = new GameObject("User Anchor");
            var anchor = anchorGO.AddComponent<UnityARUserAnchorComponent>();
            anchorGO.transform.position = pose.position;
            anchorGO.transform.rotation = pose.rotation;
            return anchor;
        }
    }
}
