//-----------------------------------------------------------------------
// <copyright file="MapUtils.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Map
{
    using System;
    using Mapbox.Utils;

    /// <summary>
    /// Utilities for working with Map APIs.
    /// </summary>
    public static class MapUtils
	{
		/// <summary>
		/// Normalizes a static style URL.
		/// </summary>
		/// <returns>The static style URL.</returns>
		/// <param name="url">A url, either a Mapbox URI (mapbox://{username}/{styleid}) or a full url to a map.</param>
		public static string NormalizeStaticStyleURL(string url)
		{
			bool isMapboxUrl = url.StartsWith("mapbox://", StringComparison.Ordinal);

			// Support full Mapbox URLs by returning here if a mapbox URL is not detected.
			if (!isMapboxUrl)
			{
				return url;
			}

			string[] split = url.Split('/');
			var user = split[3];
			var style = split[4];
			var draft = string.Empty;

			if (split.Length > 5)
			{
				draft = "/draft";
			}

			return Constants.BaseAPI + "styles/v1/" + user + "/" + style + draft + "/tiles";
		}

		/// <summary>
		/// Converts a MapId to a URL.
		/// </summary>
		/// <returns>The identifier to URL.</returns>
		/// <param name="id">The style id.</param>
		public static string MapIdToUrl(string id)
		{
			// TODO: Validate that id is a real id
			const string MapBaseApi = Constants.BaseAPI + "v4/";

			return MapBaseApi + id;
		}
	}
}
