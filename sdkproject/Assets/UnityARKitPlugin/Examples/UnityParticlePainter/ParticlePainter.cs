using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class ParticlePainter : MonoBehaviour {
    public ParticleSystem painterParticlePrefab;
    public float minDistanceThreshold;
    public float maxDistanceThreshold;
    private bool frameUpdated = false;
    public float particleSize = .1f;
    public float penDistance = 0.2f;
    public ColorPicker colorPicker;
    private ParticleSystem currentPS;
    private ParticleSystem.Particle [] particles;
    private Vector3 previousPosition = Vector3.zero;  //camera starts from origin
    private List<Vector3> currentPaintVertices;
    private Color currentColor = Color.white;
    private List<ParticleSystem> paintSystems;
    private int paintMode = 0;  //0 = off, 1 = pick color, 2 = paint

	// Use this for initialization
	void Start () {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
        currentPS = Instantiate (painterParticlePrefab);
        currentPaintVertices = new List<Vector3> ();
        paintSystems = new List<ParticleSystem> ();
        frameUpdated = false;
        colorPicker.onValueChanged.AddListener( newColor => currentColor = newColor);
        colorPicker.gameObject.SetActive (false);
	}

    public void ARFrameUpdated(UnityARCamera camera)
    {
        Matrix4x4 matrix = new Matrix4x4();
        matrix.SetColumn(3, camera.worldTransform.column3);
      
        Vector3 currentPositon = UnityARMatrixOps.GetPosition(matrix) + (Camera.main.transform.forward * penDistance);
        if (Vector3.Distance (currentPositon, previousPosition) > minDistanceThreshold) {
            if (paintMode == 2) currentPaintVertices.Add (currentPositon);
            frameUpdated = true;
            previousPosition = currentPositon;
        }
    }

    void OnGUI()
    {
        string modeString = paintMode == 0 ? "OFF" : (paintMode == 1 ? "PICK" : "PAINT");
        if (GUI.Button(new Rect(Screen.width -100.0f, 0.0f, 100.0f, 50.0f), modeString))
         {
            paintMode = (paintMode + 1) % 3;
            colorPicker.gameObject.SetActive (paintMode == 1);
            if (paintMode == 2)
                RestartPainting ();
         }
        
    }
	
    void RestartPainting()
    {
        paintSystems.Add (currentPS);
        currentPS = Instantiate (painterParticlePrefab);
        currentPaintVertices = new List<Vector3> ();
    }

	// Update is called once per frame
	void Update () {
        if (frameUpdated && paintMode == 2) {
            if ( currentPaintVertices.Count > 0) {
                int numParticles = currentPaintVertices.Count;
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[numParticles];
                int index = 0;
                foreach (Vector3 currentPoint in currentPaintVertices) {     
                    particles [index].position = currentPoint;
                    particles [index].startColor = currentColor;
                    particles [index].startSize = particleSize;
                    index++;
                }
                currentPS.SetParticles (particles, numParticles);
            } else {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
                particles [0].startSize = 0.0f;
                currentPS.SetParticles (particles, 1);
            }
            frameUpdated = false;
        }
	}
}
