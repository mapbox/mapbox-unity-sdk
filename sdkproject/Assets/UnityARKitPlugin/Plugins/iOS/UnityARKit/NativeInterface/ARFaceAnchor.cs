using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using AOT;

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

	};


	public class ARFaceGeometry
	{
		private UnityARFaceGeometry uFaceGeometry;

		public ARFaceGeometry (UnityARFaceGeometry ufg)
		{
			uFaceGeometry = ufg;
		}

		public int vertexCount { get { return uFaceGeometry.vertexCount; } }
		public int triangleCount {  get  { return uFaceGeometry.triangleCount; } }
		public int textureCoordinateCount { get { return uFaceGeometry.textureCoordinateCount; } }

		public Vector3 [] vertices { get { return MarshalVertices(uFaceGeometry.vertices,vertexCount); } }

		public Vector2 [] textureCoordinates { get { return MarshalTexCoords(uFaceGeometry.textureCoordinates, textureCoordinateCount); } }

		public int [] triangleIndices { get { return MarshalIndices(uFaceGeometry.triangleIndices, triangleCount); } }

		Vector3 [] MarshalVertices(IntPtr ptrFloatArray, int vertCount)
		{
			int numFloats = vertCount * 4;
			float [] workVerts = new float[numFloats];
			Marshal.Copy (ptrFloatArray, workVerts, 0, (int)(numFloats)); 

			Vector3[] verts = new Vector3[vertCount];

			for (int count = 0; count < numFloats; count++)
			{
				verts [count / 4].x = workVerts[count++];
				verts [count / 4].y = workVerts[count++];
				verts [count / 4].z = -workVerts[count++];
			}

			return verts;
		}

		int [] MarshalIndices(IntPtr ptrIndices, int triCount)
		{
			int numIndices = triCount * 3;
			short [] workIndices = new short[numIndices];  //since ARKit returns Int16
			Marshal.Copy (ptrIndices, workIndices, 0, numIndices);

			int[] triIndices = new int[numIndices];
			for (int count = 0; count < numIndices; count+=3) {
				//reverse winding order
				triIndices [count] = workIndices [count];
				triIndices [count + 1] = workIndices [count + 2];
				triIndices [count + 2] = workIndices [count + 1];
			}

			return triIndices;
		}

		Vector2 [] MarshalTexCoords(IntPtr ptrTexCoords, int texCoordCount)
		{
			int numFloats = texCoordCount * 2;
			float [] workTexCoords = new float[numFloats];
			Marshal.Copy (ptrTexCoords, workTexCoords, 0, (int)(numFloats)); 

			Vector2[] texCoords = new Vector2[texCoordCount];

			for (int count = 0; count < numFloats; count++)
			{
				texCoords [count / 2].x = workTexCoords[count++];
				texCoords [count / 2].y = workTexCoords[count];
			}

			return texCoords;

		}
	}


	public class ARFaceAnchor 
	{
		private UnityARFaceAnchorData faceAnchorData;
		private static Dictionary<string, float> blendshapesDictionary;

		public ARFaceAnchor (UnityARFaceAnchorData ufad)
		{
			faceAnchorData = ufad;
			if (blendshapesDictionary == null) {
				blendshapesDictionary = new Dictionary<string, float> ();
			}
		}
		

		public string identifierStr { get { return faceAnchorData.identifierStr; } }

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

		public ARFaceGeometry faceGeometry { get { return new ARFaceGeometry (faceAnchorData.faceGeometry);	} }

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
}
