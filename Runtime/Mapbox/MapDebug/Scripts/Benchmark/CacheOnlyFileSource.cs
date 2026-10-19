using System;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// IFileSource decorator that blocks all network requests when enabled.
    /// When BlockNetwork is true, all request methods return immediately with a cancelled response.
    /// When false, delegates to the inner file source.
    /// Used during benchmark recording to ensure deterministic, cache-only tile loading.
    /// </summary>
    public class CacheOnlyFileSource : IFileSource
    {
        private readonly IFileSource _inner;

        public IFileSource Inner => _inner;
        public bool BlockNetwork { get; set; }

        public CacheOnlyFileSource(IFileSource inner)
        {
            _inner = inner;
        }

        public IAsyncRequest Request(string uri, Action<Response> callback, int timeout = 10)
        {
            if (BlockNetwork)
            {
                var response = new Response();
                response.AddException(new Exception("[CacheOnly] Network blocked during benchmark recording."));
                callback?.Invoke(response);
                return null;
            }
            return _inner.Request(uri, callback, timeout);
        }

        public IWebRequest MapboxImageRequest(string uri, Action<WebRequestResponse> callback, string etag = "",
            int timeout = 10, bool isNonReadable = true)
        {
            if (BlockNetwork)
            {
                var response = new WebRequestResponse
                {
                    Result = WebResponseResult.Cancelled,
                    StatusCode = 0
                };
                callback?.Invoke(response);
                return null;
            }
            return _inner.MapboxImageRequest(uri, callback, etag, timeout, isNonReadable);
        }

        public IWebRequest CustomImageRequest(string uri, Action<WebRequestResponse> callback, string etag = null,
            int timeout = 10, bool isNonReadable = true)
        {
            if (BlockNetwork)
            {
                var response = new WebRequestResponse
                {
                    Result = WebResponseResult.Cancelled,
                    StatusCode = 0
                };
                callback?.Invoke(response);
                return null;
            }
            return _inner.CustomImageRequest(uri, callback, etag, timeout, isNonReadable);
        }

        public IWebRequest MapboxDataRequest(string uri, Action<WebRequestResponse> callback, string etag = "",
            int timeout = 10)
        {
            if (BlockNetwork)
            {
                var response = new WebRequestResponse
                {
                    Result = WebResponseResult.Cancelled,
                    StatusCode = 0
                };
                callback?.Invoke(response);
                return null;
            }
            return _inner.MapboxDataRequest(uri, callback, etag, timeout);
        }

        public void OnDestroy()
        {
            _inner.OnDestroy();
        }
    }
}
