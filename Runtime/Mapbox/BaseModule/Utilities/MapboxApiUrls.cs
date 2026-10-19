//-----------------------------------------------------------------------
// <copyright file="MapboxApiUrls.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
	/// <summary>
	///     Attaches Mapbox credentials (<c>access_token</c> and <c>sku</c>) to request URLs.
	/// </summary>
	/// <remarks>
	///     Credentials are attached to Mapbox hosts only: the URL must be an absolute
	///     <c>https</c> URI whose host is <c>mapbox.com</c> or a subdomain of it. Any other
	///     URL is returned unchanged, so a custom imagery source, a proxy, or a redirect
	///     pointing somewhere else never receives the user's token. This is the same rule
	///     mapbox-gl-js applies.
	///
	///     Every <c>*.mapbox.com</c> subdomain is trusted, which covers <c>api.mapbox.com</c>,
	///     <c>api.tiles.mapbox.com</c>, <c>events.mapbox.com</c> and anything added later
	///     under that domain. This class is the single place to extend if credentials ever
	///     need to reach another Mapbox domain, such as a China endpoint.
	/// </remarks>
	public static class MapboxApiUrls
	{
		private const string MapboxDomain = "mapbox.com";
		private const string MapboxDomainSuffix = ".mapbox.com";

		/// <summary>
		///     Maximum number of distinct hosts we will warn about. Keeps a misconfigured
		///     URL template from growing this set without bound.
		/// </summary>
		private const int MaxWarnedHosts = 16;

		/// <summary>
		///     Hosts we have already warned about. Guarded by <see cref="WarnedHostsLock"/>
		///     because requests are issued off the main thread.
		/// </summary>
		private static readonly HashSet<string> _warnedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private static readonly object WarnedHostsLock = new object();

		/// <summary>
		///     Returns true when <paramref name="uri"/> is an absolute https URL whose host is
		///     mapbox.com or a subdomain of it. Pure: no logging, no state changes.
		/// </summary>
		/// <param name="uri"> The URL to test. May be null, relative, or malformed. </param>
		public static bool IsMapboxHost(string uri)
		{
			Uri parsedUri;
			if (!Uri.TryCreate(uri, UriKind.Absolute, out parsedUri))
			{
				return false;
			}

			return IsMapboxHost(parsedUri);
		}

		/// <summary>
		///     Host rule for an already-parsed URI, so callers that have one do not parse twice.
		/// </summary>
		private static bool IsMapboxHost(Uri parsedUri)
		{
			if (!string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// Deliberately tested against the parsed host, never the raw string, so that
			// "https://api.mapbox.com.evil.example/x" and "https://api.mapbox.com@evil.example/x"
			// are both rejected.
			var host = parsedUri.Host;
			return string.Equals(host, MapboxDomain, StringComparison.OrdinalIgnoreCase)
			       || host.EndsWith(MapboxDomainSuffix, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		///     Appends <c>access_token</c> and <c>sku</c> to <paramref name="uri"/> when it
		///     resolves to a Mapbox host, and returns it unchanged otherwise.
		/// </summary>
		/// <param name="uri"> The request URL. </param>
		/// <param name="accessToken"> The Mapbox access token. No credentials are attached when this is empty. </param>
		/// <param name="getSkuToken"> Supplies the SKU token. Only evaluated when credentials are actually attached. </param>
		/// <returns> The URL with credentials attached, or the original URL. </returns>
		public static string AppendCredentials(string uri, string accessToken, Func<string> getSkuToken)
		{
			if (string.IsNullOrEmpty(accessToken))
			{
				return uri;
			}

			Uri parsedUri;
			if (!Uri.TryCreate(uri, UriKind.Absolute, out parsedUri))
			{
				// This used to throw a UriFormatException, so it stays loud and is never deduplicated.
				Debug.LogError(string.Format(
					"Mapbox credentials were not attached because '{0}' is not an absolute URL. Request URLs must be absolute.",
					uri));
				return uri;
			}

			if (!IsMapboxHost(parsedUri))
			{
				WarnOncePerHost(parsedUri.Host);
				return uri;
			}

			var uriBuilder = new UriBuilder(parsedUri);
			string accessTokenQuery = "access_token=" + accessToken;
			string mapsSkuToken = "sku=" + getSkuToken();
			if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
			{
				uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery + "&" + mapsSkuToken;
			}
			else
			{
				uriBuilder.Query = accessTokenQuery + "&" + mapsSkuToken;
			}

			return uriBuilder.ToString();
		}

		private static void WarnOncePerHost(string host)
		{
			lock (WarnedHostsLock)
			{
				if (_warnedHosts.Count >= MaxWarnedHosts || !_warnedHosts.Add(host))
				{
					return;
				}
			}

			Debug.LogWarning(string.Format(
				"Mapbox credentials were not attached to a request for '{0}' because the host is not mapbox.com or a subdomain of it. " +
				"If this is a proxy or a custom endpoint that needs credentials, include them in the URL template itself. " +
				"This warning is shown once per host.",
				host));
		}
	}
}
