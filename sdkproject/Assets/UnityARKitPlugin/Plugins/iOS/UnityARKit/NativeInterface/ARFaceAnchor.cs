using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using AOT;
using System.Text;

namespace UnityEngine.XR.iOS
{

	public static class ARBlendShapeLocation
	{
		 public const string  BrowDownLeft        =   "browDown_L";
	 	 public const string  BrowDownRight       =   "browDown_R";
		 public const string  BrowInnerUp         =   "browInnerUp";
		 public const string  BrowOuterUpLeft     =   "browOuterUp_L";
		 public const string  BrowOuterUpRight    =   "browOuterUp_R";
		 public const string  CheekPuff           =   "cheekPuff";
		 public const string  CheekSquintLeft     =   "cheekSquint_L";
		 public const string  CheekSquintRight    =   "cheekSquint_R";
		 public const string  EyeBlinkLeft        =   "eyeBlink_L";
		 public const string  EyeBlinkRight       =   "eyeBlink_R";
		 public const string  EyeLookDownLeft     =   "eyeLookDown_L";
		 public const string  EyeLookDownRight    =   "eyeLookDown_R";
		 public const string  EyeLookInLeft       =   "eyeLookIn_L";
		 public const string  EyeLookInRight      =   "eyeLookIn_R";
		 public const string  EyeLookOutLeft      =   "eyeLookOut_L";
		 public const string  EyeLookOutRight     =   "eyeLookOut_R";
		 public const string  EyeLookUpLeft       =   "eyeLookUp_L";
		 public const string  EyeLookUpRight      =   "eyeLookUp_R";
		 public const string  EyeSquintLeft       =   "eyeSquint_L";
		 public const string  EyeSquintRight      =   "eyeSquint_R";
		 public const string  EyeWideLeft         =   "eyeWide_L";
		 public const string  EyeWideRight        =   "eyeWide_R";
		 public const string  JawForward          =   "jawForward";
		 public const string  JawLeft             =   "jawLeft";
		 public const string  JawOpen             =   "jawOpen";
		 public const string  JawRight            =   "jawRight";
		 public const string  MouthClose          =   "mouthClose";
		 public const string  MouthDimpleLeft     =   "mouthDimple_L";
		 public const string  MouthDimpleRight    =   "mouthDimple_R";
		 public const string  MouthFrownLeft      =   "mouthFrown_L";
		 public const string  MouthFrownRight     =   "mouthFrown_R";
		 public const string  MouthFunnel         =   "mouthFunnel";
		 public const string  MouthLeft           =   "mouthLeft";
		 public const string  MouthLowerDownLeft  =   "mouthLowerDown_L";
		 public const string  MouthLowerDownRight =   "mouthLowerDown_R";
		 public const string  MouthPressLeft      =   "mouthPress_L";
		 public const string  MouthPressRight     =   "mouthPress_R";
		 public const string  MouthPucker         =   "mouthPucker";
		 public const string  MouthRight          =   "mouthRight";
		 public const string  MouthRollLower      =   "mouthRollLower";
		 public const string  MouthRollUpper      =   "mouthRollUpper";
		 public const string  MouthShrugLower     =   "mouthShrugLower";
		 public const string  MouthShrugUpper     =   "mouthShrugUpper";
		 public const string  MouthSmileLeft      =   "mouthSmile_L";
		 public const string  MouthSmileRight     =   "mouthSmile_R";
		 public const string  MouthStretchLeft    =   "mouthStretch_L";
		 public const string  MouthStretchRight   =   "mouthStretch_R";
		 public const string  MouthUpperUpLeft    =   "mouthUpperUp_L";
		 public const string  MouthUpperUpRight   =   "mouthUpperUp_R";
		 public const string  NoseSneerLeft       =   "noseSneer_L";
		 public const string  NoseSneerRight      =   "noseSneer_R";
		 public const string  TongueOut           =   "tongueOut";
	}


	public struct UnityARFaceGeometry
	{
		public int vertexCount;
		public IntPtr vertices;
		public int textureCoordinateCount;
		public IntPtr textureCoordinates;
		public int triangleCount;
		public IntPtr triangleIndices;

	}

