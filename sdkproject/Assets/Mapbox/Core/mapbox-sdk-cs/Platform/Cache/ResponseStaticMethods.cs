using System;
using System.Collections.Generic;
using Mapbox.Platform;
using UnityEngine;
using UnityEngine.Networking;

namespace Mapbox.Core.Platform.Cache
{
	public static class ResponseStaticMethods
	{
		private static string EtagHeaderName = "ETag";
		private static string CacheControlHeaderName = "Cache-Control";

		public static DateTime GetExpirationDate(this UnityWebRequest response)
		{
			DateTime expirationDate = DateTime.Now;
			var headerValue = response.GetResponseHeader(CacheControlHeaderName);
			if (!string.IsNullOrEmpty(headerValue))
			{
				var cacheEntries = headerValue.Split(',');
				if (cacheEntries.Length > 0)
				{
					foreach (var entry in cacheEntries)
					{
						var value = entry.Split('=');
						if (value[0] == "max-age")
						{
							expirationDate = expirationDate + TimeSpan.FromSeconds(int.Parse(value[1]));
							return expirationDate;
						}
					}
				}
			}

			return expirationDate;
		}

		public static string GetETag(this UnityWebRequest response)
		{
			string eTag = response.GetResponseHeader(EtagHeaderName);
			if (string.IsNullOrEmpty(eTag))
			{
				Debug.LogWarning("no 'ETag' header present in response");
			}

			return eTag;
		}

		public static DateTime GetExpirationDate(this Response response)
		{
			DateTime expirationDate = DateTime.Now;
			if (response.Headers.ContainsKey(CacheControlHeaderName))
			{
				var cacheEntries = response.Headers[CacheControlHeaderName].Split(',');
				if (cacheEntries.Length > 0)
				{
					foreach (var entry in cacheEntries)
					{
						var value = entry.Split('=');
						if (value[0] == "max-age")
						{
							expirationDate = expirationDate + TimeSpan.FromSeconds(int.Parse(value[1]));
							return expirationDate;
						}
					}
				}
			}

			return expirationDate;
		}

		public static string GetETag(this Response response)
		{
			string eTag = String.Empty;
			if (!response.Headers.ContainsKey(EtagHeaderName))
			{
				Debug.LogWarning("no 'ETag' header present in response");
			}
			else
			{
				eTag = response.Headers[EtagHeaderName];
			}

			return eTag;
		}
	}
}