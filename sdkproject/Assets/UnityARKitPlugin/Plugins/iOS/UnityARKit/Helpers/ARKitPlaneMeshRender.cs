using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class ARKitPlaneMeshRender : MonoBehaviour {

	[SerializeField]
	private MeshFilter meshFilter;
	[SerializeField]
	private LineRenderer lineRenderer;
	private Mesh planeMesh;

	public void InitiliazeMesh(ARPlaneAnchor arPlaneAnchor)
	{
		planeMesh = new Mesh ();
		UpdateMesh (arPlaneAnchor);
		meshFilter.mesh = planeMesh;

	}

	public void UpdateMesh(ARPlaneAnchor arPlaneAnchor)
	{
        if (UnityARSessionNativeInterface.IsARKit_1_5_Supported()) //otherwise we cannot access planeGeometry
        {
	        if (arPlaneAnchor.planeGeometry.vertices.Length != planeMesh.vertices.Length || 
	            arPlaneAnchor.planeGeometry.textureCoordinates.Length != planeMesh.uv.Length ||
	            arPlaneAnchor.planeGeometry.triangleIndices.Length != planeMesh.triangles.Length)
	        {
		        planeMesh.Clear();
	        }
	        
            planeMesh.vertices = arPlaneAnchor.planeGeometry.vertices;
            planeMesh.uv = arPlaneAnchor.planeGeometry.textureCoordinates;
            planeMesh.triangles = arPlaneAnchor.planeGeometry.triangleIndices;

            lineRenderer.positionCount = arPlaneAnchor.planeGeometry.boundaryVertexCount;
            lineRenderer.SetPositions(arPlaneAnchor.planeGeometry.boundaryVertices);

            // Assign the mesh object and update it.
            planeMesh.RecalculateBounds();
            planeMesh.RecalculateNormals();
        }

	}

	void PrintOutMesh()
	{
		string outputMessage = "\n";
		outputMessage += "Vertices = " + planeMesh.vertices.GetLength (0);
		outputMessage += "\nVertices = [";
		foreach (Vector3 v in planeMesh.vertices) {
			outputMessage += v.ToString ();
			outputMessage += ",";
		}
		outputMessage += "]\n Triangles = " + planeMesh.triangles.GetLength (0);
		outputMessage += "\n Triangles = [";
		foreach (int i in planeMesh.triangles) {
			outputMessage += i;
			outputMessage += ",";
		}
		outputMessage += "]\n";
		Debug.Log (outputMessage);

	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
