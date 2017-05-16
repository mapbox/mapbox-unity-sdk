//-----------------------------------------------------------------------
// <copyright file="GeocodeResource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Geocoding
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Platform;

	/// <summary> Base geocode class. </summary>
	/// <typeparam name="T"> Type of Query field (either string or LatLng). </typeparam>
	public abstract class GeocodeResource<T> : Resource
	{
		/// <summary> A List of all possible geocoding feature types. </summary>
		public static readonly List<string> FeatureTypes = new List<string>
		{
			"country", "region", "postcode", "place", "locality", "neighborhood", "address", "poi"
		};

		private readonly string apiEndpoint = "geocoding/v5/";

		private readonly string mode = "mapbox.places/";

		// Optional
		private string[] types;

		/// <summary> Gets or sets the query. </summary>
		public abstract T Query { get; set; }

		/// <summary> Gets the API endpoint as a partial URL path. </summary>
		public override string ApiEndpoint {
			get {
				return this.apiEndpoint;
			}
		}

		/// <summary> Gets the mode. </summary>
		public string Mode {
			get {
				return this.mode;
			}
		}

		/// <summary> Gets or sets which feature types to return results for. </summary>
		public string[] Types {
			get {
				return this.types;
			}

			set {
				if (value == null)
				{
					this.types = value;
					return;
				}

				for (int i = 0; i < value.Length; i++)
				{
					// Validate provided types
					if (!FeatureTypes.Contains(value[i]))
					{
						throw new Exception("Invalid type. Must be \"country\", \"region\", \"postcode\",  \"place\",  \"locality\",  \"neighborhood\",  \"address\", or  \"poi\".");
					}
				}

				this.types = value;
			}
		}
	}
}
