using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.iOS.Utils;


namespace UnityEngine.XR.iOS
{
    public class ARPointCloud 
    {
        IntPtr m_Ptr;
        Vector3[] m_Positions;
        UInt64[] m_Identifiers;

        internal IntPtr nativePtr { get { return m_Ptr; } }

        public int Count
        {
            get { return GetCount(); }
        }

        public Vector3[] Points 
        {
            get { return GetPoints(); }
        }

        public UInt64[] Identifiers
        {
            get { return GetIdentifiers();  }
        }

        internal static ARPointCloud FromPtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            return new ARPointCloud(ptr);
        }

        internal ARPointCloud(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentException("ptr may not be IntPtr.Zero");

            m_Ptr = ptr;
            GetPoints();
            GetIdentifiers();
        }
#if !UNITY_EDITOR && UNITY_IOS
        [DllImport("__Internal")]
        static extern int pointCloud_GetCount(IntPtr ptr);

        [DllImport("__Internal")]
        static extern IntPtr pointCloud_GetPointsPtr(IntPtr ptr);

        [DllImport("__Internal")]
        static extern IntPtr pointCloud_GetIdentifiersPtr(IntPtr ptr);
        
        int GetCount()
        {
            if (m_Positions != null)
            {
                return m_Positions.Length;
            }
            
            return pointCloud_GetCount(m_Ptr);
        }
    
        Vector3[] GetPoints()
        {
            if (m_Positions != null)
            {
                return m_Positions;
            }
            
            IntPtr pointsPtr = pointCloud_GetPointsPtr (m_Ptr);
            int pointCount = Count;
            if (pointCount <= 0 || pointsPtr == IntPtr.Zero) 
            {
                return null;
            }
            
            // Load the results into a managed array.
            var floatCount = pointCount * 4;
            float [] resultVertices = new float[floatCount];
            Marshal.Copy(pointsPtr, resultVertices, 0, floatCount);

            m_Positions = new Vector3[pointCount];

            for (int count = 0; count < pointCount; count++)
            {
                //convert to Unity coords system
                m_Positions[count].x = resultVertices[count * 4];
                m_Positions[count].y = resultVertices[count * 4 + 1];
                m_Positions[count].z = -resultVertices[count * 4 + 2];
            }

            return m_Positions;
        }

        UInt64[] GetIdentifiers()
        {
            if (m_Identifiers != null)
            {
                return m_Identifiers;
            }
            
            IntPtr identifiersPtr = pointCloud_GetIdentifiersPtr(m_Ptr);
            int identifiersCount = Count;
            if (identifiersCount <= 0 || identifiersPtr == IntPtr.Zero) 
            {
                return null;
            }

            // Load the results into a managed array.
            Int64 [] copyIdentifiers = new Int64[identifiersCount];
            Marshal.Copy(identifiersPtr, copyIdentifiers, 0, identifiersCount);

            m_Identifiers = new UInt64[identifiersCount];
            int index = 0;
            foreach (Int64 identifier in copyIdentifiers)
            {
                //convert to UInt64
                m_Identifiers[index++] = (UInt64) identifier;
            }

            return m_Identifiers;
        }
#else
        
        internal ARPointCloud(serializablePointCloud spc)
        {
            if (spc.pointCloudData != null) 
            {
                int numVectors = spc.pointCloudData.Length / (3 * sizeof(float));
                m_Positions = new Vector3[numVectors];
                for (int i = 0; i < numVectors; i++) 
                {
                    int bufferStart = i * 3;
                    m_Positions[i].x = BitConverter.ToSingle (spc.pointCloudData, (bufferStart) * sizeof(float));
                    m_Positions[i].y = BitConverter.ToSingle (spc.pointCloudData, (bufferStart+1) * sizeof(float));
                    m_Positions[i].z = BitConverter.ToSingle (spc.pointCloudData, (bufferStart+2) * sizeof(float));
                }
            } 
            else 
            {
                m_Positions = null;
            }

            if (spc.pointCloudIds != null)
            {
                int numIds = spc.pointCloudIds.Length / sizeof(UInt64);
                m_Identifiers = new ulong[numIds];
                for (int i = 0; i < numIds; i++)
                {
                    m_Identifiers[i] = BitConverter.ToUInt64(spc.pointCloudIds, i * sizeof(UInt64));
                }
            }
            else
            {
                m_Identifiers = null;
            }
        }

        int GetCount()
        {
            if (m_Positions == null)
            {
                return 0;
            }

            return m_Positions.Length;
        }

        Vector3[] GetPoints()
        {
            return m_Positions;
        }

        UInt64[] GetIdentifiers()
        {
            return m_Identifiers;
        }
#endif


    }
}