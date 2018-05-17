using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using Utils;
using System.Collections;
using UnityEngine.XR;

#if UNITY_EDITOR
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.XR.iOS;

// Runs on the Editor. Talks to the remote Player.
namespace UnityARInterface
{
    public class ARRemoteEditorInterface : ARInterface
    {
        private bool m_SendVideo;
        public bool sendVideo
        {
            get { return m_SendVideo; }
            set
            {
                m_SendVideo = value;
                if (editorConnection != null)
                {
                    SendToPlayer(
                        ARMessageIds.SubMessageIds.enableVideo,
                        new SerializableEnableVideo(sendVideo));
                }
            }
        }

        public EditorConnection editorConnection = null;
        private int m_CurrentPlayerId = -1;
        private SerializableFrame m_Frame = null;
        private Settings m_CachedSettings;
        private Camera m_CachedCamera;
        private LightEstimate m_LightEstimate;
        private CameraImage m_CameraImage;
        private ARBackgroundRenderer m_BackgroundRenderer;
        public bool connected { get { return m_CurrentPlayerId != -1; } }
        public int playerId { get { return m_CurrentPlayerId; } }

        public bool IsRemoteServiceRunning { get; protected set; } 

        private bool m_BackgroundRendering;
        public override bool BackgroundRendering
        {
            get
            {
                return m_BackgroundRendering;
            }
            set
            {
                m_BackgroundRendering = value;

                if (m_BackgroundRenderer != null){
                    m_BackgroundRenderer.mode = m_BackgroundRendering ? 
                        ARRenderMode.MaterialAsBackground : ARRenderMode.StandardBackground;
                }

                if (editorConnection != null)
                {
                    SendToPlayer(
                        ARMessageIds.SubMessageIds.backgroundRendering,
                        new SerializableBackgroundRendering(m_BackgroundRendering));
                }
            }
        }

        Texture2D m_RemoteScreenYTexture;
        Texture2D m_RemoteScreenUVTexture;

        List<Vector3> m_PointCloud;
		private Matrix4x4 m_DisplayTransform;

        public void ScreenCaptureParamsMessageHandler(MessageEventArgs message)
        {
            var screenCaptureParams = message.data.Deserialize<SerializableScreenCaptureParams>();

            if (m_RemoteScreenYTexture == null ||
                m_RemoteScreenYTexture.width != screenCaptureParams.width ||
                m_RemoteScreenYTexture.height != screenCaptureParams.height)
            {
                if (m_RemoteScreenYTexture != null)
                    GameObject.Destroy(m_RemoteScreenYTexture);

                m_RemoteScreenYTexture = new Texture2D(
                    screenCaptureParams.width,
                    screenCaptureParams.height,
                    TextureFormat.R8, false, true);
            }

            if (m_RemoteScreenUVTexture == null ||
                m_RemoteScreenUVTexture.width != screenCaptureParams.width ||
                m_RemoteScreenUVTexture.height != screenCaptureParams.height)
            {
                if (m_RemoteScreenUVTexture != null)
                    GameObject.Destroy(m_RemoteScreenUVTexture);

                m_RemoteScreenUVTexture = new Texture2D(
                    screenCaptureParams.width / 2,
                    screenCaptureParams.height / 2,
                    TextureFormat.RG16, false, true);
            }

            Material YUVMaterial = Resources.Load("YUVMaterial", typeof(Material)) as Material;
            YUVMaterial.SetMatrix("_DisplayTransform", GetDisplayTransform());
            YUVMaterial.SetTexture("_textureY", m_RemoteScreenYTexture);
            YUVMaterial.SetTexture("_textureCbCr", m_RemoteScreenUVTexture);

            if(m_BackgroundRenderer != null){
                m_BackgroundRenderer.backgroundMaterial = null;
                m_BackgroundRenderer.camera = null;
            }

            m_BackgroundRenderer = new ARBackgroundRenderer();
            m_BackgroundRenderer.backgroundMaterial = YUVMaterial;
            m_BackgroundRenderer.camera = m_CachedCamera;
            //Set mode and send to player
            BackgroundRendering = m_BackgroundRendering;
           
            m_CameraImage.width = screenCaptureParams.width;
            m_CameraImage.height = screenCaptureParams.height;
        }

        public void ScreenCaptureYMessageHandler(MessageEventArgs message)
        {
            m_CameraImage.y = message.data;

            if (m_RemoteScreenYTexture == null)
                return;

            m_RemoteScreenYTexture.LoadRawTextureData(message.data);
            m_RemoteScreenYTexture.Apply();
        }

        public void ScreenCaptureUVMessageHandler(MessageEventArgs message)
        {
            m_CameraImage.uv = message.data;

            //In case of ARCore sending grayscale image, UV data would be null.
            if (m_RemoteScreenUVTexture == null || message.data == null)
                return;

            m_RemoteScreenUVTexture.LoadRawTextureData(message.data);
            m_RemoteScreenUVTexture.Apply();
        }

