using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.iOS;

namespace UnityARInterface
{
    public class ARKitInterface : ARInterface
    {
        private Material m_CachedClearMaterial;
        private UnityARSessionNativeInterface nativeInterface
        { get { return UnityARSessionNativeInterface.GetARSessionNativeInterface(); } }

        private bool m_TexturesInitialized;
        private int m_CurrentFrameIndex;
        private int m_CameraWidth;
        private int m_CameraHeight;
        private byte[] m_TextureYBytes;
        private byte[] m_TextureUVBytes;
        private byte[] m_TextureYBytes2;
        private byte[] m_TextureUVBytes2;
        private GCHandle m_PinnedYArray;
        private GCHandle m_PinnedUVArray;
        private Vector3[] m_PointCloudData;
        private LightEstimate m_LightEstimate;
		private Matrix4x4 m_DisplayTransform;
        private ARKitWorldTrackingSessionConfiguration m_SessionConfig;
        private Dictionary<string, ARAnchor> m_Anchors = new Dictionary<string, ARAnchor>();

        public override bool IsSupported
        {
            get
            {
                return m_SessionConfig.IsSupported;
            }
        }

        // Use this for initialization
        public override IEnumerator StartService(Settings settings)
        {
            m_SessionConfig = new ARKitWorldTrackingSessionConfiguration(
                UnityARAlignment.UnityARAlignmentGravity,
                settings.enablePlaneDetection ? UnityARPlaneDetection.Horizontal : UnityARPlaneDetection.None,
                settings.enablePointCloud,
                settings.enableLightEstimation);

            if (!IsSupported)
            {
                Debug.LogError("The requested ARKit session configuration is not supported");
                return null;
            }

            UnityARSessionRunOption runOptions =
                UnityARSessionRunOption.ARSessionRunOptionRemoveExistingAnchors |
                UnityARSessionRunOption.ARSessionRunOptionResetTracking;

            nativeInterface.RunWithConfigAndOptions(
                m_SessionConfig, runOptions);

            // Register for plane detection
            UnityARSessionNativeInterface.ARAnchorAddedEvent += AddAnchor;
            UnityARSessionNativeInterface.ARAnchorUpdatedEvent += UpdateAnchor;
            UnityARSessionNativeInterface.ARAnchorRemovedEvent += RemoveAnchor;
            UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateFrame;
            UnityARSessionNativeInterface.ARUserAnchorUpdatedEvent += UpdateUserAnchor;

            IsRunning = true;

            return null;
        }

        private Vector3 GetWorldPosition(ARPlaneAnchor arPlaneAnchor)
        {
            return UnityARMatrixOps.GetPosition(arPlaneAnchor.transform) +
                new Vector3(arPlaneAnchor.center.x, arPlaneAnchor.center.y, -arPlaneAnchor.center.z);
        }

        private BoundedPlane GetBoundedPlane(ARPlaneAnchor arPlaneAnchor)
        {
            return new BoundedPlane()
            {
                id = arPlaneAnchor.identifier,
                center = GetWorldPosition(arPlaneAnchor),
                rotation = UnityARMatrixOps.GetRotation(arPlaneAnchor.transform),
                extents = new Vector2(arPlaneAnchor.extent.x, arPlaneAnchor.extent.z)
            };
        }

        void UpdateFrame(UnityARCamera camera)
        {
            if (!m_TexturesInitialized)
            {
                m_CameraWidth = camera.videoParams.yWidth;
                m_CameraHeight = camera.videoParams.yHeight;

                int numYBytes = camera.videoParams.yWidth * camera.videoParams.yHeight;
                int numUVBytes = camera.videoParams.yWidth * camera.videoParams.yHeight / 2; //quarter resolution, but two bytes per pixel

                m_TextureYBytes = new byte[numYBytes];
                m_TextureUVBytes = new byte[numUVBytes];
                m_TextureYBytes2 = new byte[numYBytes];
                m_TextureUVBytes2 = new byte[numUVBytes];
                m_PinnedYArray = GCHandle.Alloc(m_TextureYBytes);
                m_PinnedUVArray = GCHandle.Alloc(m_TextureUVBytes);
                m_TexturesInitialized = true;
            }

            m_PointCloudData = camera.pointCloudData;
            m_LightEstimate.capabilities = LightEstimateCapabilities.AmbientColorTemperature | LightEstimateCapabilities.AmbientIntensity;
			m_LightEstimate.ambientColorTemperature = camera.lightData.arLightEstimate.ambientColorTemperature;

            // Convert ARKit intensity to Unity intensity
            // ARKit ambient intensity ranges 0-2000
            // Unity ambient intensity ranges 0-8 (for over-bright lights)
			m_LightEstimate.ambientIntensity = camera.lightData.arLightEstimate.ambientIntensity / 1000f;

			//get display transform matrix sent up from sdk
			m_DisplayTransform.SetColumn(0, camera.displayTransform.column0);
			m_DisplayTransform.SetColumn(1, camera.displayTransform.column1);
			m_DisplayTransform.SetColumn(2, camera.displayTransform.column2);
			m_DisplayTransform.SetColumn(3, camera.displayTransform.column3);
        }

        IntPtr PinByteArray(ref GCHandle handle, byte[] array)
        {
            handle.Free();
            handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            return handle.AddrOfPinnedObject();
        }

        byte[] ByteArrayForFrame(int frame, byte[] array0, byte[] array1)
        {
            return frame == 1 ? array1 : array0;
        }

        byte[] YByteArrayForFrame(int frame)
        {
            return ByteArrayForFrame(frame, m_TextureYBytes, m_TextureYBytes2);
        }

        byte[] UVByteArrayForFrame(int frame)
        {
            return ByteArrayForFrame(frame, m_TextureUVBytes, m_TextureUVBytes2);
        }

