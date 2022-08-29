using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class PointCloudIdsExample : MonoBehaviour 
{
    bool frameUpdated;
    ulong[] m_PointCloudIdentifiers;
    HashSet<ulong> m_IdentifiersSeenSoFar;
    int m_ExistingIdsSeen;

    void Start () 
    {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
        frameUpdated = false;
        m_IdentifiersSeenSoFar = new HashSet<ulong>();
    }

    void OnGUI()
    {
        int seenThisFrame = (m_PointCloudIdentifiers != null) ? m_PointCloudIdentifiers.Length : 0;
        string formattedMessage = String.Format("{0} new/ {1} frame/ {2} seen", seenThisFrame-m_ExistingIdsSeen, seenThisFrame, m_IdentifiersSeenSoFar.Count );
        GUI.Label(new Rect(100, 100, 200, 40), formattedMessage);
    }
    
    public void ARFrameUpdated(UnityARCamera camera)
    {
        if (camera.pointCloud != null)
        {
            m_PointCloudIdentifiers = camera.pointCloud.Identifiers;
        }
        frameUpdated = true;
    }

    // Update is called once per frame
    void Update () 
    {
        if (frameUpdated)
        {
            m_ExistingIdsSeen = 0;
            if (m_PointCloudIdentifiers != null && m_PointCloudIdentifiers.Length > 0)
            {
                foreach (var currentPointId in m_PointCloudIdentifiers) 
                {
                    if (m_IdentifiersSeenSoFar.Contains(currentPointId))
                    {
                        m_ExistingIdsSeen++;
                    }
                    else
                    {
                        m_IdentifiersSeenSoFar.Add(currentPointId);
                    }
                }
            } 
            frameUpdated = false;
        }
    }
}
