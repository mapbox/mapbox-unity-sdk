//-----------------------------------------------------------------------
// <copyright file="Resource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Platform
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;
#if UNITY_IOS
	using UnityEngine;
#endif

	/// <summary> Abstract class representing a Mapbox resource URL. </summary>
	public abstract class Resource
	{
		/// <summary> Gets the API endpoint, which is a partial URL path. </summary>
		public abstract string ApiEndpoint { get; }

		/// <summary>Builds a complete, valid URL string.</summary>
		/// <returns>Returns URL string.</returns>
		public abstract string GetUrl();

		/// <summary> Encodes a URI with a querystring. </summary>
		/// <param name="values"> Querystring values. </param>
		/// <returns> Encoded URL. </returns>
		protected static String EncodeQueryString(IEnumerable<KeyValuePair<string, string>> values)
		{
			if (values != null)
			{
				// we are seeing super weird crashes on some iOS devices:
				// see 'ForwardGeocodeResource' for more details
				var encodedValues = from p in values
#if UNITY_IOS
									let k = WWW.EscapeURL(p.Key.Trim())
									let v = WWW.EscapeURL(p.Value)
#else
									let k = Uri.EscapeDataString(p.Key.Trim())
									let v = Uri.EscapeDataString(p.Value)
#endif
									orderby k
									select string.IsNullOrEmpty(v) ? k : string.Format("{0}={1}", k, v);
				if (encodedValues.Count() == 0)
				{
					return string.Empty;
				}
				else
				{
					return "?" + string.Join(
						"&", encodedValues.ToArray());
				}
			}

			return string.Empty;
		}

		/// <summary>Builds a string from an array of options for use in URLs.</summary>
		/// <param name="items"> Array of option strings. </param>
		/// <param name="separator"> Character to use for separating items in arry. Defaults to ",". </param>
		/// <returns>Comma-separated string of options.</returns>
		/// <typeparam name="U">Type in the array.</typeparam>
		protected static string GetUrlQueryFromArray<U>(U[] items, string separator = ",")
		{
			return string.Join(separator, items.Select(item => item.ToString()).ToArray());
		}
	}
}
