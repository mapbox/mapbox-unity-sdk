//-----------------------------------------------------------------------
// <copyright file="MapMatchingResource.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapMatching
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Platform;
	using Mapbox.Utils;
	using Mapbox.VectorTile.ExtensionMethods;

	/// <summary> Base geocode class. </summary>
	/// <typeparam name="T"> Type of Query field (either string or LatLng). </typeparam>
	public class MapMatchingResource : Resource
	{

		private readonly string _apiEndpoint = "matching/v5/";
		private Vector2d[] _coordinates;
		private uint[] _radiuses;
		private long[] _timestamps;


		/// <summary> Gets the API endpoint as a partial URL path. </summary>
		public override string ApiEndpoint
		{
			get { return _apiEndpoint; }
		}


		/// <summary>A directions profile ID.</summary>
		public Profile Profile = Profile.MapboxDriving;


		/// <summary> Coordinate to visit in order; there can be between 2 and 100 coordinates. </summary>
		public Vector2d[] Coordinates
		{
			get { return _coordinates; }
			set
			{
				if (null == value)
				{
					throw new Exception("Coordinates cannot be null.");
				}
				if (value.Length < 2 || value.Length > 100)
				{
					throw new Exception("Must be between 2 and 100 elements in coordinates array");
				}

				_coordinates = value;
			}
		}


		/// <summary>
		/// <para>Format of the returned geometry.</para>
		/// <para>Allowed values are: geojson (as LineString ), polyline with precision 5,  polyline6 (polyline with precision 6).</para>
		/// <para>The default value is polyline.</para>
		/// </summary>
		public Nullable<Geometries> Geometries;


		/// <summary>
		/// <para>A list of uints in meters indicating the assumed precision of the used tracking device.</para>
		/// <para>There must be as many radiuses as there are coordinates in the request.</para>
		/// <para>Values can be a number between 0 and 30.</para>
		/// <para>Use higher numbers (20-30) for noisy traces and lower numbers (1-10) for clean traces.</para>
		/// <para>The default value is 5.</para>
		/// </summary>
		public uint[] Radiuses
		{
			get { return _radiuses; }
			set
			{
				if (null == _coordinates) { throw new Exception("Coordinates not set"); }
				//allow for nulling radiuses
				if (null == value)
				{
					_radiuses = null;
					return;
				}
				if (value.Length != _coordinates.Length) { throw new Exception("There must be as many radiuses as there are coordinates in the request."); }
				if (value.Where(r => r == 0).Count() > 0) { throw new Exception("Radius must be greater than 0"); }

				_radiuses = value;
			}
		}


		/// <summary>
		/// <para>Whether to return steps and turn-by-turn instructions.</para>
		/// <para>Can be  true or false.</para>
		/// <para>The default is false.</para>
		/// </summary>
		public bool? Steps;


		/// <summary>
		/// <para>Type of returned overview geometry.</para>
		/// <para>Can be full (the most detailed geometry available), simplified (a simplified version of the full geometry), or none (no overview geometry).</para>
		/// <para>The default is  simplified.</para>
		/// </summary>
		public Nullable<Overview> Overview;


		/// <summary>
		/// <para>Timestamps corresponding to each coordinate provided in the request.</para>
		/// <para>Must be numbers in Unix time (seconds since the Unix epoch).</para>
		/// <para>There must be as many timestamps as there are coordinates in the request.</para>
		/// </summary>
		public long[] Timestamps
		{
			get { return _timestamps; }
			set
			{
				if (null == _coordinates) { throw new Exception("Coordinates not set"); }
				//allow for nulling timestamps
				if (null == value)
				{
					_timestamps = null;
					return;
				}
				if (value.Length != _coordinates.Length) { throw new Exception("There must be as many timestapms as there are coordinates in the request."); }

				_timestamps = value;
			}
		}


		/// <summary>
		/// <para>Whether or not to return additional metadata along the route.</para>
		/// <para>Possible values are: duration, distance and speed.</para>
		/// <para>Several annotations can be used.</para>
		/// <para>Combine via '|'.</para>
		/// </summary>
		public Nullable<Annotations> Annotations;


		/// <summary>
		/// <para>Whether or not to transparently remove clusters and re-sample traces for improved map matching results.</para>
		/// <para>Removed tracepoints are set to 'null' in the response!</para>
		/// <para>Can be true or false.</para>
		/// <para>The default is false.</para>
		/// </summary>
		public bool? Tidy;


		/// <summary>
		/// <para>Language of returned turn-by-turn text instructions.</para>
		/// <para>The default is English.</para>
		/// </summary>
		public Nullable<InstructionLanguages> Language;


		public override string GetUrl()
		{
			if (null == _coordinates)
			{
				throw new Exception("Coordinates cannot be null.");
			}

			Dictionary<string, string> options = new Dictionary<string, string>();

			if (Geometries.HasValue) { options.Add("geometries", Geometries.Value.Description()); }
			if (null != _radiuses) { options.Add("radiuses", GetUrlQueryFromArray(_radiuses, ";")); }
			if (Steps.HasValue) { options.Add("steps", Steps.ToString().ToLower()); }
			if (Overview.HasValue) { options.Add("overview", Overview.Value.Description()); }
			if (null != _timestamps) { options.Add("timestamps", GetUrlQueryFromArray(_timestamps, ";")); }
			if (Annotations.HasValue) { options.Add("annotations", getUrlQueryFromAnnotations(Annotations.Value, ",")); }
			if (Tidy.HasValue) { options.Add("tidy", Tidy.Value.ToString().ToLower()); }
			if (Language.HasValue) { options.Add("language", Language.Value.Description()); }

			return
				Constants.BaseAPI
				+ _apiEndpoint
				+ Profile.Description() + "/"
				+ GetUrlQueryFromArray<Vector2d>(_coordinates, ";")
				+ ".json"
				+ EncodeQueryString(options);
		}



		/// <summary>
		/// Convert Annotations (several could be combined) into a string of their descriptions.
		/// </summary>
		/// <param name="annotation">Current annotation</param>
		/// <param name="separator">Character to use for separating items in string.</param>
		/// <returns></returns>
		private string getUrlQueryFromAnnotations(Annotations anno, string separator)
		{
			List<string> descriptions = new List<string>();

			//iterate through all possible 'Annotations' values
			foreach (var a in Enum.GetValues(typeof(Annotations)).Cast<Annotations>())
			{
				//if current value is set, add its description
				if (a == (anno & a)) { descriptions.Add(a.Description()); }
			}

			return string.Join(separator, descriptions.ToArray());
		}


	}
}
