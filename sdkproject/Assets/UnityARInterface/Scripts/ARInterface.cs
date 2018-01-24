using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
    public abstract class ARInterface
    {
        public struct Settings
        {
            public bool enablePointCloud;
            public bool enablePlaneDetection;
            public bool enableLightEstimation;
        }

        public struct CameraImage
        {
            public byte[] y;
            public byte[] uv;
            public int width;
            public int height;
        }

        public struct PointCloud
        {
            public List<Vector3> points;
        }

        [Flags]
        public enum LightEstimateCapabilities
        {
            None = 0,
            AmbientIntensity = 1 << 0,
            AmbientColorTemperature = 1 << 1,
        }

        public struct LightEstimate
        {
            public LightEstimateCapabilities capabilities;
            public float ambientIntensity;
            public float ambientColorTemperature;
        };

        public static Action<BoundedPlane> planeAdded;
        public static Action<BoundedPlane> planeUpdated;
        public static Action<BoundedPlane> planeRemoved;

        public virtual bool IsRunning { get; protected set; } 

        public virtual bool IsSupported { get { return true; } }

        public abstract IEnumerator StartService(Settings settings);

        public abstract void StopService();

        public bool TryGetPose(ref Pose pose)
        {
            return TryGetUnscaledPose(ref pose);
        }

        public abstract void SetupCamera(Camera camera);

        // This is to perform any per-frame updates for the camera,
        // e.g. FOV or projection matrix. Not for setting position.
        public abstract void UpdateCamera(Camera camera);

        public abstract void Update();

        public abstract bool TryGetUnscaledPose(ref Pose pose);

        public abstract bool TryGetCameraImage(ref CameraImage cameraImage);

        public abstract bool TryGetPointCloud(ref PointCloud pointCloud);

        public abstract LightEstimate GetLightEstimate();

		public abstract Matrix4x4 GetDisplayTransform ();

        protected void OnPlaneAdded(BoundedPlane plane)
        {
            if (planeAdded != null)
                planeAdded(plane);
        }

        protected void OnPlaneUpdated(BoundedPlane plane)
        {
            if (planeUpdated != null)
                planeUpdated(plane);
        }

        protected void OnPlaneRemoved(BoundedPlane plane)
        {
            if (planeRemoved != null)
                planeRemoved(plane);
        }

        public virtual void ApplyAnchor(ARAnchor arAnchor)
        {
        }

        public virtual void DestroyAnchor(ARAnchor arAnchor)
        {
        }

        private static ARInterface m_Interface;

        public static ARInterface GetInterface()
        {
            if (m_Interface == null)
            {
#if UNITY_EDITOR
                m_Interface = new AREditorInterface();
#elif UNITY_IOS
                m_Interface = new ARKitInterface();
#elif UNITY_ANDROID
                m_Interface = new ARCoreInterface();
#endif
            }

            return m_Interface;
        }

        public static void SetInterface(ARInterface arInterface)
        {
            m_Interface = arInterface;
        }
    }
}