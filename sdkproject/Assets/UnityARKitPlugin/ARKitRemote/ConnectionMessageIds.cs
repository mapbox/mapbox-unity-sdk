using System;

namespace UnityEngine.XR.iOS
{
	public static class ConnectionMessageIds
	{
		public static Guid fromEditorARKitSessionMsgId { get { return new Guid("523bb5dd-163b-4e5b-9271-d18a50e8897e"); } }
		public static Guid updateCameraFrameMsgId { get { return new Guid("6d8c39bf-279a-46cf-91f4-9827a44443af"); } }
		public static Guid addPlaneAnchorMsgeId { get { return new Guid("a435cdb9-fa85-4d3c-9d3f-57fa85f62da3"); } }
		public static Guid updatePlaneAnchorMsgeId { get { return new Guid("84d5ad8d-e7f9-432c-ae5d-40717790a12f"); } }
		public static Guid removePlaneAnchorMsgeId { get { return new Guid("b07750a2-8825-4e86-9483-0b22b07df800"); } }
		public static Guid screenCaptureYMsgId { get { return new Guid("25c3d26f-72c5-4f3e-9a1f-c8c9b859453b"); } }
		public static Guid screenCaptureUVMsgId { get { return new Guid("d7f4d3cd-2d12-4ab7-b755-932fe7ab744d"); } }
		public static Guid addFaceAnchorMsgeId { get { return new Guid("7d7531e9-28b8-40b3-9afd-b6e7baa8e630"); } }
		public static Guid updateFaceAnchorMsgeId { get { return new Guid("80880c6e-d3f5-449a-9c8b-55c95b188563"); } }
		public static Guid removeFaceAnchorMsgeId { get { return new Guid("ba429c59-067e-4548-ab01-d7129f060872"); } }
	};

	public static class SubMessageIds
	{
		public static Guid editorInitARKit { get { return new Guid("2e5d7c45-daef-474d-bf55-1f02f0a10b69"); } }
		public static Guid editorInitARKitFaceTracking { get { return new Guid("3e86ccf6-93c6-4b07-b78f-0a60f6ed4a7a"); } }
	};
}