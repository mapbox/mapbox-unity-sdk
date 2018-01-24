using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using GoogleARCoreInternal;
using System.Collections;
using System.Runtime.InteropServices;

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
        private SessionManager m_SessionManager;
        private ARCoreSessionConfig m_ARCoreSessionConfig;
        private ARCoreBackgroundRenderer m_BackgroundRenderer;
        private Matrix4x4 m_DisplayTransform = Matrix4x4.identity;
        private List<Vector4> m_TempPointCloud = new List<Vector4>();
        private Dictionary<ARAnchor, Anchor> m_Anchors = new Dictionary<ARAnchor, Anchor>();

        public override bool IsSupported
        {
            get
            {
                if (m_SessionManager == null)
                    m_SessionManager = SessionManager.CreateSession();

                if (m_ARCoreSessionConfig == null)
                    m_ARCoreSessionConfig = ScriptableObject.CreateInstance<ARCoreSessionConfig>();

                return m_SessionManager.CheckSupported((m_ARCoreSessionConfig));
            }
        }

        public override IEnumerator StartService(Settings settings)
        {
            if (m_ARCoreSessionConfig == null)
                m_ARCoreSessionConfig = ScriptableObject.CreateInstance<ARCoreSessionConfig>();

            m_ARCoreSessionConfig.EnableLightEstimation = settings.enableLightEstimation;
            m_ARCoreSessionConfig.EnablePlaneFinding = settings.enablePlaneDetection;
            //Do we want to match framerate to the camera?
            m_ARCoreSessionConfig.MatchCameraFramerate = false;

            //Using the SessionManager instead of ARCoreSession allows us to check if the config is supported,
            //And also using the session without the need for a GameObject or an additional MonoBehaviour.
            if (m_SessionManager == null)
            {
                m_SessionManager = SessionManager.CreateSession();
                if (!IsSupported){
                    ARDebug.LogError("The requested ARCore session configuration is not supported.");
                    yield break;
                }

                Session.Initialize(m_SessionManager);

                if (Session.ConnectionState != SessionConnectionState.Uninitialized)
                {
                    ARDebug.LogError("Could not create an ARCore session.  The current Unity Editor may not support this " +
                        "version of ARCore.");
                    yield break;
                }
            }
            //We ask for permission to use the camera and wait
            var task = AskForPermissionAndConnect(m_ARCoreSessionConfig);
            yield return task.WaitForCompletion();
            //After the operation is done, we double check if the connection was successful
            IsRunning = task.Result == SessionConnectionState.Connected;

            if (IsRunning)
                TextureReader_create((int)k_ImageFormatType, k_ARCoreTextureWidth, k_ARCoreTextureHeight, true);

        }

        //Checks if we can establish a connection, and ask for permission
        private AsyncTask<SessionConnectionState> AskForPermissionAndConnect(ARCoreSessionConfig sessionConfig)
        {
            const string androidCameraPermissionName = "android.permission.CAMERA";

            if (m_SessionManager == null)
            {
                ARDebug.LogError("Cannot connect because ARCoreSession failed to initialize.");
                return new AsyncTask<SessionConnectionState>(SessionConnectionState.Uninitialized);
            }

            if (sessionConfig == null)
            {
                ARDebug.LogError("Unable to connect ARSession session due to missing ARSessionConfig.");
                m_SessionManager.ConnectionState = SessionConnectionState.MissingConfiguration;
                return new AsyncTask<SessionConnectionState>(Session.ConnectionState);
            }

            // We have already connected at least once.
            if (Session.ConnectionState != SessionConnectionState.Uninitialized)
            {
                ARDebug.LogError("Multiple attempts to connect to the ARSession.  Note that the ARSession connection " +
                    "spans the lifetime of the application and cannot be reconfigured.  This will change in future " +
                    "versions of ARCore.");
                return new AsyncTask<SessionConnectionState>(Session.ConnectionState);
            }

            // Create an asynchronous task for the potential permissions flow and service connection.
            Action<SessionConnectionState> onTaskComplete;
            var returnTask = new AsyncTask<SessionConnectionState>(out onTaskComplete);
            returnTask.ThenAction((connectionState) =>
            {
                m_SessionManager.ConnectionState = connectionState;
            });

            // Attempt service connection immediately if permissions are granted.
            if (AndroidPermissionsManager.IsPermissionGranted(androidCameraPermissionName))
            {
                Connect(sessionConfig, onTaskComplete);
                return returnTask;
            }

            // Request needed permissions and attempt service connection if granted.
            AndroidPermissionsManager.RequestPermission(androidCameraPermissionName).ThenAction((requestResult) =>
            {
                if (requestResult.IsAllGranted)
                {
                    Connect(sessionConfig, onTaskComplete);
                }
                else
                {
                    ARDebug.LogError("ARCore connection failed because a needed permission was rejected.");
                    onTaskComplete(SessionConnectionState.UserRejectedNeededPermission);
                }
            });

            return returnTask;
        }

        //Connect is called once the permission to use the camera is granted.
        private void Connect(ARCoreSessionConfig sessionConfig, Action<SessionConnectionState> onComplete)
        {
            if (!m_SessionManager.CheckSupported(sessionConfig))
            {
                ARDebug.LogError("The requested ARCore session configuration is not supported.");
                onComplete(SessionConnectionState.InvalidConfiguration);
                return;
            }

            if (!m_SessionManager.SetConfiguration(sessionConfig))
            {
                ARDebug.LogError("ARCore connection failed because the current configuration is not supported.");
                onComplete(SessionConnectionState.InvalidConfiguration);
                return;
            }

            Frame.Initialize(m_SessionManager.FrameManager);

            // ArSession_resume needs to be called in the UI thread due to b/69682628.
            AsyncTask.PerformActionInUIThread(() =>
            {
                if (!m_SessionManager.Resume())
                {
                    onComplete(SessionConnectionState.ConnectToServiceFailed);
                }
                else
                {
                    onComplete(SessionConnectionState.Connected);
                }
            });
        }

        public override void StopService()
        {
            var anchors = m_Anchors.Keys;
            foreach (var anchor in anchors)
            {
                DestroyAnchor(anchor);
            }
            Frame.Destroy();
            Session.Destroy();
            TextureReader_destroy();
            IsRunning = false;
        }

        public override bool TryGetUnscaledPose(ref Pose pose)
        {
            if (Frame.TrackingState != TrackingState.Tracking)
                return false;

            pose.position = Frame.Pose.position;
            pose.rotation = Frame.Pose.rotation;
            return true;
        }

        public override bool TryGetCameraImage(ref CameraImage cameraImage)
        {
            if (Frame.TrackingState != TrackingState.Tracking)
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
            if (Frame.TrackingState != TrackingState.Tracking)
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
            if (Session.ConnectionState == SessionConnectionState.Connected && Frame.LightEstimate.State == LightEstimateState.Valid)
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
            camera.gameObject.SetActive(false);
            m_BackgroundRenderer = camera.gameObject.AddComponent<ARCoreBackgroundRenderer>();
            m_BackgroundRenderer.BackgroundMaterial = Resources.Load("Materials/ARBackground", typeof(Material)) as Material;
            camera.gameObject.SetActive(true);
        }

        public override void UpdateCamera(Camera camera)
        {
            if (Screen.orientation == m_CachedScreenOrientation)
                return;

            CalculateDisplayTransform();
            m_CachedScreenOrientation = Screen.orientation;
        }


        private bool FloatCompare(float a, float b)
        {
            return Mathf.Abs(a - b) < 9.99999944E-11f;
        }

        private bool PlaneUpdated(TrackedPlane tp, BoundedPlane bp)
        {
            var extents = (!FloatCompare(tp.ExtentX, bp.extents.x) || !FloatCompare(tp.ExtentZ, bp.extents.y));
            var rotation = tp.Rotation != bp.rotation;
            var position = tp.Position != bp.center;
            return (extents || rotation || position);
        }

        public override void Update()
        {
            if (m_SessionManager == null)
            {
                return;
            }

            AsyncTask.OnUpdate();

            if (Frame.TrackingState != TrackingState.Tracking)
                return;

            if(m_ARCoreSessionConfig.EnablePlaneFinding)
            {
                Frame.GetPlanes(m_TrackedPlaneBuffer);
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
                            boundedPlane.center = trackedPlane.Position;
                            boundedPlane.rotation = trackedPlane.Rotation;
                            boundedPlane.extents.x = trackedPlane.ExtentX;
                            boundedPlane.extents.y = trackedPlane.ExtentZ;
                            OnPlaneUpdated(boundedPlane);
                        }
                    }
                    // add any new planes
                    else
                    {
                        boundedPlane = new BoundedPlane()
                        {
                            id = Guid.NewGuid().ToString(),
                            center = trackedPlane.Position,
                            rotation = trackedPlane.Rotation,
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
            Anchor arCoreAnchor = Session.CreateWorldAnchor(new Pose(arAnchor.transform.position, arAnchor.transform.rotation));
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
