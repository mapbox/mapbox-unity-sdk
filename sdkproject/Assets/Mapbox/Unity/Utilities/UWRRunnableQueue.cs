
namespace Mapbox.Unity.Utilities
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Networking;
	using System.Collections;

#if UNITY_EDITOR
	using UnityEditor;
#endif
	internal class UWRRunnableQueue : MonoBehaviour
	{
		private const int REQUEST_COUNT = 10;

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