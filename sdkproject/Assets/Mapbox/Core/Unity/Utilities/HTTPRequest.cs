//-----------------------------------------------------------------------
// <copyright file="HTTPRequest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Unity.Utilities
{
    using System;
    using UnityEngine.Networking;
    using System.Collections;
    using Mapbox.Platform;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    internal sealed class HTTPRequest : IAsyncRequest
    {
        private UnityWebRequest _request;
        private readonly Action<Response> _callback;

        public HTTPRequest(string url, Action<Response> callback)
        {
            _request = UnityWebRequest.Get(url);
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
            if (_request != null)
            {
                _request.Abort();
            }
        }

        private IEnumerator DoRequest()
        {
            _request.Send();
            while (!_request.isDone)
            {
                yield return 0;
            }
            var response = new Response();
            response.Error = this._request.error;
            response.Data = this._request.downloadHandler.data;

            _callback(response);
            _request.Dispose();
            _request = null;
        }
    }
}
