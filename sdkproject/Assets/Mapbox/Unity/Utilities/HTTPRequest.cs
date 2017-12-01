//-----------------------------------------------------------------------
// <copyright file="HTTPRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace Mapbox.Unity.Utilities
{
	using System;
	using System.Net;
	using UnityEngine.Networking;
	using System.Collections;
	using Mapbox.Platform;
	using UnityEngine;

	using System.Security.Cryptography.X509Certificates;
	using System.Net.Security;

#if UNITY_EDITOR
	using UnityEditor;
#endif

	internal sealed class HTTPRequest : IAsyncRequest
	{
		//private UnityWebRequest _request;
		private HttpWebRequest _request;
		private int _timeout;
		private readonly Action<Response> _callback;
		bool _wasCancelled;

		public bool IsCompleted { get; private set; }

		// TODO: simplify timeout for Unity 5.6+
		// https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest-timeout.html
		public HTTPRequest(string url, Action<Response> callback, int timeout)
		{
			//UnityEngine.Debug.Log("HTTPRequest: " + url);
			IsCompleted = false;
			_timeout = timeout;

			//_request = UnityWebRequest.Get(url);
			WebProxy aProxy = (WebProxy)WebRequest.DefaultWebProxy;
			//Debug.Log("HTTP Request Proxy: " + aProxy.Address);
			_request = (HttpWebRequest)WebRequest.Create(url);
			_request.Proxy = aProxy;
			_request.Method = "GET";

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

		public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
		{
			bool isOk = true;

			// If there are errors in the certificate chain, look at each error to determine the cause.
			if (sslPolicyErrors != SslPolicyErrors.None) 
			{
				for (int i=0; i<chain.ChainStatus.Length; i++) 
				{
					if (chain.ChainStatus [i].Status != X509ChainStatusFlags.RevocationStatusUnknown) 
					{
						chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
						chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
						chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
						chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
						bool chainIsValid = chain.Build ((X509Certificate2)certificate);

						if (!chainIsValid) 
						{
							isOk = false;
						}
					}
				}
			}

			return isOk;
		}

		private IEnumerator DoRequest()
		{
			//ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
			ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
			HttpWebResponse http_response = (HttpWebResponse)_request.GetResponse();

			/*_request.Send();

			DateTime timeout = DateTime.Now.AddSeconds(_timeout);
			bool didTimeout = false;

			while (!_request.isDone)
			{
				yield return null;
				if (DateTime.Now > timeout)
				{
					_request.Abort();
					didTimeout = true;
					break;
				}
			}

			Response response;
			if (didTimeout)
			{
				response = Response.FromWebResponse(this, _request, new Exception("Request Timed Out"));
			}
			else
			{
				response = Response.FromWebResponse(this, _request, _wasCancelled ? new Exception("Request Cancelled") : null);
			}*/
			Response response;
			response = Response.FromWebResponse(this, http_response, _wasCancelled ? new Exception("Request Cancelled") : null);

			_callback(response);
			//_request.Dispose();
			_request = null;
			IsCompleted = true;

			yield return null;
		}
	}
}
