using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using GoogleARCore;
using GoogleARCoreInternal;
using ARCoreNative = GoogleAR.UnityNative;

namespace UnityARInterface
{
    public class ARCoreInterface : ARInterface
    {
        private List<TrackedPlane> m_TrackedPlaneBuffer = new List<TrackedPlane>();
        private System.Nullable<float> m_HorizontalFov;
        private System.Nullable<float> m_VerticalFov;
        private ScreenOrientation m_CachedScreenOrientation;
        private Dictionary<TrackedPlane, BoundedPlane> m_TrackedPlanes = new Dictionary<TrackedPlane, BoundedPlane>();
        private SessionComponent m_Session;
        private Matrix4x4 m_DisplayTransform = Matrix4x4.identity;

        public override bool StartService(Settings settings)
        {
            if (m_Session == null)
            {
                var sessionConfig = ScriptableObject.CreateInstance<SessionConfig>();

                // We're going to manage the camera separately, but this needs to be true
                // so the ARCore native API knows we're going to use the camera.
                sessionConfig.m_enableARBackground = true;
                sessionConfig.m_enablePlaneFinding = settings.enablePlaneDetection;
                sessionConfig.m_enablePointcloud = settings.enablePointCloud;

                var gameObject = new GameObject("Session Manager");

                // Deactivate the GameObject before adding the SessionComponent
                // or else the Awake method will be called before we have set
                // the session config.
                gameObject.SetActive(false);
                m_Session = gameObject.AddComponent<SessionComponent>();
                m_Session.m_connectOnAwake = false;
                m_Session.m_arSessionConfig = sessionConfig;

                // We are going to manage the camera separately, so
                // intentionally leave null.
                m_Session.m_firstPersonCamera = null;
                gameObject.SetActive(true);
            }

            m_Session.Connect();
            return SessionManager.ConnectionState == SessionConnectionState.Connected;
        }

        public override void StopService()
        {
            // Not implemented on ARCore.
            return;
        }

        public override bool TryGetUnscaledPose(ref Pose pose)
        {
            if (Frame.TrackingState != FrameTrackingState.Tracking)
                return false;

            ARCoreNative.PoseData poseData;

            bool getPoseSuccess = ARCoreNative.InputTracking.TryGetPoseAtTime(
                out poseData,
                ARCoreNative.CoordinateFrame.StartOfService,
                ARCoreNative.CoordinateFrame.CameraColor);

            if (getPoseSuccess && (poseData.statusCode == ARCoreNative.PoseStatus.Valid))
            {
                pose.position = poseData.position;
                pose.rotation = poseData.rotation;
                return true;
            }

            return false;
        }

        public override bool TryGetCameraImage(ref CameraImage cameraImage)
        {
            ARCoreNative.NativeImage nativeImage = new ARCoreNative.NativeImage();
            if (ARCoreNative.Device.TryAcquireLatestImageBuffer(ref nativeImage))
            {
                cameraImage.width = (int)nativeImage.width;
                cameraImage.height = (int)nativeImage.height;

                var planeInfos = nativeImage.planeInfos;

                // The Y plane is always the first one.
                var yOffset = planeInfos[0].offset;
                var numYBytes = planeInfos[0].size;
                IntPtr yPlaneStart = new IntPtr(nativeImage.planeData.ToInt64() + yOffset);

                if (cameraImage.y == null || cameraImage.y.Length != numYBytes)
                    cameraImage.y = new byte[numYBytes];

                Marshal.Copy(yPlaneStart, cameraImage.y, 0, (int)numYBytes);

                // UV planes are not deterministic, but we want all the data in one go
                // so the offset will be the min of the two planes.
                int uvOffset = Mathf.Min(
                    (int)nativeImage.planeInfos[1].offset,
                    (int)nativeImage.planeInfos[2].offset);

                // Find the end of the uv plane data
                int uvDataEnd = 0;
                for (int i = 1; i < planeInfos.Count; ++i)
                {
                    uvDataEnd = Mathf.Max(uvDataEnd, (int)planeInfos[i].offset + planeInfos[i].size);
                }

                // Finally, compute the number of bytes by subtracting the end from the beginning
                var numUVBytes = uvDataEnd - uvOffset;
                IntPtr uvPlaneStart = new IntPtr(nativeImage.planeData.ToInt64() + uvOffset);

                if (cameraImage.uv == null || cameraImage.uv.Length != numUVBytes)
                    cameraImage.uv = new byte[numUVBytes];

                Marshal.Copy(uvPlaneStart, cameraImage.uv, 0, (int)numUVBytes);

                ARCoreNative.Device.ReleaseImageBuffer(nativeImage);

                // The data is usually provided as VU rather than UV,
                // so we need to swap the bytes.
                // There's no way to know this currently, but it's always
                // been this way on every device so far.
                for (int i = 1; i < numUVBytes; i += 2)
                {
                    var b = cameraImage.uv[i - 1];
                    cameraImage.uv[i - 1] = cameraImage.uv[i];
                    cameraImage.uv[i] = b;
                }

                return true;
            }

            return false;
        }

