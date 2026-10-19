using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.TileJSON;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.BaseModule.Data.DataFetchers
{
	public class DataFetchingManager : IFileSource
	{
		/// <summary>
		/// A fetch command in queue is about the get started.
		/// </summary>
		public Action<FetchInfo> FetchInitialized = (t)=> {};
		
		/// <summary>
		/// This doesn't mean success or failure, it shows a fetch command fired from queue
		/// has finalized. Can be success, error, cancellation.
		/// Tile object inside the attached FetchInfo contains the details.
		/// </summary>
		public event Action<FetchInfo> FetchFinished = (t)=> {};
		
		/// <summary>
		/// This is an event for when data fetching command is removed from queue.
		/// Command may have been cancelled much earlier but getting removed from queue (this event)
		/// much later. This shouldn't be used for attaching logic to data fetching cancellation
		/// </summary>
		public event Action<FetchInfo> FetchCancelled = (t)=> {};
		
		protected float _requestDelay = 0.2f;
		protected IFileSource _fileSource;
		protected Queue<FetchInfo> _fetchQueue;
		protected HashSet<FetchInfo> _globalActiveRequests;
		protected int _activeRequestLimit = 30;
		private bool _isDestroying = false;

		public DataFetchingManager(string getAccessToken, Func<string> getSkuToken)
		{
			_fileSource = new ResilientWebRequestFileSource(getAccessToken, getSkuToken);
			_fetchQueue = new Queue<FetchInfo>();
			_globalActiveRequests = new HashSet<FetchInfo>();
			Runnable.Run(UpdateTick());
		}

		public virtual void EnqueueForFetching(FetchInfo info)
		{
			if (info == null) return;

			// Nothing will service this once teardown has started, and requests already in flight
			// when the app stops keep delivering callbacks that enqueue more work. The callback is
			// deliberately not invoked here: it would run against objects being destroyed, and
			// anything waiting on it is a coroutine that dies with the app anyway.
			if (_isDestroying || _fetchQueue == null) return;

			info.QueueTime = Time.time;
			_fetchQueue.Enqueue(info);
		}

		public virtual TileJSON GetTileJSON(int timeout = 10)
		{
			return new TileJSON(_fileSource, timeout);
		}
		
		public void OnDestroy()
		{
			_isDestroying = true;

			// Emptied but not nulled. Setting the field to null turned every late enqueue into a
			// NullReferenceException instead of a no-op, and the queue is structural rather than
			// per-session state.
			_fetchQueue?.Clear();

			_fileSource?.OnDestroy();
			_fileSource = null;
		}
		
		private IEnumerator UpdateTick()
		{
			while (true)
			{
				while (!_isDestroying && _fetchQueue.Count > 0 &&
				       _globalActiveRequests.Count < _activeRequestLimit)
				{
					var info = _fetchQueue.Peek(); //we just peek first as we might want to hold it until delay timer runs out
					if (info.Tile.CurrentTileState == TileState.Canceled)
					{
						_fetchQueue.Dequeue();
						FetchCancelled(info);
						info.Callback(new DataFetchingResult() { State = WebResponseResult.Cancelled });
						continue;
					}

					if (QueueTimeHasMatured(info.QueueTime, _requestDelay) || !Application.isPlaying)
					{
						_fetchQueue.Dequeue();
						_globalActiveRequests.Add(info);
						FetchInitialized(info);
						info.Tile.Initialize(
							_fileSource,
							(dataFetchingResult) =>
							{
								_globalActiveRequests.Remove(info);
								info.Callback(dataFetchingResult);
								FetchFinished?.Invoke(info);
							});
						yield return null;
					}
					else
					{
						yield return null;
					}
				}
				yield return null;
			}
		}

		private static bool QueueTimeHasMatured(float queueTime, float maturationAge)
		{
			return Time.time - queueTime >= maturationAge;
		}
		


		#region IFileSource interface for direct access without queue
		public IAsyncRequest Request(string uri, Action<Response> callback, int timeout = 10)
		{
			return _fileSource.Request(uri, callback, timeout);
		}

		public IWebRequest MapboxImageRequest(string uri, Action<WebRequestResponse> callback, string etag = "", int timeout = 10,
			bool isNonReadable = true)
		{
			return _fileSource.MapboxImageRequest(uri, callback, etag, timeout, isNonReadable);	
		}

		public IWebRequest CustomImageRequest(string uri, Action<WebRequestResponse> callback, string etag = null, int timeout = 10,
			bool isNonReadable = true)
		{
			return _fileSource.CustomImageRequest(uri, callback, etag, timeout, isNonReadable);
		}

		public IWebRequest MapboxDataRequest(string uri, Action<WebRequestResponse> callback, string etag = "", int timeout = 10)
		{
			return _fileSource.MapboxDataRequest(uri, callback, etag, timeout);
		}
		#endregion
	}
}
