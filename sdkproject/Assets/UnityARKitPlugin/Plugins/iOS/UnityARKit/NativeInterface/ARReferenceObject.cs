using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace UnityEngine.XR.iOS
{
    public class ARReferenceObject
    {
        IntPtr m_Ptr;

        internal IntPtr nativePtr { get { return m_Ptr; } }

        public bool Save(string path)
        {
            return referenceObject_ExportObjectToURL(m_Ptr, path);
		}

		public static ARReferenceObject Load(string path)
		{
			var ptr = referenceObject_InitWithArchiveUrl(path);
			if (ptr == IntPtr.Zero)
				return null;

			return new ARReferenceObject(ptr);
		}

		public static ARReferenceObject SerializeFromByteArray(byte[] mapByteArray)
		{
			long lengthBytes = mapByteArray.LongLength;
			GCHandle handle = GCHandle.Alloc (mapByteArray, GCHandleType.Pinned);
			IntPtr newMapPtr = referenceObject_SerializeFromByteArray(handle.AddrOfPinnedObject(), lengthBytes);
			handle.Free ();
			return new ARReferenceObject (newMapPtr);
		}

		public byte [] SerializeToByteArray()
		{
			byte[] referenceObjectByteArray = new byte[referenceObject_SerializedLength(m_Ptr)];
			GCHandle handle = GCHandle.Alloc (referenceObjectByteArray, GCHandleType.Pinned);
			referenceObject_SerializeToByteArray(m_Ptr,handle.AddrOfPinnedObject());
			handle.Free ();
			return referenceObjectByteArray;
		}

        public Vector3 center
        {
            get { return UnityARMatrixOps.GetPosition(referenceObject_GetCenter(m_Ptr)); }
        }

        public Vector3 extent
        {
            get { return referenceObject_GetExtent(m_Ptr); }
        }

        public string name
        {
            get { return referenceObject_GetName(m_Ptr); }
            set { referenceObject_SetName(m_Ptr, value); }
        }

		public ARPointCloud pointCloud 
		{
			get 
			{
				return ARPointCloud.FromPtr (referenceObject_GetPointCloud (m_Ptr));
			}
		}

        internal static ARReferenceObject FromPtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            return new ARReferenceObject(ptr);
        }

        internal ARReferenceObject(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentException("ptr may not be IntPtr.Zero");

            m_Ptr = ptr;
        }

#if !UNITY_EDITOR && UNITY_IOS
        [DllImport("__Internal")]
        static extern bool referenceObject_ExportObjectToURL(IntPtr ptr, string path);

		[DllImport("__Internal")]
		static extern IntPtr referenceObject_InitWithArchiveUrl(string path);

		[DllImport("__Internal")]
		static extern bool referenceObject_Save(IntPtr referenceObjectPtr, string path);

		[DllImport("__Internal")]
		static extern IntPtr referenceObject_Load(string path);

        [DllImport("__Internal")]
        static extern Vector3 referenceObject_GetCenter(IntPtr ptr);

        [DllImport("__Internal")]
        static extern Vector3 referenceObject_GetExtent(IntPtr ptr);

        [DllImport("__Internal")]
        static extern string referenceObject_GetName(IntPtr ptr);

        [DllImport("__Internal")]
        static extern void referenceObject_SetName(IntPtr ptr, string name);

		[DllImport("__Internal")]
		static extern IntPtr referenceObject_GetPointCloud(IntPtr ptr);

		[DllImport("__Internal")]
		static extern long referenceObject_SerializedLength(IntPtr worldMapPtr);

		[DllImport("__Internal")]
		static extern void referenceObject_SerializeToByteArray(IntPtr worldMapPtr, IntPtr serByteArray);

		[DllImport("__Internal")]
		static extern IntPtr referenceObject_SerializeFromByteArray(IntPtr serByteArray, long lengthBytes);

#else
        static bool referenceObject_ExportObjectToURL(IntPtr ptr, string path) { return false; }
		static bool referenceObject_Save(IntPtr referenceObjectPtr, string path) { return false; }
		static IntPtr referenceObject_Load(string path) { return IntPtr.Zero; }
		static IntPtr referenceObject_InitWithArchiveUrl(string path) { return IntPtr.Zero; }
        static Vector3 referenceObject_GetCenter(IntPtr ptr) { return Vector3.zero; }
        static Vector3 referenceObject_GetExtent(IntPtr ptr) { return Vector3.zero; }
        static string referenceObject_GetName(IntPtr ptr) { return ""; }
        static void referenceObject_SetName(IntPtr ptr, string name) {}
		static IntPtr referenceObject_GetPointCloud(IntPtr ptr) { return IntPtr.Zero; }
		static long  referenceObject_SerializedLength(IntPtr worldMapPtr) { return 0; }
		static void referenceObject_SerializeToByteArray(IntPtr worldMapPtr, IntPtr serByteArray) { }
		static IntPtr referenceObject_SerializeFromByteArray(IntPtr serByteArray, long lengthBytes) { return IntPtr.Zero; }
#endif
    }



	public struct ARKitObjectScanningSessionConfiguration
	{
		public UnityARAlignment alignment;
		public UnityARPlaneDetection planeDetection;
		public bool getPointCloudData;
		public bool enableLightEstimation;
		public bool enableAutoFocus;
		public bool IsSupported { get { return IsARKitObjectScanningConfigurationSupported(); } private set { } }

		public ARKitObjectScanningSessionConfiguration(UnityARAlignment alignment = UnityARAlignment.UnityARAlignmentGravity,
			UnityARPlaneDetection planeDetection = UnityARPlaneDetection.None, bool getPointCloudData = false,
			bool enableLightEstimation = false, bool enableAutoFocus = false)
		{
			this.alignment = alignment;
			this.planeDetection = planeDetection;
			this.getPointCloudData = getPointCloudData;
			this.enableLightEstimation = enableLightEstimation;
			this.enableAutoFocus = enableAutoFocus;
		}

		#if UNITY_EDITOR || !UNITY_IOS
		private bool IsARKitObjectScanningConfigurationSupported()
		{
			return true;
		}
		#else
		[DllImport("__Internal")]
		private static extern bool IsARKitObjectScanningConfigurationSupported();
		#endif

	}