        public override bool TryGetPointCloud(ref PointCloud pointCloud)
        {
            // Fill in the data to draw the point cloud.
            GoogleARCore.PointCloud nativePointCloud = Frame.PointCloud;
            if (nativePointCloud.PointCount == 0)
                return false;

            if (pointCloud.points == null)
                pointCloud.points = new List<Vector3>();

            pointCloud.points.Clear();

            for (int i = 0; i < nativePointCloud.PointCount; i++)
            {
                pointCloud.points.Add(nativePointCloud.GetPoint(i));
            }

            return true;
        }

        public override LightEstimate GetLightEstimate()
        {
            if (SessionManager.ConnectionState == SessionConnectionState.Connected)
            {
                return new LightEstimate()
                {
                    capabilities = LightEstimateCapabilities.AmbientIntensity,
                    ambientIntensity = Frame.LightEstimate.PixelIntensity
                };
            }
            else
            {
                // Zero initialized means capabilities will be None
                return new LightEstimate();
            }
        }

		public override Matrix4x4 GetDisplayTransform()
		{
			return m_DisplayTransform;
		}

        private void CalculateDisplayTransform()
        {
            var cosTheta = 1f;
            var sinTheta = 0f;

            switch (Screen.orientation)
            {
                case ScreenOrientation.Portrait:
                    cosTheta = 0f;
                    sinTheta = -1f;
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                    cosTheta = 0f;
                    sinTheta = 1f;
                    break;
                case ScreenOrientation.LandscapeLeft:
                    cosTheta = 1f;
                    sinTheta = 0f;
                    break;
                case ScreenOrientation.LandscapeRight:
                    cosTheta = -1f;
                    sinTheta = 0f;
                    break;
            }

            m_DisplayTransform.m00 = cosTheta;
            m_DisplayTransform.m01 = sinTheta;
            m_DisplayTransform.m10 = sinTheta;
            m_DisplayTransform.m11 = -cosTheta;
        }

        public override void SetupCamera(Camera camera)
        {
            var backgroundRender = new ARBackgroundRenderer();
            backgroundRender.backgroundMaterial =
                Resources.Load("Materials/ARBackground", typeof(Material)) as Material;
            backgroundRender.mode = ARRenderMode.MaterialAsBackground;
            backgroundRender.camera = camera;

            ARCoreNative.Device.backgroundRenderer = backgroundRender;
        }

        public override void UpdateCamera(Camera camera)
        {
            if (Screen.orientation == m_CachedScreenOrientation)
                return;

            CalculateDisplayTransform();

            m_CachedScreenOrientation = Screen.orientation;

            if (m_CachedScreenOrientation == ScreenOrientation.Portrait ||
                m_CachedScreenOrientation == ScreenOrientation.PortraitUpsideDown)
            {
                if (m_HorizontalFov.HasValue)
                {
                    camera.fieldOfView = m_HorizontalFov.Value;
                }
                else
                {
                    float fieldOfView;
                    if (ARCoreNative.Device.TryGetHorizontalFov(out fieldOfView))
                    {
                        m_HorizontalFov = fieldOfView;
                        camera.fieldOfView = fieldOfView;
                    }
                }
            }
            else
            {
                if (m_VerticalFov.HasValue)
                {
                    camera.fieldOfView = m_VerticalFov.Value;
                }
                else
                {
                    float fieldOfView;
                    if (ARCoreNative.Device.TryGetVerticalFov(out fieldOfView))
                    {
                        m_VerticalFov = fieldOfView;
                        camera.fieldOfView = fieldOfView;
                    }
                }
            }
        }

        public override void Update()
        {
            SessionManager.Instance.EarlyUpdate();

            if (Frame.TrackingState != FrameTrackingState.Tracking)
                return;

            Frame.GetAllPlanes(ref m_TrackedPlaneBuffer);

            foreach (var trackedPlane in m_TrackedPlaneBuffer)
            {
                if (trackedPlane.IsUpdated)
                {
                    BoundedPlane boundedPlane;
                    if (m_TrackedPlanes.TryGetValue(trackedPlane, out boundedPlane))
                    {
                        if (trackedPlane.SubsumedBy == null)
                        {
                            OnPlaneUpdated(boundedPlane);
                        }
                        else
                        {
                            OnPlaneRemoved(boundedPlane);
                            m_TrackedPlanes.Remove(trackedPlane);
                        }
                    }
                    else
                    {
                        boundedPlane = new BoundedPlane()
                        {
                            id = Guid.NewGuid().ToString(),
                            center = trackedPlane.Position,
                            rotation = trackedPlane.Rotation,
                            extents = trackedPlane.Bounds
                        };

                        m_TrackedPlanes.Add(trackedPlane, boundedPlane);
                        OnPlaneAdded(boundedPlane);
                    }
                }
            }

            // Check for planes that were removed from the tracked plane list
            List<TrackedPlane> planesToRemove = new List<TrackedPlane>();
            foreach (var kvp in m_TrackedPlanes)
            {
                var trackedPlane = kvp.Key;

                if (!m_TrackedPlaneBuffer.Exists(x => x == trackedPlane))
                {
                    OnPlaneRemoved(kvp.Value);

                    // Add to list here to avoid mutating the dictionary
                    // while iterating over it.
                    planesToRemove.Add(trackedPlane);
                }
            }

            foreach (var plane in planesToRemove)
            {
                m_TrackedPlanes.Remove(plane);
            }
        }
    }
}
