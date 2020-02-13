using System;
using UnityEngine;
using UnityEngine.XR.iOS;
using System.Text;
using System.Collections.Generic;

namespace UnityEngine.XR.iOS.Utils
{
	/// <summary>
	/// Since unity doesn't flag the Vector4 as serializable, we
	/// need to create our own version. This one will automatically convert
	/// between Vector4 and SerializableVector4
	/// </summary>
	[Serializable]
	public class SerializableVector4
	{
		/// <summary>
		/// x component
		/// </summary>
		public float x;

		/// <summary>
		/// y component
		/// </summary>
		public float y;

		/// <summary>
		/// z component
		/// </summary>
		public float z;

		/// <summary>
		/// w component
		/// </summary>
		public float w;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rX"></param>
		/// <param name="rY"></param>
		/// <param name="rZ"></param>
		/// <param name="rW"></param>
		public SerializableVector4(float rX, float rY, float rZ, float rW)
		{
			x = rX;
			y = rY;
			z = rZ;
			w = rW;
		}

		/// <summary>
		/// Returns a string representation of the object
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("[{0}, {1}, {2}, {3}]", x, y, z, w);
		}

		/// <summary>
		/// Automatic conversion from SerializableVector4 to Vector4
		/// </summary>
		/// <param name="rValue"></param>
		/// <returns></returns>
		public static implicit operator Vector4(SerializableVector4 rValue)
		{
			return new Vector4(rValue.x, rValue.y, rValue.z, rValue.w);
		}

