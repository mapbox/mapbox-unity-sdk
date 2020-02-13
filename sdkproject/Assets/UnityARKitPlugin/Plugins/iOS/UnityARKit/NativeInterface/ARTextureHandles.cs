using System;
using UnityEngine.XR.iOS;

namespace UnityEngine.XR.iOS
{

    public class ARTextureHandles
    {
        public struct ARTextureHandlesStruct
        {
            // Native (Metal) texture handles for the device camera buffer
            public IntPtr textureY;
            public IntPtr textureCbCr;
        }

        private ARTextureHandlesStruct m_ARTextureHandlesStruct;
        public IntPtr TextureY
        {
            get { return m_ARTextureHandlesStruct.textureY; }
        }
        public IntPtr TextureCbCr
        {
            get { return m_ARTextureHandlesStruct.textureCbCr; }
        }

        public ARTextureHandles(ARTextureHandlesStruct arTextureHandlesStruct)
        {
            m_ARTextureHandlesStruct = arTextureHandlesStruct;
        }

#if !UNITY_EDITOR && UNITY_IOS
        ~ARTextureHandles()
        {
            UnityARSessionNativeInterface.ReleaseVideoTextureHandles(m_ARTextureHandlesStruct);
        }
#endif
        public bool IsNull()
        {
            return (m_ARTextureHandlesStruct.textureY == IntPtr.Zero) || (m_ARTextureHandlesStruct.textureCbCr == IntPtr.Zero);
        }


        // Disable the default and copy constructors because we are not currently tracking references of the Objective C handles in this case.
        private ARTextureHandles()
        {
            // This
            Debug.Assert(false, "should not call the default constructor for ARTextureHandles");
            m_ARTextureHandlesStruct = new ARTextureHandlesStruct { textureY = IntPtr.Zero, textureCbCr = IntPtr.Zero };
        }

        private ARTextureHandles(ARTextureHandles arTextureHandles)
        {
            Debug.Assert(false, "should not call the copy constructor for ARTextureHandles");
            m_ARTextureHandlesStruct = new ARTextureHandlesStruct { textureY = IntPtr.Zero, textureCbCr = IntPtr.Zero };
        }

    }
}

