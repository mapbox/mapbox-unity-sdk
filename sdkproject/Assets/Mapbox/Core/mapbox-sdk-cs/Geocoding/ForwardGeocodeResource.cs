//-----------------------------------------------------------------------
// <copyright file="ForwardGeocodeResource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Geocoding
{
    using System;
    using System.Collections.Generic;
    using Mapbox.Utils;

    /// <summary> A forward geocode request. </summary>
    public sealed class ForwardGeocodeResource : GeocodeResource<string>
	{
		/// <summary>
		///     ISO 3166-1 alpha-2 country codes.
		///     See <see href="https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2">for all options</see>.
		/// </summary>
		private static readonly List<string> CountryCodes = new List<string>()
		{
			"ad", "ae", "af", "ag", "ai", "al", "am", "ao", "aq", "ar", "as", "at", "au", "aw", "ax", "az", "ba", "bb", "bd", "be", "bf", "bg", "bh", "bi", "bj", "bl", "bm", "bn", "bo", "bq", "br", "bs", "bt", "bv", "bw", "by", "bz", "ca", "cc", "cd", "cf", "cg", "ch", "ci", "ck", "cl", "cm", "cn", "co", "cr", "cu", "cv", "cw", "cx", "cy", "cz", "de", "dj", "dk", "dm", "do", "dz", "ec", "ee", "eg", "eh", "er", "es", "et", "fi", "fj", "fk", "fm", "fo", "fr", "ga", "gb", "gd", "ge", "gf", "gg", "gh", "gi", "gl", "gm", "gn", "gp", "gq", "gr", "gs", "gt", "gu", "gw", "gy", "hk", "hm", "hn", "hr", "ht", "hu", "id", "ie", "il", "im", "in", "io", "iq", "ir", "is", "it", "je", "jm", "jo", "jp", "ke", "kg", "kh", "ki", "km", "kn", "kp", "kr", "kw", "ky", "kz", "la", "lb", "lc", "li", "lk", "lr", "ls", "lt", "lu", "lv", "ly", "ma", "mc", "md", "me", "mf", "mg", "mh", "mk", "ml", "mm", "mn", "mo", "mp", "mq", "mr", "ms", "mt", "mu", "mv", "mw", "mx", "my", "mz", "na", "nc", "ne", "nf", "ng", "ni", "nl", "no", "np", "nr", "nu", "nz", "om", "pa", "pe", "pf", "pg", "ph", "pk", "pl", "pm", "pn", "pr", "ps", "pt", "pw", "py", "qa", "re", "ro", "rs", "ru", "rw", "sa", "sb", "sc", "sd", "se", "sg", "sh", "si", "sj", "sk", "sl", "sm", "sn", "so", "sr", "ss", "st", "sv", "sx", "sy", "sz", "tc", "td", "tf", "tg", "th", "tj", "tk", "tl", "tm", "tn", "to", "tr", "tt", "tv", "tw", "tz", "ua", "ug", "um", "us", "uy", "uz", "va", "vc", "ve", "vg", "vi", "vn", "vu", "wf", "ws", "ye", "yt", "za", "zm", "zw"
		};

		// Required
		private string query;

		// Optional
		private bool? autocomplete;

		// Optional
		private string[] country;

		// Optional
		private Vector2d? proximity;

		// Optional
		private Vector2dBounds? bbox;

		/// <summary> Initializes a new instance of the <see cref="ForwardGeocodeResource" /> class.</summary>
		/// <param name="query"> Place name for forward geocoding. </param>
		public ForwardGeocodeResource(string query)
		{
			this.Query = query;
		}

		/// <summary> Gets or sets the place name for forward geocoding. </summary>
		public override string Query {
			get {
				return this.query;
			}

			set {
				this.query = value;
			}
		}

		/// <summary> Gets or sets the autocomplete option. </summary>
		public bool? Autocomplete {
			get {
				return this.autocomplete;
			}

			set {
				this.autocomplete = value;
			}
		}

		/// <summary>
		///     Gets or sets the bounding box option. Bounding box is a rectangle within which to
		///     limit results, given as <see cref="Bbox"/>.
		/// </summary>
		public Vector2dBounds? Bbox {
			get {
				return this.bbox;
			}

			set {
				this.bbox = value;
			}
		}

		/// <summary>
		///     Gets or sets the country option. Country is an Array of ISO 3166 alpha 2 country codes.
		///     For all possible values, <see cref="CountryCodes"/>.
		/// </summary>
		public string[] Country {
			get {
				return this.country;
			}

			set {
				if (value == null)
				{
					this.country = value;
					return;
				}

				for (int i = 0; i < value.Length; i++)
				{
					// Validate that provided countries exist
					if (!CountryCodes.Contains(value[i]))
					{
						throw new Exception("Invalid country shortcode. See https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2.");
					}
				}

				this.country = value;
			}
		}

		/// <summary>
		///     Gets or sets the proximity option, which is a location around which to bias results,
		///     given as <see cref="Vector2d"/>.
		/// </summary>
		public Vector2d? Proximity {
			get {
				return this.proximity;
			}

			set {
				this.proximity = value;
			}
		}

		/// <summary> Builds a forward geocode URL string. </summary>
		/// <returns> A complete, valid forward geocode URL. </returns>
		public override string GetUrl()
		{
			Dictionary<string, string> opts = new Dictionary<string, string>();

			if (this.Autocomplete != null)
			{
				opts.Add("autocomplete", this.Autocomplete.ToString().ToLower());
			}

			if (this.Bbox != null)
			{
				var nonNullableBbox = (Vector2dBounds)this.Bbox;
				opts.Add("bbox", nonNullableBbox.ToString());
			}

			if (this.Country != null)
			{
				opts.Add("country", ForwardGeocodeResource.GetUrlQueryFromArray<string>(this.Country));
			}

			if (this.Proximity != null)
			{
				var nonNullableProx = (Vector2d)this.Proximity;
				opts.Add("proximity", nonNullableProx.ToString());
			}

			if (this.Types != null)
			{
				opts.Add("types", GetUrlQueryFromArray(this.Types));
			}

			return Constants.BaseAPI +
							this.ApiEndpoint +
							this.Mode +
							Uri.EscapeDataString(this.Query) +
							".json" +
							EncodeQueryString(opts);
		}
	}
}
