using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using AOT;


namespace UnityEngine.XR.iOS
{

	public enum UnityAREnvironmentTexturing
	{
		UnityAREnvironmentTexturingNone,
		UnityAREnvironmentTexturingManual,
		UnityAREnvironmentTexturingAutomatic
	};

   public enum UnityAREnvironmentTextureFormat : long
   {
       // NOTE: Not a complete set, but an initial mapping that matches an internal set of texture readback mappings.
       UnityAREnvironmentTextureFormatR16,
       UnityAREnvironmentTextureFormatRG16,
       UnityAREnvironmentTextureFormatBGRA32,
       UnityAREnvironmentTextureFormatRGBA32,
       UnityAREnvironmentTextureFormatRGBAFloat,
       UnityAREnvironmentTextureFormatRGBAHalf,
       UnityAREnvironmentTextureFormatDefault = UnityAREnvironmentTextureFormatBGRA32
   };

   public struct UnityAREnvironmentProbeCubemapData
   {
       public IntPtr cubemapPtr;
       public UnityAREnvironmentTextureFormat textureFormat;
       public int width;
       public int height;
       public int mipmapCount;
   };

	public struct UnityAREnvironmentProbeAnchorData 
	{

		public IntPtr ptrIdentifier;

		/**
	 		The transformation matrix that defines the anchor's rotation, translation and scale in world coordinates.
			 */
		public UnityARMatrix4x4 transform;

		public UnityAREnvironmentProbeCubemapData cubemapData;

		public Vector3 probeExtent;

	};

    public static class UnityAREnvironmentProbeCubemapDataMethods
    {
        public static TextureFormat GetTextureFormat(this UnityAREnvironmentTextureFormat unityAREnvironmentTextureFormat)
        {
            switch (unityAREnvironmentTextureFormat)
            {
                case UnityAREnvironmentTextureFormat.UnityAREnvironmentTextureFormatR16:
                    return TextureFormat.R16;
                case UnityAREnvironmentTextureFormat.UnityAREnvironmentTextureFormatRG16:
                    return TextureFormat.RG16;
                case UnityAREnvironmentTextureFormat.UnityAREnvironmentTextureFormatRGBA32:
                    return TextureFormat.RGBA32;
                case UnityAREnvironmentTextureFormat.UnityAREnvironmentTextureFormatRGBAFloat:
                    return TextureFormat.RGBAFloat;
                case UnityAREnvironmentTextureFormat.UnityAREnvironmentTextureFormatRGBAHalf:
                    return TextureFormat.RGBAHalf;
                case UnityAREnvironmentTextureFormat.UnityAREnvironmentTextureFormatBGRA32:
                default:
                    return TextureFormat.BGRA32;
            }
        }
    }

	public class AREnvironmentProbeAnchor 
	{

		UnityAREnvironmentProbeAnchorData envProbeAnchorData;

		public AREnvironmentProbeAnchor(UnityAREnvironmentProbeAnchorData uarepad)
		{
			envProbeAnchorData = uarepad;
		}

		public string identifier { get { return Marshal.PtrToStringAuto(envProbeAnchorData.ptrIdentifier); } }

		public Matrix4x4 transform { 
			get { 
				Matrix4x4 matrix = new Matrix4x4 ();
				matrix.SetColumn (0, envProbeAnchorData.transform.column0);
				matrix.SetColumn (1, envProbeAnchorData.transform.column1);
				matrix.SetColumn (2, envProbeAnchorData.transform.column2);
				matrix.SetColumn (3, envProbeAnchorData.transform.column3);
				return matrix;
			}
		}

		public Cubemap Cubemap
		{
			get 
			{ 
                if (envProbeAnchorData.cubemapData.cubemapPtr != IntPtr.Zero) {
                    Cubemap cubemap = Cubemap.CreateExternalTexture(envProbeAnchorData.cubemapData.width,
                                                                    envProbeAnchorData.cubemapData.textureFormat.GetTextureFormat(),
                                                                    (envProbeAnchorData.cubemapData.mipmapCount > 1),
                                                                    envProbeAnchorData.cubemapData.cubemapPtr);
                    cubemap.filterMode = FilterMode.Trilinear;
                    return cubemap;
				} else {
					return null;
				}
			}

		}

