using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
    public class ARAnchor : ARBase
    {
        [HideInInspector]
        public string anchorID;

        private ARInterface m_ARInterface;

        private void Awake()
        {
            m_ARInterface = ARInterface.GetInterface();
            if (m_ARInterface == null)
                Destroy(this);
        }

        void Start()
        {
            UpdateAnchor();
        }

        public void UpdateAnchor()
        {
            m_ARInterface.DestroyAnchor(this);
            m_ARInterface.ApplyAnchor(this);
        }

        private void OnDestroy()
        {
            if (m_ARInterface != null && !string.IsNullOrEmpty(anchorID))
            {
                m_ARInterface.DestroyAnchor(this);
            }
        }

    }
}