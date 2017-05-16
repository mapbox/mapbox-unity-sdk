//-----------------------------------------------------------------------
// <copyright file="HTTPRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
//     Based on http://stackoverflow.com/a/12606963 and http://wiki.unity3d.com/index.php/WebAsync
// </copyright>
//-----------------------------------------------------------------------

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_WP_8_1 || UNITY_WSA || UNITY_WEBGL || UNITY_IOS || UNITY_PS4 || UNITY_SAMSUNGTV || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS
#define UNITY
#endif

#if !UNITY

namespace Mapbox.Platform {


	using System;
	using System.Net;
#if !UNITY && !NETFX_CORE
	using System.Net.Cache;
#endif
	using System.IO;
	using System.Collections.Generic;
	using System.Threading;
	using System.ComponentModel;
	using Utils;
#if NETFX_CORE
	using System.Net.Http;
	using System.Linq;
#endif

	//using System.Windows.Threading;

	internal sealed class HTTPRequestThreaded : IAsyncRequest {


		public bool IsCompleted { get; private set; }


		private Action<Response> _callback;
#if !NETFX_CORE
		private HttpWebRequest _request;
#else
		private HttpClient _request;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
#endif
#if !UNITY
		private SynchronizationContext _sync = AsyncOperationManager.SynchronizationContext;
#endif
		private int _timeOut;
		private string _requestUrl;
		private readonly string _userAgent = "mapbox-sdk-cs";


		/// <summary>
		/// 
		/// </summary>
		/// <param name="url"></param>
		/// <param name="callback"></param>
		/// <param name="timeOut">seconds</param>
		public HTTPRequestThreaded(string url, Action<Response> callback, int timeOut = 10) {

			IsCompleted = false;
			_callback = callback;
			_timeOut = timeOut;
			_requestUrl = url;

			setupRequest();
			getResponseAsync(_request, EvaluateResponse);
		}


		private void setupRequest() {

#if !NETFX_CORE
			_request = WebRequest.Create(_requestUrl) as HttpWebRequest;
			_request.UserAgent = _userAgent;
			//_hwr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36";
			//_hwr.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
			_request.Credentials = CredentialCache.DefaultCredentials;
			_request.KeepAlive = true;
			_request.ProtocolVersion = HttpVersion.Version11; // improved performance

			// improved performance. 
			// ServicePointManager.DefaultConnectionLimit doesn't seem to change anything
			// set ConnectionLimit per request
			// https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest(v=vs.90).aspx#Remarks
			// use a value that is 12 times the number of CPUs on the local computer
			_request.ServicePoint.ConnectionLimit  = Environment.ProcessorCount * 6;

			_request.ServicePoint.UseNagleAlgorithm = true;
			_request.ServicePoint.Expect100Continue = false;
			_request.ServicePoint.MaxIdleTime = 2000;
			_request.Method = "GET";
			_request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
			_request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			//_hwr.Timeout = timeOut * 1000; doesn't work in async calls, see below

#else
			HttpClientHandler handler = new HttpClientHandler() {
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
				AllowAutoRedirect = true,
				UseDefaultCredentials = true

			};
			_request = new HttpClient(handler);
			_request.DefaultRequestHeaders.Add("User-Agent", _userAgent);
			_request.Timeout = TimeSpan.FromSeconds(_timeOut);

			// TODO: how to set ConnectionLimit? ServicePoint.ConnectionLimit doesn't seem to be available.
#endif

#if !UNITY && !NETFX_CORE
			// 'NoCacheNoStore' greatly reduced the number of faulty request
			// seems that .Net caching and Mapbox API don't play well together
			_request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
#endif
		}



#if NETFX_CORE

		private async void getResponseAsync(HttpClient request, Action<HttpResponseMessage, Exception> gotResponse) {

			// TODO: implement a strategy similar to the full .Net one to avoid blocking of 'GetAsync()'
			// see 'Remarks' https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=netcore-1.1#System_Net_Http_HttpClient_Timeout
			// "A Domain Name System (DNS) query may take up to 15 seconds to return or time out."

			HttpResponseMessage response = null;
			try {
				response = await request.GetAsync(_requestUrl, _cancellationTokenSource.Token);
				gotResponse(response, null);
			}
			catch (Exception ex) {
				gotResponse(response, ex);
			}

		}