		public Vector3 Extent
		{
			get { return envProbeAnchorData.probeExtent; }
		}
	}


	public partial class UnityARSessionNativeInterface
	{
		// Object Anchors
		public delegate void AREnvironmentProbeAnchorAdded(AREnvironmentProbeAnchor anchorData);
		public static event AREnvironmentProbeAnchorAdded AREnvironmentProbeAnchorAddedEvent;

		public delegate void AREnvironmentProbeAnchorUpdated(AREnvironmentProbeAnchor anchorData);
		public static event AREnvironmentProbeAnchorUpdated AREnvironmentProbeAnchorUpdatedEvent;

		public delegate void AREnvironmentProbeAnchorRemoved(AREnvironmentProbeAnchor anchorData);
		public static event AREnvironmentProbeAnchorRemoved AREnvironmentProbeAnchorRemovedEvent;


		delegate void internal_AREnvironmentProbeAnchorAdded(UnityAREnvironmentProbeAnchorData anchorData);
		delegate void internal_AREnvironmentProbeAnchorUpdated(UnityAREnvironmentProbeAnchorData anchorData);
		delegate void internal_AREnvironmentProbeAnchorRemoved(UnityAREnvironmentProbeAnchorData anchorData);

		public UnityAREnvironmentProbeAnchorData AddEnvironmentProbeAnchor(UnityAREnvironmentProbeAnchorData anchorData)
		{
			#if !UNITY_EDITOR && UNITY_IOS
			return SessionAddEnvironmentProbeAnchor(m_NativeARSession, anchorData);
			#else
			return new UnityAREnvironmentProbeAnchorData();
			#endif
		}


#if !UNITY_EDITOR && UNITY_IOS

		#region Environment Probe Anchors
		[MonoPInvokeCallback(typeof(internal_AREnvironmentProbeAnchorAdded))]
		static void _envprobe_anchor_added(UnityAREnvironmentProbeAnchorData anchor)
		{
			if (AREnvironmentProbeAnchorAddedEvent != null)
			{
				AREnvironmentProbeAnchor arEnvProbeAnchor = new AREnvironmentProbeAnchor(anchor);
				AREnvironmentProbeAnchorAddedEvent(arEnvProbeAnchor);
			}
		}

		[MonoPInvokeCallback(typeof(internal_AREnvironmentProbeAnchorUpdated))]
		static void _envprobe_anchor_updated(UnityAREnvironmentProbeAnchorData anchor)
		{
			if (AREnvironmentProbeAnchorUpdatedEvent != null)
			{
				AREnvironmentProbeAnchor arEnvProbeAnchor = new AREnvironmentProbeAnchor(anchor);
				AREnvironmentProbeAnchorUpdatedEvent(arEnvProbeAnchor);
			}
		}

		[MonoPInvokeCallback(typeof(internal_AREnvironmentProbeAnchorRemoved))]
		static void _envprobe_anchor_removed(UnityAREnvironmentProbeAnchorData anchor)
		{
			if (AREnvironmentProbeAnchorRemovedEvent != null)
			{
				AREnvironmentProbeAnchor arEnvProbeAnchor = new AREnvironmentProbeAnchor(anchor);
				AREnvironmentProbeAnchorRemovedEvent(arEnvProbeAnchor);
			}
		}
		#endregion

		[DllImport("__Internal")]
		private static extern void session_SetEnvironmentProbeAnchorCallbacks(IntPtr nativeSession, internal_AREnvironmentProbeAnchorAdded envprobeAnchorAddedCallback, 
		internal_AREnvironmentProbeAnchorUpdated envprobeAnchorUpdatedCallback, 
		internal_AREnvironmentProbeAnchorRemoved envprobeAnchorRemovedCallback);

		[DllImport("__Internal")]
		private static extern UnityAREnvironmentProbeAnchorData SessionAddEnvironmentProbeAnchor (IntPtr nativeSession, UnityAREnvironmentProbeAnchorData anchorData);


#endif //!UNITY_EDITOR && UNITY_IOS




	}
}