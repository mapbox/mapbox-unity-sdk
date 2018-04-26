using UnityEngine;

namespace UnityARInterface
{
    public class ARAnchor : ARBase
    {
        [HideInInspector]
        public string anchorID;

        private ARInterface m_ARInterface;
        private bool started;

        private void Awake()
        {
            m_ARInterface = ARInterface.GetInterface();
            if (m_ARInterface == null)
                Destroy(this);
        }

        void Start()
        {
            UpdateAnchor();
            started = true;
        }

        private void OnEnable()
        {
            if (started)
                UpdateAnchor();
        }

        private void OnDisable()
        {
            m_ARInterface.DestroyAnchor(this);
        }

        private void OnDestroy()
        {
            m_ARInterface.DestroyAnchor(this);
        }

        public void UpdateAnchor()
        {
            m_ARInterface.DestroyAnchor(this);
            m_ARInterface.ApplyAnchor(this);
        }

    }
}