		private async void EvaluateResponse(HttpResponseMessage apiResponse, Exception apiEx) {

			var response = await Response.FromWebResponse(this, apiResponse, apiEx);

			// post (async) callback back to the main/UI thread
			// Unity: SynchronizationContext doesn't do anything
			//        use the Dispatcher
#if !UNITY
			_sync.Post(delegate {
				_callback(response);
				IsCompleted = true;
				_callback = null;
#if NETFX_CORE
				if (null != _request) {
					_request.Dispose();
					_request = null;
				}
#endif
			}, null);
#else
			UnityToolbag.Dispatcher.InvokeAsync(() => {
				_callback(response);
				IsCompleted = true;
				_callback = null;
#if NETFX_CORE
				if (null != _request) {
					_request.Dispose();
					_request = null;
				}
#endif
			});
#endif
		}

#endif


#if !NETFX_CORE
		private void getResponseAsync(HttpWebRequest request, Action<HttpWebResponse, Exception> gotResponse) {

			// create an additional action wrapper, because of:
			// https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.begingetresponse.aspx
			// The BeginGetResponse method requires some synchronous setup tasks to complete (DNS resolution,
			//proxy detection, and TCP socket connection, for example) before this method becomes asynchronous.
			// As a result, this method should never be called on a user interface (UI) thread because it might
			// take considerable time(up to several minutes depending on network settings) to complete the
			// initial synchronous setup tasks before an exception for an error is thrown or the method succeeds.

			Action actionWrapper = () => {
				try {
					// BeginInvoke runs on a thread of the thread pool (!= main/UI thread)
					// that's why we need SynchronizationContext when 
					// TODO: how to influence threadpool: nr of threads etc.
					long startTicks = DateTime.Now.Ticks;
					request.BeginGetResponse((asycnResult) => {
						try { // there's a try/catch here because execution path is different from invokation one, exception here may cause a crash
							long beforeEndGet = DateTime.Now.Ticks;
							HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asycnResult);
							//long finished = DateTime.Now.Ticks;
							//long duration = finished - startTicks;
							//TimeSpan ts = TimeSpan.FromTicks(duration);
							//TimeSpan tsEndGet = TimeSpan.FromTicks(finished - beforeEndGet);
							//TimeSpan tsBeginGet = TimeSpan.FromTicks(beforeEndGet - startTicks);
							//UnityEngine.Debug.Log("received response - " + ts.Milliseconds + "ms" + " BeginGet: " + tsBeginGet.Milliseconds + " EndGet: " + tsEndGet.Milliseconds + " CompletedSynchronously: " + asycnResult.CompletedSynchronously);
							gotResponse(response, null);
						}
						// EndGetResponse() throws on on some status codes, try to get response anyway (and status codes)
						catch (WebException wex) {
							//another place to watchout for HttpWebRequest.Abort to occur
							if (wex.Status == WebExceptionStatus.RequestCanceled) {
								gotResponse(null, wex);
							} else {
								HttpWebResponse hwr = wex.Response as HttpWebResponse;
								if (null == hwr) {
									throw;
								}
								gotResponse(hwr, wex);
							}
						}
						catch (Exception ex) {
							gotResponse(null, ex);
						}
					}
					, null);
				}
				catch (Exception ex) {
					//catch exception from HttpWebRequest.Abort
					gotResponse(null, ex);
				}
			};

			try {
				actionWrapper.BeginInvoke(new AsyncCallback((iAsyncResult) => {
					var action = (Action)iAsyncResult.AsyncState;
					action.EndInvoke(iAsyncResult);
				})
				, actionWrapper);
			}
			catch (Exception ex) {
				gotResponse(null, ex);
			}
		}



		private void EvaluateResponse(HttpWebResponse apiResponse, Exception apiEx) {

			var response = Response.FromWebResponse(this, apiResponse,apiEx);

			// post (async) callback back to the main/UI thread
			// Unity: SynchronizationContext doesn't do anything
			//        use the Dispatcher
#if !UNITY
			_sync.Post(delegate {
				_callback(response);
				IsCompleted = true;
				_callback = null;
#if NETFX_CORE
				if (null != _request) {
					_request.Dispose();
					_request = null;
				}
#endif
			}, null);
#else
			// Unity is playing
			if (UnityToolbag.Dispatcher._instanceExists) {
				UnityToolbag.Dispatcher.InvokeAsync(() => {
					_callback(response);
					IsCompleted = true;
					_callback = null;
#if NETFX_CORE
					if (null != _request) {
						_request.Dispose();
						_request = null;
					}
#endif
				});
			} else { // Unity is in Edit Mode
#if UNITY_EDITOR
				Mapbox.Unity.DispatcherEditor.InvokeAsync(() => {
					_callback(response);
					IsCompleted = true;
					_callback = null;
#if NETFX_CORE
					if (null != _request) {
						_request.Dispose();
						_request = null;
					}
#endif
				});
#endif

			}
#endif
		}
#endif



		public void Cancel() {

#if !NETFX_CORE
			if (null != _request) {
				_request.Abort();
			}
#else
			_cancellationTokenSource.Cancel();
#endif
		}


	}
}


#endif