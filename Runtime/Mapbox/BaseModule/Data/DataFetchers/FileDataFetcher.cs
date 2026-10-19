using System;
using System.Collections;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Mapbox.BaseModule.Data.DataFetchers
{
	public class FileDataFetcher
	{
		//TODO keep track of all fetchFile coroutines and cancel them on destroy/app stopped
		public void FetchData<T>(string filePath, CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T> callback) where T : RasterData, new()
		{
			var webRequest = UnityWebRequestTexture.GetTexture(filePath, isTextureNonreadable);
			Runnable.Run(FetchTextureFromFile(webRequest, (r) =>
				callback(BuildRasterData<T>(r, webRequest, tileId, tilesetId))));
		}
		
		public IEnumerator FetchDataCoroutine<T>(string filePath, CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T> callback) where T : RasterData, new()
		{
			var webRequest = UnityWebRequestTexture.GetTexture(filePath, isTextureNonreadable);
			yield return FetchTextureFromFile(webRequest, (r) =>
				callback(BuildRasterData<T>(r, webRequest, tileId, tilesetId)));
		}

		/// <summary>
		/// Turns a finished file read into a cache item, recording on it whether the read actually
		/// worked.
		///
		/// Both entry points used to ignore the WebRequestResponse completely and build the item
		/// unconditionally, so a failed read was delivered as a perfectly ordinary cache item that
		/// merely happened to carry a null Texture - indistinguishable, to the caller, from a hit.
		/// MapboxCacheManager.GetImageAsync already had the code to turn that into a cache miss
		/// (`if (textureCacheItem.HasError) callback(null)`), but HasError was never assigned true
		/// by anything in the SDK, so that branch was dead and the texture-less item was cached as
		/// real. From there CheckInstantData reported the tile present forever, and it was never
		/// re-requested: a permanently blank imagery tile, or a terrain tile stuck Temporary and
		/// re-polled every frame.
		/// </summary>
		private static T BuildRasterData<T>(WebRequestResponse response, UnityWebRequest webRequest, CanonicalTileId tileId, string tilesetId) where T : RasterData, new()
		{
			// Only read the download handler when the request actually succeeded; asking a failed
			// request for its content is pointless and noisy.
			var failed = response == null || response.Result != WebResponseResult.Success;
			var texture = failed ? null : DownloadHandlerTexture.GetContent(webRequest);

			var rasterData = new T()
			{
				TileId = tileId,
				TilesetId = tilesetId,
				Texture = texture,
				CacheType = CacheType.FileCache,
				// A successful read that yielded no texture is also an error: the item cannot be
				// used, and letting it pass as a hit is what poisons the memory cache.
				HasError = failed || texture == null
			};

			if (rasterData.Texture != null)
			{
				rasterData.Texture.wrapMode = TextureWrapMode.Clamp;
#if UNITY_EDITOR
				rasterData.Texture.name = string.Format("{0}_{1}", tileId.ToString(), tilesetId);
#endif
			}

			return rasterData;
		}
		
		private IEnumerator FetchTextureFromFile(UnityWebRequest webRequest, Action<WebRequestResponse> callback)
		{
			using (webRequest)
			{
				var response = new WebRequestResponse();
				yield return webRequest.SendWebRequest();
				
				#if UNITY_EDITOR
				//need this for tests
				while (webRequest.result == UnityWebRequest.Result.InProgress) yield return null;
				#endif
				
				if (webRequest != null &&
				    webRequest.result == UnityWebRequest.Result.ConnectionError ||
				    webRequest.result == UnityWebRequest.Result.ProtocolError)
				{
					//Debug.Log(webRequest.error);
					response.Result = WebResponseResult.Failed;
					response.AddException(new Exception(webRequest.error));
				}
				else
				{
					response.Result = WebResponseResult.Success;
					response.StatusCode = webRequest.responseCode;
					response.Data = webRequest.downloadHandler.data;
				}

				callback(response);
			}
		}
	}
}