        private void AddAnchor(ARPlaneAnchor arPlaneAnchor)
        {
            OnPlaneAdded(GetBoundedPlane(arPlaneAnchor));
        }

        private void RemoveAnchor(ARPlaneAnchor arPlaneAnchor)
        {
            OnPlaneRemoved(GetBoundedPlane(arPlaneAnchor));
        }

        private void UpdateAnchor(ARPlaneAnchor arPlaneAnchor)
        {
            OnPlaneUpdated(GetBoundedPlane(arPlaneAnchor));
        }

        private void UpdateUserAnchor(ARUserAnchor anchorData)
        {
            ARAnchor anchor;
            if(m_Anchors.TryGetValue(anchorData.identifier, out anchor)){
                anchor.transform.position = anchorData.transform.GetColumn(3);
                anchor.transform.rotation = anchorData.transform.rotation;   
            }
        }


        public override void StopService()
        {
            var anchors = m_Anchors.Values;
            foreach (var anchor in anchors)
            {
                DestroyAnchor(anchor);
            }
            UnityARSessionNativeInterface.ARUserAnchorUpdatedEvent -= UpdateUserAnchor;
            UnityARSessionNativeInterface.GetARSessionNativeInterface().Pause();

            nativeInterface.SetCapturePixelData(false, IntPtr.Zero, IntPtr.Zero);
            m_PinnedYArray.Free();
            m_PinnedUVArray.Free();
            m_TexturesInitialized = false;

            IsRunning = false;
        }

        public override bool TryGetUnscaledPose(ref Pose pose)
        {
            Matrix4x4 matrix = nativeInterface.GetCameraPose();
            pose.position = UnityARMatrixOps.GetPosition(matrix);
            pose.rotation = UnityARMatrixOps.GetRotation(matrix);
            return true;
        }

        public override bool TryGetCameraImage(ref CameraImage cameraImage)
        {
            ARTextureHandles handles = nativeInterface.GetARVideoTextureHandles();
            if (handles.textureY == System.IntPtr.Zero || handles.textureCbCr == System.IntPtr.Zero)
                return false;

            if (!m_TexturesInitialized)
                return false;

            m_CurrentFrameIndex = (m_CurrentFrameIndex + 1) % 2;

            nativeInterface.SetCapturePixelData(true,
                PinByteArray(ref m_PinnedYArray, YByteArrayForFrame(m_CurrentFrameIndex)),
                PinByteArray(ref m_PinnedUVArray, UVByteArrayForFrame(m_CurrentFrameIndex)));

            cameraImage.y = YByteArrayForFrame(1 - m_CurrentFrameIndex);
            cameraImage.uv = UVByteArrayForFrame(1 - m_CurrentFrameIndex);
            cameraImage.width = m_CameraWidth;
            cameraImage.height = m_CameraHeight;
            return true;
        }

        public override bool TryGetPointCloud(ref PointCloud pointCloud)
        {
            if (m_PointCloudData == null)
                return false;

            if (pointCloud.points == null)
                pointCloud.points = new List<Vector3>();

            pointCloud.points.Clear();
            pointCloud.points.AddRange(m_PointCloudData);
            return true;
        }

        public override LightEstimate GetLightEstimate()
        {
            return m_LightEstimate;
        }

		public override Matrix4x4 GetDisplayTransform()
		{
			return m_DisplayTransform;
		}

        public override void SetupCamera(Camera camera)
        {
            UnityARVideo unityARVideo = camera.GetComponent<UnityARVideo>();
            if (unityARVideo == null)
            {
                m_CachedClearMaterial = Resources.Load("YUVMaterial", typeof(Material)) as Material;
            }
            else
            {
                m_CachedClearMaterial = unityARVideo.m_ClearMaterial;
                GameObject.Destroy(unityARVideo);
            }

            unityARVideo = camera.gameObject.AddComponent<UnityARVideo>();
            unityARVideo.m_ClearMaterial = m_CachedClearMaterial;

            if (camera.GetComponent<UnityARCameraNearFar>() == null)
                camera.gameObject.AddComponent<UnityARCameraNearFar>();

            camera.clearFlags = CameraClearFlags.Depth;
        }

        public override void UpdateCamera(Camera camera)
        {
            camera.projectionMatrix = nativeInterface.GetCameraProjection();
        }

        public override void Update()
        {

        }

        public override void ApplyAnchor(ARAnchor arAnchor)
        {
            if (!IsRunning)
                return;
            
            Matrix4x4 matrix = Matrix4x4.TRS(arAnchor.transform.position, arAnchor.transform.rotation, arAnchor.transform.localScale);
            UnityARUserAnchorData anchorData = new UnityARUserAnchorData();
            anchorData.transform.column0 = matrix.GetColumn(0);
            anchorData.transform.column1 = matrix.GetColumn(1);
            anchorData.transform.column2 = matrix.GetColumn(2);
            anchorData.transform.column3 = matrix.GetColumn(3);

            anchorData = UnityARSessionNativeInterface.GetARSessionNativeInterface().AddUserAnchor(anchorData);
            arAnchor.anchorID = anchorData.identifierStr;
            m_Anchors[arAnchor.anchorID] = arAnchor;
        }

        public override void DestroyAnchor(ARAnchor arAnchor)
        {
            if(!string.IsNullOrEmpty(arAnchor.anchorID)){
                UnityARSessionNativeInterface.GetARSessionNativeInterface().RemoveUserAnchor(arAnchor.anchorID);
                if (m_Anchors.ContainsKey(arAnchor.anchorID))
                {
                    m_Anchors.Remove(arAnchor.anchorID);
                }
                arAnchor.anchorID = null;
            }
        }
    }
}
