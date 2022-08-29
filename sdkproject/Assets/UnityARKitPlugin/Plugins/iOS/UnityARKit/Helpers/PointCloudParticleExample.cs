using UnityEngine;
using UnityEngine.XR.iOS;

public class PointCloudParticleExample : MonoBehaviour 
{
    public ParticleSystem pointCloudParticlePrefab;
    public int maxPointsToShow;
    public float particleSize = 1.0f;
    Vector3[] m_PointCloudData;
    bool frameUpdated = false;
    ParticleSystem currentPS;
    ParticleSystem.Particle [] particles;

    // Use this for initialization
    void Start () 
    {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
        currentPS = Instantiate (pointCloudParticlePrefab);
        m_PointCloudData = null;
        frameUpdated = false;
    }
    
    public void ARFrameUpdated(UnityARCamera camera)
    {
        if (camera.pointCloud != null)
        {
           m_PointCloudData = camera.pointCloud.Points;
        }
        frameUpdated = true;
    }

    // Update is called once per frame
    void Update () 
    {
        if (frameUpdated) 
        {
            if (m_PointCloudData != null && m_PointCloudData.Length > 0 && maxPointsToShow > 0) 
            {
                int numParticles = Mathf.Min (m_PointCloudData.Length, maxPointsToShow);
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[numParticles];
                int index = 0;
                foreach (Vector3 currentPoint in m_PointCloudData) 
                {     
                    particles [index].position = currentPoint;
                    particles [index].startColor = new Color (1.0f, 1.0f, 1.0f);
                    particles [index].startSize = particleSize;
                    index++;
                    if (index >= numParticles) break;
                }
                currentPS.SetParticles (particles, numParticles);
            } 
            else 
            {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
                particles [0].startSize = 0.0f;
                currentPS.SetParticles (particles, 1);
            }
            frameUpdated = false;
        }
    }
}
