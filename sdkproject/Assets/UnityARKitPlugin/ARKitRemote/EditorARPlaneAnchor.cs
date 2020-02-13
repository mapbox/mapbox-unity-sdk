using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS.Utils;
using System.Text;

namespace UnityEngine.XR.iOS 
{
	#if UNITY_EDITOR
	public class ARPlaneGeometry
	{
		private serializablePlaneGeometry sPlaneGeometry;

		public ARPlaneGeometry (serializablePlaneGeometry ufg)
		{
			sPlaneGeometry = ufg;
		}

		public int vertexCount { get { return sPlaneGeometry.Vertices.Length; } }
		public int triangleCount {  get  { return sPlaneGeometry.TriangleIndices.Length; } }
		public int textureCoordinateCount { get { return sPlaneGeometry.TexCoords.Length; } }
		public int boundaryVertexCount { get { return sPlaneGeometry.BoundaryVertices.Length; } }

		public Vector3 [] vertices { get { return sPlaneGeometry.Vertices; } }

		public Vector2 [] textureCoordinates { get { return sPlaneGeometry.TexCoords; } }

		public int [] triangleIndices { get { return sPlaneGeometry.TriangleIndices; } }

		public Vector3 [] boundaryVertices { get { return sPlaneGeometry.BoundaryVertices; } }

	}

	public class ARPlaneAnchor 
	{
		serializableUnityARPlaneAnchor m_spa;

		public ARPlaneAnchor(serializableUnityARPlaneAnchor spa)
		{
			m_spa = spa;
		}

		public string identifier { get { return  Encoding.UTF8.GetString (m_spa.identifierStr); } }

		public Matrix4x4 transform { get { return m_spa.worldTransform; } }

		public ARPlaneGeometry planeGeometry { 
			get {
				return new ARPlaneGeometry (m_spa.planeGeometry);	
			} 
		}

		public ARPlaneAnchorAlignment alignment {
			get {
				return m_spa.planeAlignment;
			}
		}

		public Vector3 extent {
			get {
				return new Vector3 (m_spa.extent.x, m_spa.extent.y, m_spa.extent.z);
			}
		}

		public Vector3 center {
			get {
				return new Vector3 (m_spa.center.x, m_spa.center.y, m_spa.center.z);
			}
		}

	}
	#endif
}