	public struct UnityARFaceAnchorData
	{

		public IntPtr ptrIdentifier;

		/**
 		The transformation matrix that defines the anchor's rotation, translation and scale in world coordinates.
		 */
		public UnityARMatrix4x4 transform;

		public string identifierStr { get { return Marshal.PtrToStringAuto(this.ptrIdentifier); } }

		public UnityARFaceGeometry faceGeometry;
		public IntPtr blendShapes;
		public UnityARMatrix4x4 leftEyeTransform;
		public UnityARMatrix4x4 rightEyeTransform;
		public Vector3 lookAtPoint;
		public bool isTracked;   //this is from the new ARTrackable protocol that ARFaceAnchor now subscribes to
	}
		

	#if !UNITY_EDITOR && UNITY_IOS
	public class ARFaceGeometry
	{
		internal UnityARFaceGeometry uFaceGeometry { private get; set; }

		readonly Vector3[] m_Vertices;
		readonly Vector2[] m_TextureCoordinates;
		readonly int[] m_TriangleIndices;
		readonly float[] m_WorkVertices;
		readonly float[] m_WorkTextureCoordinates;
		readonly short[] m_WorkIndices;  //since ARKit returns Int16
		readonly int m_VertexCount;
		readonly int m_TextureCoordinateCount;
		readonly int m_TriangleCount;
		readonly int m_IndexCount;
		readonly int m_WorkVertexCount;
		readonly int m_WorkTextureCoordinateCount;

		public ARFaceGeometry (UnityARFaceGeometry ufg)
		{
			uFaceGeometry = ufg;
			m_VertexCount = ufg.vertexCount;
			m_Vertices = new Vector3[m_VertexCount];
			m_WorkVertexCount = m_VertexCount * 4;
			m_WorkVertices = new float[m_WorkVertexCount];
			m_TextureCoordinateCount = ufg.textureCoordinateCount;
			m_TextureCoordinates = new Vector2[textureCoordinateCount];
			m_WorkTextureCoordinateCount = m_TextureCoordinateCount * 2;
			m_WorkTextureCoordinates = new float[m_WorkTextureCoordinateCount];
			m_TriangleCount = ufg.triangleCount;
			m_IndexCount = m_TriangleCount * 3;
			m_TriangleIndices = new int[m_IndexCount];
			m_WorkIndices = new short[m_IndexCount];
		}

		public int vertexCount { get { return m_VertexCount; } }
		public int triangleCount { get { return m_TriangleCount; } }
		public int textureCoordinateCount { get { return m_TextureCoordinateCount; } }

		public Vector3 [] vertices { get { return MarshalVertices(uFaceGeometry.vertices); } }

		public Vector2 [] textureCoordinates { get { return MarshalTexCoords(uFaceGeometry.textureCoordinates); } }

		public int [] triangleIndices { get { return MarshalIndices(uFaceGeometry.triangleIndices); } }

		Vector3 [] MarshalVertices(IntPtr ptrFloatArray)
		{
			Marshal.Copy (ptrFloatArray, m_WorkVertices, 0, m_WorkVertexCount);

			for (var count = 0; count < m_WorkVertexCount; count++)
			{
				m_Vertices[count / 4].x =  m_WorkVertices[count++];
				m_Vertices[count / 4].y =  m_WorkVertices[count++];
				m_Vertices[count / 4].z = -m_WorkVertices[count++];
			}

			return m_Vertices;
		}

		int [] MarshalIndices(IntPtr ptrIndices)
		{
			Marshal.Copy (ptrIndices, m_WorkIndices, 0, m_IndexCount);

			for (var count = 0; count < m_IndexCount; count+=3) {
				//reverse winding order
				m_TriangleIndices [count] =     m_WorkIndices [count];
				m_TriangleIndices [count + 1] = m_WorkIndices [count + 2];
				m_TriangleIndices [count + 2] = m_WorkIndices [count + 1];
			}

			return m_TriangleIndices;
		}