		/// <summary>
		/// Automatic conversion from Vector4 to SerializableVector4
		/// </summary>
		/// <param name="rValue"></param>
		/// <returns></returns>
		public static implicit operator SerializableVector4(Vector4 rValue)
		{
			return new SerializableVector4(rValue.x, rValue.y, rValue.z, rValue.w);
		}
	}

	[Serializable]  
	public class serializableUnityARMatrix4x4
	{
		public SerializableVector4 column0;
		public SerializableVector4 column1;
		public SerializableVector4 column2;
		public SerializableVector4 column3;

		public serializableUnityARMatrix4x4(SerializableVector4 v0, SerializableVector4 v1, SerializableVector4 v2, SerializableVector4 v3)
		{
			column0 = v0;
			column1 = v1;
			column2 = v2;
			column3 = v3;
		}

		/// <summary>
		/// Automatic conversion from UnityARMatrix4x4 to serializableUnityARMatrix4x4
		/// </summary>
		/// <param name="rValue"></param>
		/// <returns></returns>
		public static implicit operator serializableUnityARMatrix4x4(UnityARMatrix4x4 rValue)
		{
			return new serializableUnityARMatrix4x4(rValue.column0, rValue.column1, rValue.column2, rValue.column3);
		}

		/// <summary>
		/// Automatic conversion from serializableUnityARMatrix4x4 to UnityARMatrix4x4
		/// </summary>
		/// <param name="rValue"></param>
		/// <returns></returns>
		public static implicit operator UnityARMatrix4x4(serializableUnityARMatrix4x4 rValue)
		{
			return new UnityARMatrix4x4(rValue.column0, rValue.column1, rValue.column2, rValue.column3);
		}


		public static implicit operator serializableUnityARMatrix4x4(Matrix4x4 rValue)
		{
			return new serializableUnityARMatrix4x4(rValue.GetColumn(0), rValue.GetColumn(1), rValue.GetColumn(2), rValue.GetColumn(3));
		}

		public static implicit operator Matrix4x4(serializableUnityARMatrix4x4 rValue)
		{
			#if UNITY_2017_1_OR_NEWER		
			return new Matrix4x4(rValue.column0, rValue.column1, rValue.column2, rValue.column3);
			#else
			Matrix4x4 mRet = new Matrix4x4 ();
			mRet.SetColumn (0, rValue.column0);
			mRet.SetColumn (1, rValue.column1);
			mRet.SetColumn (2, rValue.column2);
			mRet.SetColumn (3, rValue.column3);
			return mRet;
			#endif
		}

	};


	[Serializable]
	public class serializableSHC
	{
		public byte [] shcData;

		public serializableSHC(byte [] inputSHCData)
		{
			shcData = inputSHCData;
		}

		public static implicit operator serializableSHC(float [] floatsSHC)
		{
			if (floatsSHC != null)
			{
				byte [] createBuf = new byte[floatsSHC.Length * sizeof(float)];
				for(int i = 0; i < floatsSHC.Length; i++)
				{
					Buffer.BlockCopy( BitConverter.GetBytes( floatsSHC[i] ), 0, createBuf, (i)*sizeof(float), sizeof(float) );
				}
				return new serializableSHC (createBuf);
			}
			else 
			{
				return new serializableSHC(null);
			}
		}

		public static implicit operator float [] (serializableSHC spc)
		{
			if (spc.shcData != null) 
			{
				int numFloats = spc.shcData.Length / (sizeof(float));
				float [] shcFloats = new float[numFloats];
				for (int i = 0; i < numFloats; i++) 
				{
					shcFloats [i] = BitConverter.ToSingle (spc.shcData, i * sizeof(float));
				}
				return shcFloats;
			} 
			else 
			{
				return null;
			}
		}


	};

	[Serializable]
	public class serializableUnityARLightData
	{
		public LightDataType whichLight;
		public serializableSHC lightSHC;
		public SerializableVector4 primaryLightDirAndIntensity;
		public float ambientIntensity;
		public float ambientColorTemperature;

		serializableUnityARLightData(UnityARLightData lightData)
		{
			whichLight = lightData.arLightingType;
			if (whichLight == LightDataType.DirectionalLightEstimate) {
				lightSHC = lightData.arDirectonalLightEstimate.sphericalHarmonicsCoefficients;
				Vector3 lightDir = lightData.arDirectonalLightEstimate.primaryLightDirection;
				float lightIntensity = lightData.arDirectonalLightEstimate.primaryLightIntensity;
				primaryLightDirAndIntensity = new SerializableVector4 (lightDir.x, lightDir.y, lightDir.z, lightIntensity);
			} else {
				ambientIntensity = lightData.arLightEstimate.ambientIntensity;
				ambientColorTemperature = lightData.arLightEstimate.ambientColorTemperature;
			}
		}

		public static implicit operator serializableUnityARLightData(UnityARLightData rValue)
		{
			return new serializableUnityARLightData(rValue);
		}

		public static implicit operator UnityARLightData(serializableUnityARLightData rValue)
		{
			UnityARDirectionalLightEstimate udle = null;
			UnityARLightEstimate ule = new UnityARLightEstimate (rValue.ambientIntensity, rValue.ambientColorTemperature);

			if (rValue.whichLight == LightDataType.DirectionalLightEstimate) {
				Vector3 lightDir = new Vector3 (rValue.primaryLightDirAndIntensity.x, rValue.primaryLightDirAndIntensity.y, rValue.primaryLightDirAndIntensity.z);
				udle = new UnityARDirectionalLightEstimate (rValue.lightSHC, lightDir, rValue.primaryLightDirAndIntensity.w);
			} 

			return new UnityARLightData(rValue.whichLight, ule, udle);
		}

	}


	[Serializable]  
	public class serializableUnityARCamera
	{
		public serializableUnityARMatrix4x4 worldTransform;
		public serializableUnityARMatrix4x4 projectionMatrix;
		public ARTrackingState trackingState;
		public ARTrackingStateReason trackingReason;
		public UnityVideoParams videoParams;
		public serializableUnityARLightData lightData;
		public serializablePointCloud pointCloud;
		public serializableUnityARMatrix4x4 displayTransform;
		public ARWorldMappingStatus worldMappingStatus;


		public serializableUnityARCamera( serializableUnityARMatrix4x4 wt, serializableUnityARMatrix4x4 pm, ARTrackingState ats, ARTrackingStateReason atsr, UnityVideoParams uvp, UnityARLightData lightDat, serializableUnityARMatrix4x4 dt, serializablePointCloud spc, 
				ARWorldMappingStatus awms)
		{
			worldTransform = wt;
			projectionMatrix = pm;
			trackingState = ats;
			trackingReason = atsr;
			videoParams = uvp;
			lightData = lightDat;
			displayTransform = dt;
			pointCloud = spc;
			worldMappingStatus = awms;
		}
		#if UNITY_EDITOR
		public static implicit operator UnityARCamera(serializableUnityARCamera rValue)
		{
			return new UnityARCamera (rValue.worldTransform, rValue.projectionMatrix, rValue.trackingState, rValue.trackingReason, rValue.videoParams, rValue.lightData, rValue.displayTransform, rValue.pointCloud, rValue.worldMappingStatus);
		}
		#else //!UNITY_EDITOR
		public static implicit operator serializableUnityARCamera(UnityARCamera rValue)
		{
			return new serializableUnityARCamera(rValue.worldTransform, rValue.projectionMatrix, rValue.trackingState, rValue.trackingReason, rValue.videoParams, rValue.lightData, rValue.displayTransform, rValue.pointCloud, rValue.worldMappingStatus);
		}
		#endif
	};

	[Serializable]
	public class serializablePlaneGeometry
	{
		public byte [] vertices;
		public byte [] texCoords;
		public byte [] triIndices;
		public byte[] boundaryVertices;


		public serializablePlaneGeometry(byte [] inputVertices, byte [] inputTexCoords, byte [] inputTriIndices, byte [] boundaryVerts)
		{
			vertices = inputVertices;
			texCoords = inputTexCoords;
			triIndices = inputTriIndices;
			boundaryVertices = boundaryVerts;

		}

		#if !UNITY_EDITOR
		public static implicit operator serializablePlaneGeometry(ARPlaneGeometry planeGeom)
		{
			if (planeGeom.vertexCount != 0 && planeGeom.textureCoordinateCount != 0 && planeGeom.triangleCount != 0)
			{
				Vector3 [] planeVertices = planeGeom.vertices;
				byte [] cbVerts = new byte[planeGeom.vertexCount * sizeof(float) * 3];
				Buffer.BlockCopy( planeVertices, 0, cbVerts, 0, planeGeom.vertexCount * sizeof(float) * 3 );
			
				Vector3 [] boundaryVertices = planeGeom.boundaryVertices;
				byte [] cbBVerts = new byte[planeGeom.boundaryVertexCount * sizeof(float) * 3];
				Buffer.BlockCopy( boundaryVertices, 0, cbBVerts, 0, planeGeom.boundaryVertexCount * sizeof(float) * 3 );


				Vector2 [] planeTexCoords = planeGeom.textureCoordinates;
				byte [] cbTexCoords = new byte[planeGeom.textureCoordinateCount * sizeof(float) * 2];
				Buffer.BlockCopy( planeTexCoords, 0, cbTexCoords, 0, planeGeom.textureCoordinateCount * sizeof(float) * 2 );


				int [] triIndices = planeGeom.triangleIndices;
				byte [] cbTriIndices = triIndices.SerializeToByteArray();

				return new serializablePlaneGeometry (cbVerts, cbTexCoords, cbTriIndices, cbBVerts);
			}
			else 
			{
				return new serializablePlaneGeometry(null, null, null, null);
			}
		}
		#endif //!UNITY_EDITOR

		public Vector3 [] Vertices {
			get {
				if (vertices != null) {
					int numVectors = vertices.Length / (3 * sizeof(float));
					Vector3[] verticesVec = new Vector3[numVectors];
					for (int i = 0; i < numVectors; i++) {
						int bufferStart = i * 3;
						verticesVec [i].x = BitConverter.ToSingle (vertices, (bufferStart) * sizeof(float));
						verticesVec [i].y = BitConverter.ToSingle (vertices, (bufferStart + 1) * sizeof(float));
						verticesVec [i].z = BitConverter.ToSingle (vertices, (bufferStart + 2) * sizeof(float));

					}
					return verticesVec;
				} else {
					return null;
				}
			}
		}

		public Vector3 [] BoundaryVertices {
			get {
				if (boundaryVertices != null) {
					int numVectors = boundaryVertices.Length / (3 * sizeof(float));
					Vector3[] verticesVec = new Vector3[numVectors];
					for (int i = 0; i < numVectors; i++) {
						int bufferStart = i * 3;
						verticesVec [i].x = BitConverter.ToSingle (boundaryVertices, (bufferStart) * sizeof(float));
						verticesVec [i].y = BitConverter.ToSingle (boundaryVertices, (bufferStart + 1) * sizeof(float));
						verticesVec [i].z = BitConverter.ToSingle (boundaryVertices, (bufferStart + 2) * sizeof(float));

					}
					return verticesVec;
				} else {
					return null;
				}
			}
		}

		public Vector2 [] TexCoords {
			get {
				if (texCoords != null) {
					int numVectors = texCoords.Length / (2 * sizeof(float));
					Vector2[] texCoordVec = new Vector2[numVectors];
					for (int i = 0; i < numVectors; i++) {
						int bufferStart = i * 2;
						texCoordVec [i].x = BitConverter.ToSingle (texCoords, (bufferStart) * sizeof(float));
						texCoordVec [i].y = BitConverter.ToSingle (texCoords, (bufferStart + 1) * sizeof(float));

					}
					return texCoordVec;
				} else {
					return null;
				}
			}
		}

		public int [] TriangleIndices {
			get {
				if (triIndices != null) {
					int[] triIndexVec = triIndices.Deserialize<int[]>();
					return triIndexVec;
				} else {
					return null;
				}
			}
		}

	};


	[Serializable]  
	public class serializableUnityARPlaneAnchor
	{
		public serializableUnityARMatrix4x4 worldTransform;
		public SerializableVector4 center;
		public SerializableVector4 extent;
		public ARPlaneAnchorAlignment planeAlignment;
		public serializablePlaneGeometry planeGeometry;
		public byte[] identifierStr;

		public serializableUnityARPlaneAnchor( serializableUnityARMatrix4x4 wt, SerializableVector4 ctr, SerializableVector4 ext, ARPlaneAnchorAlignment apaa,
			serializablePlaneGeometry spg, byte [] idstr)
		{
			worldTransform = wt;
			center = ctr;
			extent = ext;
			planeAlignment = apaa;
			identifierStr = idstr;
			planeGeometry = spg;
		}

		#if UNITY_EDITOR
		public static implicit operator ARPlaneAnchor(serializableUnityARPlaneAnchor rValue)
		{
			return  new ARPlaneAnchor(rValue);
		}
		#else //!UNITY_EDITOR
		public static implicit operator serializableUnityARPlaneAnchor(ARPlaneAnchor rValue)
		{
			serializableUnityARMatrix4x4 wt = rValue.transform;
			SerializableVector4 ctr = new SerializableVector4 (rValue.center.x, rValue.center.y, rValue.center.z, 1.0f);
			SerializableVector4 ext = new SerializableVector4 (rValue.extent.x, rValue.extent.y, rValue.extent.z, 1.0f);
			byte[] idstr = Encoding.UTF8.GetBytes (rValue.identifier);
			serializablePlaneGeometry spg = rValue.planeGeometry;
			return new serializableUnityARPlaneAnchor(wt, ctr, ext, rValue.alignment, spg,  idstr);
		}
		#endif

	};


	[Serializable]
	public class serializableFaceGeometry
	{
		public byte [] vertices;
		public byte [] texCoords;
		public byte [] triIndices;


		public serializableFaceGeometry(byte [] inputVertices, byte [] inputTexCoords, byte [] inputTriIndices)
		{
			vertices = inputVertices;
			texCoords = inputTexCoords;
			triIndices = inputTriIndices;

		}

		#if !UNITY_EDITOR
		public static implicit operator serializableFaceGeometry(ARFaceGeometry faceGeom)
		{
			if (faceGeom.vertexCount != 0 && faceGeom.textureCoordinateCount != 0 && faceGeom.triangleCount != 0)
			{
				Vector3 [] faceVertices = faceGeom.vertices;
				byte [] cbVerts = new byte[faceGeom.vertexCount * sizeof(float) * 3];
				Buffer.BlockCopy( faceVertices, 0, cbVerts, 0, faceGeom.vertexCount * sizeof(float) * 3 );
				

				Vector2 [] faceTexCoords = faceGeom.textureCoordinates;
				byte [] cbTexCoords = new byte[faceGeom.textureCoordinateCount * sizeof(float) * 2];
				Buffer.BlockCopy( faceTexCoords, 0, cbTexCoords, 0, faceGeom.textureCoordinateCount * sizeof(float) * 2 );


				int [] triIndices = faceGeom.triangleIndices;
				byte [] cbTriIndices = triIndices.SerializeToByteArray();

				return new serializableFaceGeometry (cbVerts, cbTexCoords, cbTriIndices);
			}
			else 
			{
				return new serializableFaceGeometry(null, null, null);
			}
		}
		#endif //!UNITY_EDITOR

		public Vector3 [] Vertices {
			get {
				if (vertices != null) {
					int numVectors = vertices.Length / (3 * sizeof(float));
					Vector3[] verticesVec = new Vector3[numVectors];
					for (int i = 0; i < numVectors; i++) {
						int bufferStart = i * 3;
						verticesVec [i].x = BitConverter.ToSingle (vertices, (bufferStart) * sizeof(float));
						verticesVec [i].y = BitConverter.ToSingle (vertices, (bufferStart + 1) * sizeof(float));
						verticesVec [i].z = BitConverter.ToSingle (vertices, (bufferStart + 2) * sizeof(float));

					}
					return verticesVec;
				} else {
					return null;
				}
			}
		}

		public Vector2 [] TexCoords {
			get {
				if (texCoords != null) {
					int numVectors = texCoords.Length / (2 * sizeof(float));
					Vector2[] texCoordVec = new Vector2[numVectors];
					for (int i = 0; i < numVectors; i++) {
						int bufferStart = i * 2;
						texCoordVec [i].x = BitConverter.ToSingle (texCoords, (bufferStart) * sizeof(float));
						texCoordVec [i].y = BitConverter.ToSingle (texCoords, (bufferStart + 1) * sizeof(float));

					}
					return texCoordVec;
				} else {
					return null;
				}
			}
		}

		public int [] TriangleIndices {
			get {
				if (triIndices != null) {
					int[] triIndexVec = triIndices.Deserialize<int[]>();
					return triIndexVec;
				} else {
					return null;
				}
			}
		}

	};


	[Serializable]  
	public class serializableUnityARFaceAnchor
	{
		public serializableUnityARMatrix4x4 worldTransform;
		public serializableFaceGeometry faceGeometry;
		public Dictionary<string, float> arBlendShapes;
		public byte[] identifierStr;
		public bool isTracked;

		public serializableUnityARFaceAnchor( serializableUnityARMatrix4x4 wt, serializableFaceGeometry fg, Dictionary<string, float> bs, byte [] idstr, bool bIsTracked)
		{
			worldTransform = wt;
			faceGeometry = fg;
			arBlendShapes = bs;
			identifierStr = idstr;
			isTracked = bIsTracked;
		}


		#if UNITY_EDITOR
		public static implicit operator ARFaceAnchor(serializableUnityARFaceAnchor rValue)
		{
			return new ARFaceAnchor(rValue);
		}
		#else
		public static implicit operator serializableUnityARFaceAnchor(ARFaceAnchor rValue)
		{
			serializableUnityARMatrix4x4 wt = rValue.transform;
			serializableFaceGeometry sfg = rValue.faceGeometry;
			byte[] idstr = Encoding.UTF8.GetBytes (rValue.identifierStr);
			return new serializableUnityARFaceAnchor(wt, sfg, rValue.blendShapes, idstr, rValue.isTracked);
		}
		#endif
	};


	[Serializable]
	public class serializablePointCloud
	{
		public byte [] pointCloudData;
		public byte[] pointCloudIds;

		public serializablePointCloud(byte [] inputPoints, byte [] inputIds)
		{
			pointCloudData = inputPoints;
			pointCloudIds = inputIds;
		}

		#if !UNITY_EDITOR 
		public static implicit operator serializablePointCloud(ARPointCloud pointCloud)
		{
			byte[] pointsBuf = null;
			byte[] idsBuf = null;

			if (pointCloud != null)
			{
				Vector3[] vecPointCloud = pointCloud.Points;
				if (vecPointCloud != null && vecPointCloud.Length > 0)
				{
					pointsBuf = new byte[vecPointCloud.Length * sizeof(float) * 3];
					for(int i = 0; i < vecPointCloud.Length; i++)
					{
						int bufferStart = i * 3;
						Buffer.BlockCopy( BitConverter.GetBytes( vecPointCloud[i].x ), 0, pointsBuf, (bufferStart)*sizeof(float), sizeof(float) );
						Buffer.BlockCopy( BitConverter.GetBytes( vecPointCloud[i].y ), 0, pointsBuf, (bufferStart+1)*sizeof(float), sizeof(float) );
						Buffer.BlockCopy( BitConverter.GetBytes( vecPointCloud[i].z ), 0, pointsBuf, (bufferStart+2)*sizeof(float), sizeof(float) );

					}
				}

				UInt64 [] idsPointCloud = pointCloud.Identifiers;
				if (idsPointCloud != null && idsPointCloud.Length > 0)
				{
					idsBuf = new byte[idsPointCloud.Length * sizeof(ulong)];
					Buffer.BlockCopy( BitConverter.GetBytes( idsPointCloud[0] ), 0, idsBuf, 0, idsPointCloud.Length * sizeof(ulong) );
				}
			}
			
			return new serializablePointCloud(pointsBuf, idsBuf);
		}
		#else  //in editor
		public static implicit operator ARPointCloud (serializablePointCloud spc)
		{
			return new ARPointCloud(spc);
		}
		#endif
	};

	[Serializable]
	public class serializableARSessionConfiguration
	{
		public UnityARAlignment alignment; 
		public UnityARPlaneDetection planeDetection;
		public bool getPointCloudData;
		public bool enableLightEstimation;
		public bool enableAutoFocus;

		public serializableARSessionConfiguration(UnityARAlignment align, UnityARPlaneDetection planeDet, bool getPtCloud, bool enableLightEst, bool enableAutoFoc)
		{
			alignment = align;
			planeDetection = planeDet;
			getPointCloudData = getPtCloud;
			enableLightEstimation = enableLightEst;
			enableAutoFocus = enableAutoFoc;
		}

		public static implicit operator serializableARSessionConfiguration(ARKitWorldTrackingSessionConfiguration awtsc)
		{
			return new serializableARSessionConfiguration (awtsc.alignment, awtsc.planeDetection, awtsc.getPointCloudData, awtsc.enableLightEstimation, awtsc.enableAutoFocus);
		}

		public static implicit operator ARKitWorldTrackingSessionConfiguration (serializableARSessionConfiguration sasc)
		{
			return new ARKitWorldTrackingSessionConfiguration (sasc.alignment, sasc.planeDetection, sasc.getPointCloudData, sasc.enableLightEstimation, sasc.enableAutoFocus);
		}

		public static implicit operator ARKitFaceTrackingConfiguration (serializableARSessionConfiguration sasc)
		{
			return new ARKitFaceTrackingConfiguration (sasc.alignment, sasc.enableLightEstimation);
		}

	};

	[Serializable]
	public class serializableARKitInit
	{
		public serializableARSessionConfiguration config;
		public UnityARSessionRunOption runOption;

		public serializableARKitInit(serializableARSessionConfiguration cfg, UnityARSessionRunOption option)
		{
			config = cfg;
			runOption = option;
		}
	};

	[Serializable]
	public class serializableFromEditorMessage
	{
		public Guid subMessageId;
		public serializableARKitInit arkitConfigMsg;

	};
}