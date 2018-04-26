using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.XR;

namespace UnityARInterface
{
    public class ARCoreInterface : ARInterface
    {
       
#region ARCoreCameraAPI
        public const string ARCoreCameraUtilityAPI = "arcore_camera_utility";

        //Texture size. Larger values are slower.
        private const int k_ARCoreTextureWidth = 640;
        private const int k_ARCoreTextureHeight = 480;

        //If imageFormatType is set to ImageFormatColor, the buffer is converted to YUV2.
        //If imageFormatType is set to ImageFormatGrayscale, the buffer is set to Y as is, 
        //while the UV components remain null. Will appear pink using the remote
        private const ImageFormatType k_ImageFormatType = ImageFormatType.ImageFormatColor;

        [DllImport(ARCoreCameraUtilityAPI)]
        public static extern void TextureReader_create(int format, int width, int height, bool keepAspectRatio);

        [DllImport(ARCoreCameraUtilityAPI)]
        public static extern void TextureReader_destroy();

        [DllImport(ARCoreCameraUtilityAPI)]
        public static extern IntPtr TextureReader_submitAndAcquire(
            int textureId, int textureWidth, int textureHeight, ref int bufferSize);

        private enum ImageFormatType
        {
            ImageFormatColor = 0,
            ImageFormatGrayscale = 1
        }

        private byte[] pixelBuffer;

#endregion

        private List<TrackedPlane> m_TrackedPlaneBuffer = new List<TrackedPlane>();
        private ScreenOrientation m_CachedScreenOrientation;
        private Dictionary<TrackedPlane, BoundedPlane> m_TrackedPlanes = new Dictionary<TrackedPlane, BoundedPlane>();
        private ARCoreSession m_ARCoreSession;
        private ARCoreSessionConfig m_ARCoreSessionConfig;
        private ARBackgroundRenderer m_BackgroundRenderer;
        private Matrix4x4 m_DisplayTransform = Matrix4x4.identity;
        private List<Vector4> m_TempPointCloud = new List<Vector4>();
        private Dictionary<ARAnchor, Anchor> m_Anchors = new Dictionary<ARAnchor, Anchor>();
        private bool m_BackgroundRendering;

        public override bool IsSupported
        {
            get
            {
                return
                    Session.Status != SessionStatus.ErrorApkNotAvailable &&
                    Session.Status != SessionStatus.ErrorSessionConfigurationNotSupported;
            }
        }

        public override bool BackgroundRendering
        {
            get
            {
                return m_BackgroundRendering;
            }
            set
            {
                if (m_BackgroundRenderer == null)
                    return;

                m_BackgroundRendering = value;
                m_BackgroundRenderer.mode = m_BackgroundRendering ? 
                    ARRenderMode.MaterialAsBackground : ARRenderMode.StandardBackground;
            }
        }

        public override IEnumerator StartService(Settings settings)
        {
            if (m_ARCoreSessionConfig == null)
                m_ARCoreSessionConfig = ScriptableObject.CreateInstance<ARCoreSessionConfig>();

            m_ARCoreSessionConfig.EnableLightEstimation = settings.enableLightEstimation;
            m_ARCoreSessionConfig.EnablePlaneFinding = settings.enablePlaneDetection;
            //Do we want to match framerate to the camera?
            m_ARCoreSessionConfig.MatchCameraFramerate = true;

            // Create a GameObject on which the session component will live.
            if (m_ARCoreSession == null)
            {
                var go = new GameObject("ARCore Session");
                go.SetActive(false);
                m_ARCoreSession = go.AddComponent<ARCoreSession>();
                m_ARCoreSession.SessionConfig = m_ARCoreSessionConfig;
                go.SetActive(true);
            }

            // Enabling the session triggers the connection
            m_ARCoreSession.SessionConfig = m_ARCoreSessionConfig;
            m_ARCoreSession.enabled = true;

            if (!IsSupported)
            {
                switch (Session.Status)
                {
                    case SessionStatus.ErrorApkNotAvailable:
                        Debug.LogError("ARCore APK is not installed");
                        yield break;
                    case SessionStatus.ErrorPermissionNotGranted:
                        Debug.LogError("A needed permission (likely the camera) has not been granted");
                        yield break;
                    case SessionStatus.ErrorSessionConfigurationNotSupported:
                        Debug.LogError("The given ARCore session configuration is not supported on this device");
                        yield break;
                    case SessionStatus.FatalError:
                        Debug.LogError("A fatal error was encountered trying to start the ARCore session");
                        yield break;
                }
            }

            while (!Session.Status.IsValid())
            {
                IsRunning = false;

                if (Session.Status.IsError())
                {
                    switch (Session.Status)
                    {
                        case SessionStatus.ErrorPermissionNotGranted:
                            Debug.LogError("A needed permission (likely the camera) has not been granted");
                            yield break;
                        case SessionStatus.FatalError:
                            Debug.LogError("A fatal error was encountered trying to start the ARCore session");
                            yield break;
                    }
                }

                yield return null;
            }

            // If we make it out of the while loop, then the session is initialized and valid
            IsRunning = true;

            if (IsRunning)
                TextureReader_create((int)k_ImageFormatType, k_ARCoreTextureWidth, k_ARCoreTextureHeight, true);

        }

