﻿using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using System.Text;
using UnityEngine.XR.iOS.Utils;

namespace UnityEngine.XR.iOS
{
	
	public class ConnectToEditor : MonoBehaviour
	{
		PlayerConnection playerConnection;
		UnityARSessionNativeInterface m_session;
		int editorID;

		Texture2D frameBufferTex;

		// Use this for initialization
		void Start()
		{
			Debug.Log("STARTING ConnectToEditor");
			editorID = -1;
			playerConnection = PlayerConnection.instance;
			playerConnection.RegisterConnection(EditorConnected);
			playerConnection.RegisterDisconnection(EditorDisconnected);
			playerConnection.Register(ConnectionMessageIds.fromEditorARKitSessionMsgId, HandleEditorMessage);
			m_session = null;

		}

		void OnGUI()
		{
			if (m_session == null) {	
				GUI.Box (new Rect ((Screen.width / 2) - 200, (Screen.height / 2), 400, 50), "Waiting for editor connection...");
			}
		}

		void HandleEditorMessage(MessageEventArgs mea)
		{
			serializableFromEditorMessage sfem = mea.data.Deserialize<serializableFromEditorMessage>();
			if (sfem != null && sfem.subMessageId == SubMessageIds.editorInitARKit) 
			{
				InitializeARKit ( sfem.arkitConfigMsg );
			} 
			else if (sfem != null && sfem.subMessageId == SubMessageIds.editorInitARKitFaceTracking) 
			{
				InitializeARKitFaceTracking( sfem.arkitConfigMsg);
			}
		}

		void InitializeARKit(serializableARKitInit sai)
		{
			#if !UNITY_EDITOR
			//get the config and runoption from editor and use them to initialize arkit on device
			Application.targetFrameRate = 60;
			m_session = UnityARSessionNativeInterface.GetARSessionNativeInterface();
			ARKitWorldTrackingSessionConfiguration config = sai.config;
			UnityARSessionRunOption runOptions = sai.runOption;
			m_session.RunWithConfigAndOptions(config, runOptions);

		 	UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
			UnityARSessionNativeInterface.ARAnchorAddedEvent += ARAnchorAdded;
			UnityARSessionNativeInterface.ARAnchorUpdatedEvent += ARAnchorUpdated;
			UnityARSessionNativeInterface.ARAnchorRemovedEvent += ARAnchorRemoved;
			#endif
		}

		void InitializeARKitFaceTracking(serializableARKitInit sai)
		{
			#if !UNITY_EDITOR
			//get the config and runoption from editor and use them to initialize arkit for facetracking on device
			Application.targetFrameRate = 60;
			m_session = UnityARSessionNativeInterface.GetARSessionNativeInterface();
			ARKitFaceTrackingConfiguration config = sai.config;
			UnityARSessionRunOption runOptions = sai.runOption;
			m_session.RunWithConfigAndOptions(config, runOptions);

			UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
			UnityARSessionNativeInterface.ARFaceAnchorAddedEvent += ARFaceAnchorAdded;
			UnityARSessionNativeInterface.ARFaceAnchorUpdatedEvent += ARFaceAnchorUpdated;
			UnityARSessionNativeInterface.ARFaceAnchorRemovedEvent += ARFaceAnchorRemoved;
			#endif
		}

		public void ARFrameUpdated(UnityARCamera camera)
		{
			#if !UNITY_EDITOR
			serializableUnityARCamera serARCamera = camera;
			SendToEditor(ConnectionMessageIds.updateCameraFrameMsgId, serARCamera);
			#endif
		}

		public void ARAnchorAdded(ARPlaneAnchor planeAnchor)
		{
			#if !UNITY_EDITOR
			serializableUnityARPlaneAnchor serPlaneAnchor = planeAnchor;
			SendToEditor (ConnectionMessageIds.addPlaneAnchorMsgeId, serPlaneAnchor);
			#endif
		}

		public void ARAnchorUpdated(ARPlaneAnchor planeAnchor)
		{
			#if !UNITY_EDITOR
			serializableUnityARPlaneAnchor serPlaneAnchor = planeAnchor;
			SendToEditor (ConnectionMessageIds.updatePlaneAnchorMsgeId, serPlaneAnchor);
			#endif
		}

		public void ARAnchorRemoved(ARPlaneAnchor planeAnchor)
		{
			#if !UNITY_EDITOR
			serializableUnityARPlaneAnchor serPlaneAnchor = planeAnchor;
			SendToEditor (ConnectionMessageIds.removePlaneAnchorMsgeId, serPlaneAnchor);
			#endif
		}

		public void ARFaceAnchorAdded(ARFaceAnchor faceAnchor)
		{
			#if !UNITY_EDITOR
			serializableUnityARFaceAnchor serFaceAnchor = faceAnchor;
			SendToEditor (ConnectionMessageIds.addFaceAnchorMsgeId, serFaceAnchor);
			#endif
		}

		public void ARFaceAnchorUpdated(ARFaceAnchor faceAnchor)
		{
			#if !UNITY_EDITOR
			serializableUnityARFaceAnchor serFaceAnchor = faceAnchor;
			SendToEditor (ConnectionMessageIds.updateFaceAnchorMsgeId, serFaceAnchor);
			#endif
		}

		public void ARFaceAnchorRemoved(ARFaceAnchor faceAnchor)
		{
			#if !UNITY_EDITOR
			serializableUnityARFaceAnchor serFaceAnchor = faceAnchor;
			SendToEditor (ConnectionMessageIds.removeFaceAnchorMsgeId, serFaceAnchor);
			#endif
		}

		void EditorConnected(int playerID)
		{
			Debug.Log("connected");

			editorID = playerID;

		}

		void EditorDisconnected(int playerID)
		{
			if (editorID == playerID)
			{
				editorID = -1;
			}

			DisconnectFromEditor ();
			#if !UNITY_EDITOR
			if (m_session != null)
			{
				m_session.Pause();
				m_session = null;
			}
			#endif
		}


		public void SendToEditor(System.Guid msgId, object serializableObject)
		{
			byte[] arrayToSend = serializableObject.SerializeToByteArray ();
			SendToEditor (msgId, arrayToSend);
		}

		public void SendToEditor(System.Guid msgId, byte[] data)
		{
			if (playerConnection.isConnected)
			{
				playerConnection.Send(msgId, data);
			}


		}

		public void DisconnectFromEditor()
		{
			#if UNITY_2017_1_OR_NEWER		
			playerConnection.DisconnectAll();
			#endif
		}


	}

}
