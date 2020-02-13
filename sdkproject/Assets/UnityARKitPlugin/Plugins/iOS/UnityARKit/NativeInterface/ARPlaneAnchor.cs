using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.iOS
{

	public struct UnityARPlaneGeometry
	{
		public int vertexCount;
		public IntPtr vertices;
		public int textureCoordinateCount;
		public IntPtr textureCoordinates;
		public int triangleCount;
		public IntPtr triangleIndices;
		public int boundaryVertexCount;
		public IntPtr boundaryVertices;

	}

	public struct UnityARAnchorData
	{
		public IntPtr ptrIdentifier;

		/**
 		The transformation matrix that defines the anchor's rotation, translation and scale in world coordinates.
		 */
		public UnityARMatrix4x4 transform;

		/**
		 The alignment of the plane.
		 */

		public ARPlaneAnchorAlignment alignment;

		/**
        The center of the plane in the anchor’s coordinate space.
        */

		public Vector4 center;

		/**
        The extent of the plane in the anchor’s coordinate space.
         */
		public Vector4 extent;

		/**
        The geometry that describes more accurately the surface found.
         */
		public UnityARPlaneGeometry planeGeometry;
	}


#if !UNITY_EDITOR
	public class ARPlaneGeometry
	{
		private UnityARPlaneGeometry uPlaneGeometry;

		public ARPlaneGeometry (UnityARPlaneGeometry upg)
		{
			uPlaneGeometry = upg;
		}

		public int vertexCount { get { return uPlaneGeometry.vertexCount; } }
		public int triangleCount {  get  { return uPlaneGeometry.triangleCount; } }
		public int textureCoordinateCount { get { return uPlaneGeometry.textureCoordinateCount; } }
		public int boundaryVertexCount { get { return uPlaneGeometry.boundaryVertexCount; } }

		public Vector3 [] vertices { get { return MarshalVertices(uPlaneGeometry.vertices,vertexCount); } }

		public Vector3 [] boundaryVertices { get { return MarshalVertices(uPlaneGeometry.boundaryVertices,boundaryVertexCount); } }

		public Vector2 [] textureCoordinates { get { return MarshalTexCoords(uPlaneGeometry.textureCoordinates, textureCoordinateCount); } }

		public int [] triangleIndices { get { return MarshalIndices(uPlaneGeometry.triangleIndices, triangleCount); } }

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

	public class ARPlaneAnchor 
	{
		private UnityARAnchorData planeAnchorData;

        public string identifierStr { get; }
        public string identifier { get { return identifierStr; } }

        public ARPlaneAnchor (UnityARAnchorData ufad)
        {
            planeAnchorData = ufad;
            identifierStr = Marshal.PtrToStringAuto(planeAnchorData.ptrIdentifier);
        }

		public Matrix4x4 transform { 
			get { 
				Matrix4x4 matrix = new Matrix4x4 ();
				matrix.SetColumn (0, planeAnchorData.transform.column0);
				matrix.SetColumn (1, planeAnchorData.transform.column1);
				matrix.SetColumn (2, planeAnchorData.transform.column2);
				matrix.SetColumn (3, planeAnchorData.transform.column3);
				return matrix;
			}
		}

		public ARPlaneAnchorAlignment alignment {
			get {
				return planeAnchorData.alignment;
			}
		}

		public Vector3 extent {
			get {
				return new Vector3 (planeAnchorData.extent.x, planeAnchorData.extent.y, planeAnchorData.extent.z);
			}
		}

		public Vector3 center {
			get {
				return new Vector3 (planeAnchorData.center.x, planeAnchorData.center.y, planeAnchorData.center.z);
			}
		}

		public ARPlaneGeometry planeGeometry { get { return new ARPlaneGeometry (planeAnchorData.planeGeometry);	} }

	}
#endif
}

