using System.Diagnostics;
using System.IO;
using Mapbox.Core.Platform.Cache;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Mapbox.Platform.Cache
{
	using System;
	using Mapbox.Platform;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Mapbox.Map;
	using System.Collections;
	using System.Linq;

	public class CachingWebFileSource : IFileSource, IDisposable
	{
		private bool _disposed;
		
		private string _accessToken;
		private Func<string> _getMapsSkuToken;

		public CachingWebFileSource(string accessToken, Func<string> getMapsSkuToken)
		{
			_accessToken = accessToken;
			_getMapsSkuToken = getMapsSkuToken;
		}

		#region idisposable

		~CachingWebFileSource()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!_disposed)
			{
				if (disposeManagedResources)
				{
//					for (int i = 0; i < _caches.Count; i++)
//					{
//						IDisposable cache = _caches[i] as IDisposable;
//						if (null != cache)
//						{
//							cache.Dispose();
//							cache = null;
//						}
//					}
				}

				_disposed = true;
			}
		}
		#endregion

		public UnityWebRequest MapboxImageRequest(string uri, Action<TextureResponse> callback, int timeout = 10, string etag = null, bool isNonreadable = true)
		{
			var finalUrl = CreateFinalUrl(uri);
			return CustomImageRequest(finalUrl, callback, timeout, etag, isNonreadable);
		}

		public UnityWebRequest CustomImageRequest(string uri, Action<TextureResponse> callback, int timeout = 10, string etag = null, bool isNonreadable = true)
		{
			var webRequest = UnityWebRequestTexture.GetTexture(uri, isNonreadable);
			webRequest.timeout = timeout;
			if (!string.IsNullOrEmpty(etag))
			{
				webRequest.SetRequestHeader("If-None-Match", etag);
			}

			Runnable.Run(FetchTexture(webRequest, callback));

			return webRequest;
		}

		private string CreateFinalUrl(string uri)
         		{
         			var uriBuilder = new UriBuilder(uri);
         			if (!string.IsNullOrEmpty(_accessToken))
         			{
         				string accessTokenQuery = "access_token=" + _accessToken;
         				string mapsSkuToken = "sku=" + _getMapsSkuToken();
         				if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
         				{
         					uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery + "&" + mapsSkuToken;
         				}
         				else
         				{
         					uriBuilder.Query = accessTokenQuery + "&" + mapsSkuToken;
         				}
         			}

         			string finalUrl = uriBuilder.ToString();
         			return finalUrl;
         		}

		private IEnumerator FetchTexture(UnityWebRequest webRequest, Action<TextureResponse> callback)
		{
			using (webRequest)
			{
				var response = new TextureResponse();
				yield return webRequest.SendWebRequest();

				if (webRequest != null && webRequest.isNetworkError || webRequest.isHttpError)
				{
					response.AddException(new Exception(webRequest.error));
				}
				else if (webRequest.responseCode == 304) // 304 NOT MODIFIED
				{
					Handle304(webRequest, response);
				}
				else
				{
					Handle200(webRequest, response);
				}

				callback(response);
			}
		}

		private void Handle200(UnityWebRequest webRequest, TextureResponse response)
		{
			response.StatusCode = webRequest.responseCode;

			string eTag = webRequest.GetETag();
			DateTime expirationDate = webRequest.GetExpirationDate();

			//IMPORTANT
			//we used to extract texture from UWR here
			//but I moved it up to image data fetcher as I felt better
			//having it right by the code where we send it to the cache.
			//It feels better control over possible texture leaks
			//call hierarchy should go like
			//ImageDataFetcher=>Raster Tile=>here=>callback to RasterTile=>callback to ImageDataFetcher
			//var texture = DownloadHandlerTexture.GetContent(uwr);
			// texture.wrapMode = TextureWrapMode.Clamp;
			//response.Texture2D = texture;

			response.ETag = eTag;
			response.ExpirationDate = expirationDate;
			response.Data = webRequest.downloadHandler.data;
		}

		private void Handle304(UnityWebRequest webRequest, TextureResponse response)
		{
			response.StatusCode = webRequest.responseCode;
			response.ExpirationDate = webRequest.GetExpirationDate();
		}

		public IAsyncRequest Request(string uri, Action<Response> callback, int timeout = 10)
		{
			var uriBuilder = new UriBuilder(uri);
			if (!string.IsNullOrEmpty(_accessToken))
			{
				string accessTokenQuery = "access_token=" + _accessToken;
				string mapsSkuToken = "sku=" + _getMapsSkuToken();
				if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
				{
					uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery + "&" + mapsSkuToken;
				}
				else
				{
					uriBuilder.Query = accessTokenQuery + "&" + mapsSkuToken;
				}
			}

			string finalUrl = uriBuilder.ToString();
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

					if (null != callback)
					{
						response.IsUpdate = true;
						callback(response);
					}
				}, timeout);
		}
	}
}