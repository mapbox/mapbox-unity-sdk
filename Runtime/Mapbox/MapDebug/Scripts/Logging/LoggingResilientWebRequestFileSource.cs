using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

public class LoggingResilientWebRequestFileSource : IFileSource
	{
		private string _accessToken;
		private Func<string> _getMapsSkuToken;
		private Random _random;
		private float _minDelay = 200; //milliseconds
		private float _maxDelay = 5000;
		private HashSet<ResilientWebRequest> _activeRequests;

		public int MapboxImageRequestCount = 0;
		public int MapboxDataRequestCount = 0;
		public int AbortedCount = 0;
		public Dictionary<long, int> ResponseCodeCounts = new Dictionary<long, int>();
		public List<string> Logs = new List<string>();
		private string _logFormat = "{0,4} {1,6:0.000} - {2,6:0.000} : {3,40}     {4,2}{5,6}{6,6}      {7}";
		private Regex regex = new Regex(@"v4/([^/]+)/(\d+)/(\d+)/(\d+)");
		
		public LoggingResilientWebRequestFileSource(string accessToken, Func<string> getMapsSkuToken)
		{
			_accessToken = accessToken;
			_getMapsSkuToken = getMapsSkuToken;
			_random = new Random();
			_activeRequests = new HashSet<ResilientWebRequest>();
		}

		public virtual IWebRequest MapboxImageRequest(string uri, Action<WebRequestResponse> callback, string etag = "", int timeout = 10, bool isNonreadable = true)
		{
			var finalUrl = CreateFinalUrl(uri);
			var webRequest = new ResilientTextureRequest(finalUrl, isNonreadable, timeout, etag);
			Runnable.Run(FetchWebData(webRequest, callback));
			MapboxImageRequestCount++;
			return webRequest;
		}
		
		public virtual IWebRequest MapboxDataRequest(string uri, Action<WebRequestResponse> callback, string etag = "", int timeout = 10)
		{
			var finalUrl = CreateFinalUrl(uri);
			var webRequest = new ResilientWebRequest(finalUrl, timeout, etag);
			Runnable.Run(FetchWebData(webRequest, callback));
			MapboxDataRequestCount++;
			return webRequest;
		}

		public virtual IWebRequest CustomImageRequest(string uri, Action<WebRequestResponse> callback, string etag = null, int timeout = 10, bool isNonreadable = true)
		{
			var webRequest = new ResilientTextureRequest(uri, isNonreadable, timeout, etag);
			Runnable.Run(FetchWebData(webRequest, callback));
			return webRequest;
		}
		
		protected virtual IEnumerator FetchWebData(ResilientWebRequest webRequest, Action<WebRequestResponse> callback)
		{
			var start = Time.realtimeSinceStartup;
			//requests are getting lost, data fetching manager is filled with dead requests, callback not working

			_activeRequests.Add(webRequest);
			var response = new WebRequestResponse();
			var maxRetries = 5;
			for (int retries = 0; retries <= maxRetries; retries++)
			{
				using (webRequest.Ready())
				{
					yield return webRequest.SendWebRequest();

					if (webRequest.Core != null)
					{
						var end = Time.realtimeSinceStartup;
						var leftUri = webRequest.Core.uri.GetLeftPart(UriPartial.Path);
						var match = regex.Match(leftUri);
						Logs.Add(string.Format(_logFormat, webRequest.responseCode, start, end,
							match.Groups[1].Value,
							match.Groups[2].Value,
							match.Groups[3].Value,
							match.Groups[4].Value,
							leftUri));
					}
					

					_activeRequests.Remove(webRequest);
					if (webRequest.IsAborted)
					{
						AbortedCount++;
						response.AddException(new Exception("Web request aborted"));
						response.Result = WebResponseResult.Cancelled;
						callback(response);
						yield break;
					}
					
					while (webRequest.result == UnityWebRequest.Result.InProgress)
						yield return null;
					
					//if image requests returns 304, which means no-change, response 
					//doesn't contain image data and webRequest.error will not be null
					// if (!string.IsNullOrEmpty(webRequest.error))
					// 	Debug.Log(webRequest.error);

					if(!ResponseCodeCounts.ContainsKey(webRequest.responseCode))
						ResponseCodeCounts.Add(webRequest.responseCode, 0);
					ResponseCodeCounts[webRequest.responseCode]++;
					
					
					if (webRequest.result == UnityWebRequest.Result.Success)
					{
						//3dBuildings return 204 when tile is empty
						if (webRequest.responseCode == 200 || webRequest.responseCode == 204)
						{
							string eTag = webRequest.GetETag();
							DateTime expirationDate = webRequest.GetExpirationDate();

							response.Result = WebResponseResult.Success;
							response.StatusCode = webRequest.responseCode;
							response.ETag = eTag;
							response.ExpirationDate = expirationDate;
							response.Data = webRequest.downloadHandler.data;

							callback(response);
							yield break;
						}
						//vector 304s go here
						//raster 304 does into failed section below
						else if (webRequest.responseCode == 304) 
						{
							string eTag = webRequest.GetETag();
							DateTime expirationDate = webRequest.GetExpirationDate();

							response.Result = WebResponseResult.Success;
							response.StatusCode = webRequest.responseCode;
							response.ETag = eTag;
							response.ExpirationDate = expirationDate;

							callback(response);
							yield break;
						}
						//some services return 404 if there isn't data. vector in ocean is a 404
						else if (webRequest.responseCode == 404)
						{
							response.Result = WebResponseResult.NoData;
							callback(response);
							yield break;
						}
						Debug.Log("what else is here? " + response.StatusCode);
					}
					else if (webRequest.result == UnityWebRequest.Result.DataProcessingError)
					{
						//raster 304s goes here
						if (webRequest.responseCode == 304)
						{
							string eTag = webRequest.GetETag();
							DateTime expirationDate = webRequest.GetExpirationDate();
					
							response.Result = WebResponseResult.Success;
							response.StatusCode = webRequest.responseCode;
							response.ETag = eTag;
							response.ExpirationDate = expirationDate;
					
							callback(response);
							yield break;
						}
						else
						{
							Debug.Log("Data processing error but it's not 304? " + response.StatusCode);
						}
					}
					else if (webRequest.result == UnityWebRequest.Result.ConnectionError) //retry
					{
						if (retries < maxRetries)
						{
							response.Result = WebResponseResult.Retrying;
							response.IsRetrying = true;
							response.AddException(new Exception(string.Format("Try: {0} at {1} - UnityWebRequest.Result.ConnectionError", retries, Time.realtimeSinceStartup)));
							callback(response);
							yield return new WaitForSeconds(GetDelayTimeInSeconds(retries + 1));
						}
					}
					else
					{
						if (webRequest.result == UnityWebRequest.Result.ProtocolError && webRequest.responseCode == 404)
						{
							response.Result = WebResponseResult.NoData;
							callback(response);
							yield break;
						}
						
						response.Result = WebResponseResult.Failed;
						response.IsRetrying = false;
						response.AddException(new Exception(webRequest.error));
						callback(response);
						yield break;
					}
				}
			}
			Debug.Log(string.Format("All {0} retries for web request failed for {1}", maxRetries, webRequest.RawUri));
			response.IsRetrying = false;
			response.Result = WebResponseResult.Failed;
			response.AddException(new Exception(string.Format("All {0} retries for web request failed for {1}", maxRetries, webRequest.RawUri)));
			callback(response);
		}

		protected string CreateFinalUrl(string uri)
		{
			return MapboxApiUrls.AppendCredentials(uri, _accessToken, _getMapsSkuToken);
		}
		
		protected bool IsRetryable(ResilientWebRequest request)
		{
			return !request.IsAborted && request.result == UnityWebRequest.Result.ConnectionError;
		}
		
		protected float GetDelayTimeInSeconds(int retries)
		{
			int maxDelayMilliseconds = (int)Math.Min(_maxDelay, Math.Pow(2, retries) * _minDelay);
			float delayMilliseconds = _random.Next((int)_minDelay, maxDelayMilliseconds + 1);
			return delayMilliseconds / 1000;
		}

		public IAsyncRequest Request(string uri, Action<Response> callback, int timeout = 10)
		{
			var finalUrl = CreateFinalUrl(uri);
			return requestTileAndCache(finalUrl, timeout, callback);
		}

		private IAsyncRequest requestTileAndCache(string url, int timeout, Action<Response> callback)
		{
			return IAsyncRequestFactory.CreateRequest(
				url,
				(Response response) =>
				{
					// if the request was successful add tile to all caches
					if (response.HasError)
					{
						response.AddException(new Exception(response.ExceptionsAsString));
					}

					response.IsUpdate = true;

					if (callback != null)
					{
						callback(response);
					}
				}, timeout);
		}

		public void OnDestroy()
		{
			foreach (var request in _activeRequests)
			{
				request.Abort();
			}
		}
	}