        public void PlaneAddedMessageHandler(MessageEventArgs message)
        {
            OnPlaneAdded(message.data.Deserialize<SerializableBoundedPlane>());
        }

        public void PlaneUpdatedMessageHandler(MessageEventArgs message)
        {
            OnPlaneUpdated(message.data.Deserialize<SerializableBoundedPlane>());
        }

        public void PlaneRemovedMessageHandler(MessageEventArgs message)
        {
            OnPlaneRemoved(message.data.Deserialize<SerializableBoundedPlane>());
        }

        public void PointCloudMessageHandler(MessageEventArgs message)
        {
            // Copy to internal buffer
            var pointCloud = message.data.Deserialize<SerializablePointCloud>();
            if (m_PointCloud == null)
                m_PointCloud = new List<Vector3>();

            m_PointCloud.Clear();
            m_PointCloud.AddRange(pointCloud.asEnumerable);
        }

        public void LightEstimateMessageHandler(MessageEventArgs message)
        {
            m_LightEstimate = message.data.Deserialize<SerializableLightEstimate>();
        }

        public void FrameMessageHandler(MessageEventArgs message)
        {
            m_Frame = message.data.Deserialize<SerializableFrame>();
        }

        public void PlayerConnectedMessageHandler(EditorConnection editorConnection, int playerId)
        {
            this.editorConnection = editorConnection;
            m_CurrentPlayerId = playerId;
        }

        public void PlayerDisconnectedMessageHandler(int playerId)
        {
            if (m_CurrentPlayerId == playerId)
            {
                m_CurrentPlayerId = -1;
                m_Frame = null;
                editorConnection = null;
            }
        }

        void SendToPlayer(System.Guid msgId, object serializableObject)
        {
            var message = new SerializableSubMessage(msgId, serializableObject);
            var bytesToSend = message.SerializeToByteArray();
            editorConnection.Send(ARMessageIds.fromEditor, bytesToSend);
        }

        public void StartRemoteService(Settings settings)
        {
            sendVideo = m_SendVideo;
            var serializedSettings = (SerializableARSettings)settings;
            SendToPlayer(ARMessageIds.SubMessageIds.startService, serializedSettings);
            IsRemoteServiceRunning = true;
        }

        public void StopRemoteService()
        {
            SendToPlayer(ARMessageIds.SubMessageIds.stopService, null);
            IsRemoteServiceRunning = false;
        }

        //
        // From the ARInterface
        //
        public override IEnumerator StartService(Settings settings)
        {
            IsRunning = true;
            return null;
        }

        public override void StopService()
        {
            IsRunning = false;
        }

        public override void SetupCamera(Camera camera)
        {
            m_CachedCamera = camera;
        }

        public override bool TryGetUnscaledPose(ref Pose pose)
        {
            if (m_Frame != null)
            {
                pose.position = m_Frame.cameraPosition;
                pose.rotation = m_Frame.cameraRotation;
                return true;
            }

            return false;
        }

        public override bool TryGetCameraImage(ref CameraImage cameraImage)
        {
            //If remote device is not sending video, we can assume that the current cached frame is out of date.
            if (!m_SendVideo)
                return false;
            //We only return a cached frame if it has all it's components
            if (m_CameraImage.height == 0 || m_CameraImage.width == 0 || m_CameraImage.y == null || m_CameraImage.uv == null)
                return false;

            cameraImage.width = m_CameraImage.width;
            cameraImage.height = m_CameraImage.height;
            cameraImage.y = m_CameraImage.y;
            cameraImage.uv = m_CameraImage.uv;

            return true;
        }

        public override bool TryGetPointCloud(ref PointCloud pointCloud)
        {
            if (m_PointCloud == null)
                return false;

            if (pointCloud.points == null)
                pointCloud.points = new List<Vector3>();

            pointCloud.points.Clear();
            pointCloud.points.AddRange(m_PointCloud);
            return true;
        }

        public override LightEstimate GetLightEstimate()
        {
            return m_LightEstimate;
        }

		public override Matrix4x4 GetDisplayTransform()
		{
			if (m_Frame != null) {
				return m_Frame.displayTransform;
			} else {
				return Matrix4x4.identity;
			}
		}

        public override void Update()
        {

        }

        public override void UpdateCamera(Camera camera)
        {
            if (m_Frame != null)
            {
                camera.projectionMatrix = m_Frame.projectionMatrix;
                if (m_BackgroundRenderer != null)
                {
                    m_BackgroundRenderer.backgroundMaterial.SetMatrix("_DisplayTransform",GetDisplayTransform());
                }
            }
        }
    }
}
#endif
