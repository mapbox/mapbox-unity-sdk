//-----------------------------------------------------------------------
// <copyright file="HTTPRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Mapbox.Unity.Utilities
{

	using System;
	using UnityEngine.Networking;
	using System.Collections;
	using Mapbox.Platform;

#if UNITY_EDITOR
	using UnityEditor;
#endif

	internal sealed class HTTPRequest : IAsyncRequest
	{
		private UnityWebRequest _request;
		private int _timeout;
		private readonly Action<Response> _callback;
		bool _wasCancelled;

		public bool IsCompleted { get; private set; }

		public HTTPRequest(string url, Action<Response> callback, int timeout = 10)
		{
			//UnityEngine.Debug.Log("HTTPRequest: " + url);
			IsCompleted = false;
			_timeout = timeout;
			_request = UnityWebRequest.Get(url);
			_callback = callback;

#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			{
				Runnable.EnableRunnableInEditor();
			}
#endif
			Runnable.Run(DoRequest());
		}

		public void Cancel()
		{
			_wasCancelled = true;

			if (_request != null)
			{
				_request.Abort();
			}
		}

		private IEnumerator DoRequest()
		{
			_request.Send();
			while (!_request.isDone)
			{
				yield return null;
			}

			var response = Response.FromWebResponse(this, _request, _wasCancelled ? new Exception("Request Cancelled") : null);

			_callback(response);
			_request.Dispose();
			_request = null;
			IsCompleted = true;
		}
	}
}
