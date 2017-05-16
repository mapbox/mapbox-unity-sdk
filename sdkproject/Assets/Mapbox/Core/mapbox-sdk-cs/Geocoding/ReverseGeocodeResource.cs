//-----------------------------------------------------------------------
// <copyright file="ReverseGeocodeResource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Geocoding
{
    using System.Collections.Generic;
    using Mapbox.Utils;

    /// <summary> A reverse geocode request. </summary>
    public sealed class ReverseGeocodeResource : GeocodeResource<Vector2d>
	{
		// Required
		private Vector2d query;

		/// <summary> Initializes a new instance of the <see cref="ReverseGeocodeResource" /> class.</summary>
		/// <param name="query"> Location to reverse geocode. </param>
		public ReverseGeocodeResource(Vector2d query)
		{
			this.Query = query;
		}

		/// <summary> Gets or sets the location. </summary>
		public override Vector2d Query {
			get {
				return this.query;
			}

			set {
				this.query = value;
			}
		}

		/// <summary> Builds a complete reverse geocode URL string. </summary>
		/// <returns> A complete, valid reverse geocode URL string. </returns>
		public override string GetUrl()
		{
			Dictionary<string, string> opts = new Dictionary<string, string>();

			if (this.Types != null)
			{
				opts.Add("types", GetUrlQueryFromArray(this.Types));
			}

			return Constants.BaseAPI +
							this.ApiEndpoint +
							this.Mode +
							this.Query.ToString() +
							".json" +
							EncodeQueryString(opts);
		}
	}
}