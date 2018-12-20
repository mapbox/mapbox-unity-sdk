//-----------------------------------------------------------------------
// <copyright file="HTTPRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Mapbox.Unity.Utilities
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Networking;
	using System.Collections;
	using Mapbox.Platform;

#if UNITY_EDITOR
	using UnityEditor;
	using System.Linq;

#endif

	public enum HttpRequestType
	{
		Get,
		Head
	}


	internal sealed class HTTPRequest : IAsyncRequest
	{
		private const int REQUEST_COUNT = 10;
		private UnityWebRequest _request;
		private HttpRequestType _requestType;
		private int _timeout;
		private readonly Action<Response> _callback;

		public bool IsCompleted { get; private set; }
		public bool IsCanceled { get; private set; }

		public HttpRequestType RequestType { get { return _requestType; } }


		/// <summary>
		///TODO UWRRunnableQueue should be outside of HTTPRequest;
		/// </summary>
		private static UWRRunnableQueue _queue;
		static HTTPRequest()
		{
			/// TODO reimplement.
			_queue = new GameObject("UWRRunnableQueue").AddComponent<UWRRunnableQueue>();
		}


		// TODO: simplify timeout for Unity 5.6+
		// https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest-timeout.html
		public HTTPRequest(string url, Action<Response> callback, int timeout, HttpRequestType requestType = HttpRequestType.Get)
		{
			IsCompleted = false;
			_requestType = requestType;

			switch (_requestType)
			{
				case HttpRequestType.Get:
					_request = UnityWebRequest.Get(url);
					break;
				case HttpRequestType.Head:
					_request = UnityWebRequest.Head(url);
					break;
				default:
					_request = UnityWebRequest.Get(url);
					break;
			}

			_request.timeout = timeout;
			_callback = callback;

			_queue.AddToRun(this);
		}

		public void Cancel()
		{
			if (_request != null)
			{
				_request.Abort();
			}
			IsCanceled = true;
		}

		public IEnumerator StartRequest()
		{

#if UNITY_EDITOR
			// otherwise requests don't work in Edit mode, eg geocoding
			// also lot of EditMode tests fail otherwise
#pragma warning disable 0618
			_request.SendWebRequest();
#pragma warning restore 0618
			while (!_request.isDone) { yield return null; }
#else
#pragma warning disable 0618
			yield return _request.Send();
#pragma warning restore 0618
#endif

			var response = Response.FromWebResponse(this, _request, null);
			_callback(response);
			_request.Dispose();
			_request = null;
			IsCompleted = true;
		}



		private Queue<HTTPRequest> _requests = new Queue<HTTPRequest>();
		private List<HTTPRequest> _runnableRequests = new List<HTTPRequest>();

		//avoiding to create a list object at every update.
		private List<HTTPRequest> _requestsToRemove = new List<HTTPRequest>();

		internal void AddToRun(HTTPRequest request)
		{
			_requests.Enqueue(request);
		}

		private void Update()
		{
			RemoveCompletedRequests();
			StartRequests(REQUEST_COUNT - _runnableRequests.Count);

		}

		private void RemoveCompletedRequests()
		{
			for (int i = 0; i < _runnableRequests.Count; i++)
			{
				var req = _runnableRequests[i];
				if (req.IsCompleted)
				{
					_requestsToRemove.Add(req);
				}
			}

			for (int i = 0; i < _requestsToRemove.Count; i++)
			{
				_runnableRequests.Remove(_requestsToRemove[i]);
			}
		}

		private void StartRequests(int requestsCount)
		{
			for (int i = requestsCount; i > 0; i--)
			{
				if (_requests.Count == 0)
					break;

				HTTPRequest requestToRun = _requests.Dequeue();
#if UNITY_EDITOR
				if (!EditorApplication.isPlaying)
				{
					Runnable.EnableRunnableInEditor();
				}
#endif
				Runnable.Run(requestToRun.StartRequest());
				_runnableRequests.Add(requestToRun);

			}
		}
	}
}
