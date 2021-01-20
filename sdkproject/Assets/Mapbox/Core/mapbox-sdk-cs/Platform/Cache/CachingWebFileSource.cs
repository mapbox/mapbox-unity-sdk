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
#if MAPBOX_DEBUG_CACHE
		private string _className;
#endif

		//private MapboxCacheManager _cacheManager;
		
		private bool _disposed;
		
		private string _accessToken;
		private Func<string> _getMapsSkuToken;
		private bool _autoRefreshCache;

		public CachingWebFileSource(string accessToken, Func<string> getMapsSkuToken, bool autoRefreshCache)
		{
#if MAPBOX_DEBUG_CACHE
			_className = this.GetType().Name;
#endif
			_accessToken = accessToken;
			_getMapsSkuToken = getMapsSkuToken;
			_autoRefreshCache = autoRefreshCache;
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

		public CachingWebFileSource AddCacheManager(MapboxCacheManager cacheManager)
		{
			//_cacheManager = cacheManager;
			return this;
		}

		public IAsyncRequest Request(
			string uri
			, Action<Response> callback
			, int timeout = 10
		)
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

#if MAPBOX_DEBUG_CACHE
			string methodName = _className + "." + new System.Diagnostics.StackFrame().GetMethod().Name;
#endif

			return requestTileAndCache(finalUrl, timeout, callback);
		}

		public UnityWebRequest MapboxImageRequest(string uri, Action<TextureResponse> callback, int timeout = 10, CanonicalTileId tileId = new CanonicalTileId(), string tilesetId = null, string etag = null, bool isNonreadable = true)
		{
			var finalUrl = CreateFinalUrl(uri);
			return CustomImageRequest(finalUrl, callback, timeout, tileId, tilesetId, etag, isNonreadable);
		}

		public UnityWebRequest CustomImageRequest(string uri, Action<TextureResponse> callback, int timeout = 10, CanonicalTileId tileId = new CanonicalTileId(), string tilesetId = null, string etag = null, bool isNonreadable = true)
		{
			UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(uri, isNonreadable);
			if (string.IsNullOrEmpty(etag))
			{
				Runnable.Run(FetchTexture(uwr, callback));
			}
			else
			{
				Runnable.Run(FetchTextureIfNoneMatch(uwr, callback, etag));
			}

			return uwr;
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

		private IEnumerator FetchTextureIfNoneMatch(UnityWebRequest uwr,  Action<TextureResponse> callback, string etag)
		{
			using (uwr)
			{
				var response = new TextureResponse();
				if (!string.IsNullOrEmpty(etag))
				{
					uwr.SetRequestHeader("If-None-Match", etag);
				}

				yield return uwr.SendWebRequest();

				if (uwr.responseCode == 304) // 304 NOT MODIFIED
				{

					response.StatusCode = uwr.responseCode;
					response.ExpirationDate = uwr.GetExpirationDate();
					callback(response);
					// textureCacheItem.ExpirationDate = uwr.GetExpirationDate();
					// _cacheManager.AddTextureItem(tilesetId, tileId, textureCacheItem, true);
				}
				else if (uwr.responseCode == 200) // 200 OK, it means etag&data has changed so need to update cache
				{
					response.StatusCode = uwr.responseCode;

					string eTag = uwr.GetETag();

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

					var expirationDate = uwr.GetExpirationDate();

					response.ETag = eTag;
					response.ExpirationDate = expirationDate;
					response.Data = uwr.downloadHandler.data;

				}
				callback(response);
			}
		}

		private IEnumerator FetchTexture(UnityWebRequest webRequest, Action<TextureResponse> callback)
		{
			// Stopwatch sw = new Stopwatch();
			// sw.Start();

			using (webRequest)
			{
				var response = new TextureResponse();
				yield return webRequest.SendWebRequest();

				// sw.Stop();
				// Debug.Log(sw.ElapsedMilliseconds);

				if (webRequest != null && webRequest.isNetworkError || webRequest.isHttpError)
				{
					response.AddException(new Exception(webRequest.error));
				}
				else
				{
					response.StatusCode = webRequest.responseCode;

					if (!webRequest.isDone)
						Debug.Log("here");

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

				callback(response);
			}
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