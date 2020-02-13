using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class UnityARFaceMeshManager : MonoBehaviour {

	[SerializeField]
	private MeshFilter meshFilter;

	private UnityARSessionNativeInterface m_session;
	private Mesh faceMesh;

	// Use this for initialization
	void Start () {
		m_session = UnityARSessionNativeInterface.GetARSessionNativeInterface();

		Application.targetFrameRate = 60;
		ARKitFaceTrackingConfiguration config = new ARKitFaceTrackingConfiguration();
		config.alignment = UnityARAlignment.UnityARAlignmentGravity;
		config.enableLightEstimation = true;

		if (config.IsSupported && meshFilter != null) {
			
			m_session.RunWithConfig (config);

			UnityARSessionNativeInterface.ARFaceAnchorAddedEvent += FaceAdded;
			UnityARSessionNativeInterface.ARFaceAnchorUpdatedEvent += FaceUpdated;
			UnityARSessionNativeInterface.ARFaceAnchorRemovedEvent += FaceRemoved;

		}


	}

	void FaceAdded (ARFaceAnchor anchorData)
	{
		gameObject.transform.localPosition = UnityARMatrixOps.GetPosition (anchorData.transform);
		gameObject.transform.localRotation = UnityARMatrixOps.GetRotation (anchorData.transform);

		
		faceMesh = new Mesh ();
		faceMesh.vertices = anchorData.faceGeometry.vertices;
		faceMesh.uv = anchorData.faceGeometry.textureCoordinates;
		faceMesh.triangles = anchorData.faceGeometry.triangleIndices;

		// Assign the mesh object and update it.
		faceMesh.RecalculateBounds();
		faceMesh.RecalculateNormals();
		meshFilter.mesh = faceMesh;
	}

	void FaceUpdated (ARFaceAnchor anchorData)
	{
		if (faceMesh != null) {
			gameObject.transform.localPosition = UnityARMatrixOps.GetPosition (anchorData.transform);
			gameObject.transform.localRotation = UnityARMatrixOps.GetRotation (anchorData.transform);
			faceMesh.vertices = anchorData.faceGeometry.vertices;
			faceMesh.uv = anchorData.faceGeometry.textureCoordinates;
			faceMesh.triangles = anchorData.faceGeometry.triangleIndices;
			faceMesh.RecalculateBounds();
			faceMesh.RecalculateNormals();
		}

	}

	void FaceRemoved (ARFaceAnchor anchorData)
	{
		meshFilter.mesh = null;
		faceMesh = null;
	}



	// Update is called once per frame
	void Update () {
		
	}

	void OnDestroy()
	{
		
	}
}
