using UnityEngine;

namespace UnityARInterface
{
    public class ARPointCloudVisualizer : ARBase
    {
        [SerializeField]
        private ParticleSystem m_PointCloudParticlePrefab;

        [SerializeField]
        private int m_MaxPointsToShow = 300;

        [SerializeField]
        private float m_ParticleSize = 1.0f;

        private ParticleSystem m_ParticleSystem;
        private ParticleSystem.Particle [] m_Particles;
        private ParticleSystem.Particle[] m_NoParticles;
        private ARInterface.PointCloud m_PointCloud;

        private void OnDisable()
        {
            m_ParticleSystem.SetParticles(m_NoParticles, 1);
        }

        // Use this for initialization
        void Start()
        {
            m_ParticleSystem = Instantiate(m_PointCloudParticlePrefab, GetRoot());
            m_NoParticles = new ParticleSystem.Particle[1];
            m_NoParticles[0].startSize = 0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (ARInterface.GetInterface().TryGetPointCloud(ref m_PointCloud))
            {
                var scale = GetScale();

                var numParticles = Mathf.Min(m_PointCloud.points.Count, m_MaxPointsToShow);
                if (m_Particles == null || m_Particles.Length != numParticles)
                    m_Particles = new ParticleSystem.Particle[numParticles];

                for (int i = 0; i < numParticles; ++i)
                {
                    m_Particles[i].position = m_PointCloud.points[i] * scale;
                    m_Particles[i].startColor = new Color(1.0f, 1.0f, 1.0f);
                    m_Particles[i].startSize = m_ParticleSize * scale;
                }

                m_ParticleSystem.SetParticles(m_Particles, numParticles);
            }
            else
            {
                m_ParticleSystem.SetParticles(m_NoParticles, 1);
            }
        }
    }
}