/// <summary>
/// Unity AR session native interface.
/// </summary>

	public partial class UnityARSessionNativeInterface
	{
		public void RunWithConfigAndOptions(ARKitObjectScanningSessionConfiguration config, UnityARSessionRunOption runOptions)
		{
			#if !UNITY_EDITOR && UNITY_IOS
			StartObjectScanningSessionWithOptions (m_NativeARSession, config, runOptions);
			#endif
		}

		public void RunWithConfig(ARKitObjectScanningSessionConfiguration config)
		{
			RunWithConfigAndOptions (config, 0);
		}

		public IntPtr CreateNativeReferenceObjectsSet(List<ARReferenceObject> refObjects)
		{
			if (IsARKit_2_0_Supported() == false) return IntPtr.Zero;
			
			IntPtr refObjectsSet = referenceObjectsSet_Create ();
			foreach (ARReferenceObject arro in refObjects) 
			{
				referenceObjectsSet_AddReferenceObject (refObjectsSet, arro.nativePtr);
			}
			return refObjectsSet;
		}

#if !UNITY_EDITOR && UNITY_IOS
	[DllImport("__Internal")]
	private static extern void StartObjectScanningSessionWithOptions(IntPtr nativeSession, ARKitObjectScanningSessionConfiguration configuration, UnityARSessionRunOption runOptions);

	[DllImport("__Internal")]
	static extern IntPtr referenceObjectsSet_Create();

	[DllImport("__Internal")]
	static extern void referenceObjectsSet_AddReferenceObject(IntPtr roSet, IntPtr referenceObject);
#else
	static IntPtr referenceObjectsSet_Create() { return IntPtr.Zero; }
	static void referenceObjectsSet_AddReferenceObject(IntPtr roSet, IntPtr referenceObject) {}
#endif
	}

	[Serializable]
	public class serializableARReferenceObject
	{
		byte [] arReferenceObjectData;

		public serializableARReferenceObject(byte [] inputObjectData)
		{
			arReferenceObjectData = inputObjectData;
		}

		public static implicit operator serializableARReferenceObject(ARReferenceObject arReferenceObject)
		{
			if (arReferenceObject != null) 
			{
				return new serializableARReferenceObject (arReferenceObject.SerializeToByteArray ());

			} 
			else 
			{
				return new serializableARReferenceObject (null);
			}
		}

		public static implicit operator ARReferenceObject(serializableARReferenceObject serReferenceObject)
		{
			if (serReferenceObject != null)
			{
				return ARReferenceObject.SerializeFromByteArray (serReferenceObject.arReferenceObjectData);
			}
			else
			{
				return null;
			}
		}

	}



}
