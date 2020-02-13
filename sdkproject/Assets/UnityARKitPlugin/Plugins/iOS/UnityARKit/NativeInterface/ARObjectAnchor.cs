using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using AOT;


namespace UnityEngine.XR.iOS
{

	public struct UnityARObjectAnchorData 
	{

		public IntPtr ptrIdentifier;

		/**
	 		The transformation matrix that defines the anchor's rotation, translation and scale in world coordinates.
			 */
		public UnityARMatrix4x4 transform;

		public IntPtr referenceObjectNamePtr;

		public IntPtr referenceObjectPtr;

	};



	public class ARObjectAnchor {

		private UnityARObjectAnchorData objectAnchorData;

		public ARObjectAnchor (UnityARObjectAnchorData uiad)
		{
			objectAnchorData = uiad;
		}


		public string identifier { get { return Marshal.PtrToStringAuto(objectAnchorData.ptrIdentifier); } }

		public Matrix4x4 transform { 
			get { 
				Matrix4x4 matrix = new Matrix4x4 ();
				matrix.SetColumn (0, objectAnchorData.transform.column0);
				matrix.SetColumn (1, objectAnchorData.transform.column1);
				matrix.SetColumn (2, objectAnchorData.transform.column2);
				matrix.SetColumn (3, objectAnchorData.transform.column3);
				return matrix;
			}
		}

		public string referenceObjectName { get { return Marshal.PtrToStringAuto(objectAnchorData.referenceObjectNamePtr); } }

		public IntPtr referenceObjectPtr { get { return objectAnchorData.referenceObjectPtr; } }
	}

	public partial class UnityARSessionNativeInterface
	{
		// Object Anchors
		public delegate void ARObjectAnchorAdded(ARObjectAnchor anchorData);
		public static event ARObjectAnchorAdded ARObjectAnchorAddedEvent;

		public delegate void ARObjectAnchorUpdated(ARObjectAnchor anchorData);
		public static event ARObjectAnchorUpdated ARObjectAnchorUpdatedEvent;

		public delegate void ARObjectAnchorRemoved(ARObjectAnchor anchorData);
		public static event ARObjectAnchorRemoved ARObjectAnchorRemovedEvent;


		delegate void internal_ARObjectAnchorAdded(UnityARObjectAnchorData anchorData);
		delegate void internal_ARObjectAnchorUpdated(UnityARObjectAnchorData anchorData);
		delegate void internal_ARObjectAnchorRemoved(UnityARObjectAnchorData anchorData);

		#if !UNITY_EDITOR && UNITY_IOS

		#region Object Anchors
		[MonoPInvokeCallback(typeof(internal_ARObjectAnchorAdded))]
		static void _object_anchor_added(UnityARObjectAnchorData anchor)
		{
			if (ARObjectAnchorAddedEvent != null)
			{
				ARObjectAnchor arObjectAnchor = new ARObjectAnchor(anchor);
				ARObjectAnchorAddedEvent(arObjectAnchor);
			}
		}

		[MonoPInvokeCallback(typeof(internal_ARObjectAnchorUpdated))]
		static void _object_anchor_updated(UnityARObjectAnchorData anchor)
		{
			if (ARObjectAnchorUpdatedEvent != null)
			{
				ARObjectAnchor arObjectAnchor = new ARObjectAnchor(anchor);
				ARObjectAnchorUpdatedEvent(arObjectAnchor);
			}
		}

		[MonoPInvokeCallback(typeof(internal_ARObjectAnchorRemoved))]
		static void _object_anchor_removed(UnityARObjectAnchorData anchor)
		{
			if (ARObjectAnchorRemovedEvent != null)
			{
				ARObjectAnchor arObjectAnchor = new ARObjectAnchor(anchor);
				ARObjectAnchorRemovedEvent(arObjectAnchor);
			}
		}
		#endregion

		[DllImport("__Internal")]
		private static extern void session_SetObjectAnchorCallbacks(IntPtr nativeSession, internal_ARObjectAnchorAdded objectAnchorAddedCallback, 
		internal_ARObjectAnchorUpdated objectAnchorUpdatedCallback, 
		internal_ARObjectAnchorRemoved objectAnchorRemovedCallback);

		#endif //!UNITY_EDITOR && UNITY_IOS




	}

}