		Vector2 [] MarshalTexCoords(IntPtr ptrTexCoords)
		{
			Marshal.Copy (ptrTexCoords, m_WorkTextureCoordinates, 0, m_WorkTextureCoordinateCount);

			for (var count = 0; count < m_WorkTextureCoordinateCount; count++)
			{
				m_TextureCoordinates[count / 2].x = m_WorkTextureCoordinates[count++];
				m_TextureCoordinates[count / 2].y = m_WorkTextureCoordinates[count];
			}

			return m_TextureCoordinates;
		}
	}

	public class ARFaceAnchor
	{
		private UnityARFaceAnchorData faceAnchorData;
		private static Dictionary<string, float> blendshapesDictionary;
		readonly ARFaceGeometry m_FaceGeometry;

		public ARFaceAnchor (UnityARFaceAnchorData ufad)
		{
			faceAnchorData = ufad;
			m_FaceGeometry = new ARFaceGeometry(ufad.faceGeometry);
			if (blendshapesDictionary == null) {
				blendshapesDictionary = new Dictionary<string, float> ();
			}
		}

		public void Update(UnityARFaceAnchorData ufad)
		{
			faceAnchorData = ufad;
			m_FaceGeometry.uFaceGeometry = ufad.faceGeometry;
		}


		public string identifierStr { get { return faceAnchorData.identifierStr; } }

		public bool isTracked { get { return faceAnchorData.isTracked; } }

		public Matrix4x4 transform { 
			get { 
				Matrix4x4 matrix = new Matrix4x4 ();
				matrix.SetColumn (0, faceAnchorData.transform.column0);
				matrix.SetColumn (1, faceAnchorData.transform.column1);
				matrix.SetColumn (2, faceAnchorData.transform.column2);
				matrix.SetColumn (3, faceAnchorData.transform.column3);
				return matrix;
			}
		}

		public Pose leftEyePose
		{
			get
			{
				Matrix4x4 anchorMat = UnityARMatrixOps.GetMatrix (faceAnchorData.transform);
				Matrix4x4 eyeMat = UnityARMatrixOps.GetMatrix (faceAnchorData.leftEyeTransform);
				Matrix4x4 worldEyeMat = anchorMat * eyeMat;
				return UnityARMatrixOps.GetPose(worldEyeMat);
			}
		}

		public Pose rightEyePose
		{
			get
			{
				Matrix4x4 anchorMat = UnityARMatrixOps.GetMatrix (faceAnchorData.transform);
				Matrix4x4 eyeMat = UnityARMatrixOps.GetMatrix (faceAnchorData.rightEyeTransform);
				Matrix4x4 worldEyeMat = anchorMat * eyeMat;
				return UnityARMatrixOps.GetPose(worldEyeMat);
			}
		}

		public Vector3 lookAtPoint
		{
			get
			{
				Matrix4x4 anchorMat = UnityARMatrixOps.GetMatrix (faceAnchorData.transform);
				return anchorMat.MultiplyPoint3x4 (UnityARMatrixOps.GetPosition (faceAnchorData.lookAtPoint));
			}
		}

		public ARFaceGeometry faceGeometry
		{
			get
			{
				m_FaceGeometry.uFaceGeometry = faceAnchorData.faceGeometry;
				return m_FaceGeometry;
			}
		}

		public Dictionary<string, float> blendShapes { get { return GetBlendShapesFromNative(faceAnchorData.blendShapes); } }

		delegate void DictionaryVisitorHandler(IntPtr keyPtr, float value);

		[DllImport("__Internal")]
		private static extern void GetBlendShapesInfo(IntPtr ptrDic, DictionaryVisitorHandler handler);

		Dictionary<string, float> GetBlendShapesFromNative(IntPtr blendShapesPtr)
		{
			blendshapesDictionary.Clear ();
			GetBlendShapesInfo (blendShapesPtr, AddElementToManagedDictionary);
			return blendshapesDictionary;
		}

		[MonoPInvokeCallback(typeof(DictionaryVisitorHandler))]
		static void AddElementToManagedDictionary(IntPtr keyPtr, float value)
		{
			string key = Marshal.PtrToStringAuto(keyPtr);
			blendshapesDictionary.Add(key, value);
		}
	}
	#endif
}