        public override void StopService()
        {
            var anchors = m_Anchors.Keys;
            foreach (var anchor in anchors)
            {
                DestroyAnchor(anchor);
            }

            m_ARCoreSession.enabled = false;
            TextureReader_destroy();
            BackgroundRendering = false;
            m_BackgroundRenderer.backgroundMaterial = null;
            m_BackgroundRenderer.camera = null;
            m_BackgroundRenderer = null;
            IsRunning = false;
        }

        public override bool TryGetUnscaledPose(ref Pose pose)
        {
            if (Session.Status != SessionStatus.Tracking)
                return false;

            pose.position = Frame.Pose.position;
            pose.rotation = Frame.Pose.rotation;
            return true;
        }

        public override bool TryGetCameraImage(ref CameraImage cameraImage)
        {
            if (Session.Status != SessionStatus.Tracking)
                return false;

            if (Frame.CameraImage.Texture == null || Frame.CameraImage.Texture.GetNativeTexturePtr() == IntPtr.Zero)
                return false;

            //This is a GL texture ID
            int textureId = Frame.CameraImage.Texture.GetNativeTexturePtr().ToInt32();
            int bufferSize = 0;
            //Ask the native plugin to start reading the image of the current frame, 
            //and return the image read from the privous frame
            IntPtr bufferPtr = TextureReader_submitAndAcquire(textureId, k_ARCoreTextureWidth, k_ARCoreTextureHeight, ref bufferSize);

            //I think this is needed because of this bug
            //https://github.com/google-ar/arcore-unity-sdk/issues/66
            GL.InvalidateState();

            if (bufferPtr == IntPtr.Zero || bufferSize == 0)
                return false;

            if (pixelBuffer == null || pixelBuffer.Length != bufferSize)
                pixelBuffer = new byte[bufferSize];

            //Copy buffer
            Marshal.Copy(bufferPtr, pixelBuffer, 0, bufferSize);

            //Convert to YUV data
            PixelBuffertoYUV2(pixelBuffer ,k_ARCoreTextureWidth, k_ARCoreTextureHeight, 
                              k_ImageFormatType, ref cameraImage.y, ref cameraImage.uv);

            cameraImage.width = k_ARCoreTextureWidth;
            cameraImage.height = k_ARCoreTextureHeight;

            return true;
        }


