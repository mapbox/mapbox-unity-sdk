using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityARInterface
{
    public class ARRemoteVideo : MonoBehaviour
    {
        public Material clearMaterial;
        public Texture2D videoTextureY;
        public Texture2D videoTextureCbCr;

        private CommandBuffer m_VideoCommandBuffer;
		private bool m_CommandBufferInitialized;
		private Matrix4x4 m_DisplayTransform;


		public void Start()
		{
			m_CommandBufferInitialized = false;
		}

		void InitializeCommandBuffer()
		{
			m_VideoCommandBuffer = new CommandBuffer(); 
			m_VideoCommandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, clearMaterial);
			GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
			m_CommandBufferInitialized = true;
		}

		void OnDestroy()
		{
			GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
			m_CommandBufferInitialized = false;
		}

		public void UpdateDisplayTransform(Matrix4x4 displayMatrix)
		{
			m_DisplayTransform.SetColumn (0, displayMatrix.GetColumn (0));
			m_DisplayTransform.SetColumn (1, displayMatrix.GetColumn (1));
			m_DisplayTransform.SetColumn (2, displayMatrix.GetColumn (2));
			m_DisplayTransform.SetColumn (3, displayMatrix.GetColumn (3));

		}

		public void OnPreRender()
		{
			if (!m_CommandBufferInitialized)
				InitializeCommandBuffer ();

			clearMaterial.SetTexture("_textureY", videoTextureY);
			clearMaterial.SetTexture("_textureCbCr", videoTextureCbCr);
			clearMaterial.SetMatrix("_DisplayTransform", m_DisplayTransform);

		}
    }
}