        private void PixelBuffertoYUV2(byte[] rgba, int width, int height,ImageFormatType imageFormatType ,ref byte[] y, ref byte[] uv)
        {
            //in grayscale, it's a byte per pixel, which we can just assign to Y, and leave uv null
            //Probably not the most accurate conversion, would save some performance
            if (imageFormatType == ImageFormatType.ImageFormatGrayscale)
            {
                y = rgba;
                uv = null;
                return;
            }

            int pixelCount = width * height;

            if (y == null || y.Length != pixelCount)
                y = new byte[pixelCount];

            if (uv == null || uv.Length != pixelCount / 2)
                uv = new byte[pixelCount / 2];

            int iY = 0;
            int iUV = 0;
            int iRGBA = 0;

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    //Random magic starts here!
                    //Convert every pixel to 1 byte Y
                    y[iY++] = (byte)(((66 * rgba[iRGBA] + 129 * rgba[iRGBA + 1] + 25 * rgba[iRGBA + 2] + 128) >> 8) + 16);
                    //Convert every pixel to 2 bytes UV at quarter resolution
                    if (row % 2 == 0 && column % 2 == 0)
                    {
                        uv[iUV++] = (byte)(((-38 * rgba[iRGBA] - 74 * rgba[iRGBA + 1] + 112 * rgba[iRGBA + 2] + 128) >> 8) + 128);
                        uv[iUV++] = (byte)(((112 * rgba[iRGBA] - 94 * rgba[iRGBA + 1] - 18 * rgba[iRGBA + 2] + 128) >> 8) + 128);
                    }
                    //To next pixel
                    iRGBA += 4;
                }
            }
        }

        public override bool TryGetPointCloud(ref PointCloud pointCloud)
        {
            if (Session.Status != SessionStatus.Tracking)
                return false;

            // Fill in the data to draw the point cloud.
            m_TempPointCloud.Clear();
            Frame.PointCloud.CopyPoints(m_TempPointCloud);

            if (m_TempPointCloud.Count == 0)
                return false;

            if (pointCloud.points == null)
                pointCloud.points = new List<Vector3>();

            pointCloud.points.Clear();
            foreach (Vector3 point in m_TempPointCloud)
                pointCloud.points.Add(point);

            return true;
        }

        public override LightEstimate GetLightEstimate()
        {
            if (Session.Status.IsValid() && Frame.LightEstimate.State == LightEstimateState.Valid)
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
            ARCoreBackgroundRenderer backgroundRenderer =
                camera.GetComponent<ARCoreBackgroundRenderer>();

            if (backgroundRenderer == null)
            {
                camera.gameObject.SetActive(false);
                backgroundRenderer = camera.gameObject.AddComponent<ARCoreBackgroundRenderer>();
                backgroundRenderer.BackgroundMaterial = Resources.Load("Materials/ARBackground", typeof(Material)) as Material;
                camera.gameObject.SetActive(true);
            }
        }

        public override void UpdateCamera(Camera camera)
        {
            // This is handled for us by the ARCoreBackgroundRenderer
        }

        private bool PlaneUpdated(TrackedPlane tp, BoundedPlane bp)
        {
            var tpExtents = new Vector2(tp.ExtentX, tp.ExtentZ);
            var extents = Vector2.Distance(tpExtents, bp.extents) > 0.005f;
            var rotation = tp.CenterPose.rotation != bp.rotation;
            var position = Vector2.Distance(tp.CenterPose.position, bp.center) > 0.005f;
            return (extents || rotation || position);
        }

        public override void Update()
        {
            if (m_ARCoreSession == null)
                return;

            AsyncTask.OnUpdate();

            if (Session.Status != SessionStatus.Tracking)
                return;

            if(m_ARCoreSessionConfig.EnablePlaneFinding)
            {
                Session.GetTrackables<TrackedPlane>(m_TrackedPlaneBuffer, TrackableQueryFilter.All);
                foreach (var trackedPlane in m_TrackedPlaneBuffer)
                {
                    BoundedPlane boundedPlane;
                    if (m_TrackedPlanes.TryGetValue(trackedPlane, out boundedPlane))
                    {
                        // remove any subsumed planes
                        if (trackedPlane.SubsumedBy != null)
                        {
                            OnPlaneRemoved(boundedPlane);
                            m_TrackedPlanes.Remove(trackedPlane);
                        }
                        // update any planes with changed extents
                        else if (PlaneUpdated(trackedPlane, boundedPlane))
                        {
                            boundedPlane.center = trackedPlane.CenterPose.position;
                            boundedPlane.rotation = trackedPlane.CenterPose.rotation;
                            boundedPlane.extents.x = trackedPlane.ExtentX;
                            boundedPlane.extents.y = trackedPlane.ExtentZ;
                            m_TrackedPlanes[trackedPlane] = boundedPlane;
                            OnPlaneUpdated(boundedPlane);
                        }
                    }
                    // add any new planes
                    else
                    {
                        boundedPlane = new BoundedPlane()
                        {
                            id = Guid.NewGuid().ToString(),
                            center = trackedPlane.CenterPose.position,
                            rotation = trackedPlane.CenterPose.rotation,
                            extents = new Vector2(trackedPlane.ExtentX, trackedPlane.ExtentZ)
                        };

                        m_TrackedPlanes.Add(trackedPlane, boundedPlane);
                        OnPlaneAdded(boundedPlane);
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
                        planesToRemove.Add(trackedPlane);
                    }
                }

                foreach (var plane in planesToRemove)
                    m_TrackedPlanes.Remove(plane);

            }

            //Update Anchors
            foreach(var anchor in m_Anchors){
                anchor.Key.transform.position = anchor.Value.transform.position;
                anchor.Key.transform.rotation = anchor.Value.transform.rotation;
            }
        }

        public override void ApplyAnchor(ARAnchor arAnchor)
        {
            if (!IsRunning)
                return;
            //Since ARCore wants to create it's own GameObject, we can keep a reference to it and copy its Pose.
            //Not the best, but probably will change when ARCore releases.
            Anchor arCoreAnchor = Session.CreateAnchor(new Pose(arAnchor.transform.position, arAnchor.transform.rotation));
            arAnchor.anchorID = Guid.NewGuid().ToString();
            m_Anchors[arAnchor] = arCoreAnchor;
        }

        public override void DestroyAnchor(ARAnchor arAnchor)
        {
            if (!string.IsNullOrEmpty(arAnchor.anchorID))
            {
                Anchor arCoreAnchor;
                if(m_Anchors.TryGetValue(arAnchor, out arCoreAnchor)){
                    UnityEngine.Object.Destroy(arCoreAnchor);
                    m_Anchors.Remove(arAnchor);
                }

                arAnchor.anchorID = null;

            }
        }
    }
}
