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

	/// <summary> Base geocode class. </summary>
	/// <typeparam name="T"> Type of Query field (either string or LatLng). </typeparam>
	public class MapMatchingResource : Resource
	{

		private readonly string _apiEndpoint = "matching/v5/";

		private string _profile = Profile.MapboxDriving;
		private Vector2d[] _coordinates;

		// optional
		private string _geometries;
		// ptional
		private uint[] _radiuses;
		// optional
		private bool? _steps;
		// optional
		private string _overview;
		// optional
		private long[] _timestamps;
		// optional
		private string[] _annotations;
		// optional
		private bool? _tidy;
		// optional
		private string _language;


		/// <summary> Gets the API endpoint as a partial URL path. </summary>
		public override string ApiEndpoint
		{
			get { return _apiEndpoint; }
		}


		/// <summary> Gets or sets which feature types to return results for. </summary>
		public Vector2d[] Coordinates
		{
			get { return _coordinates; }
			set
			{
				if (value.Length < 2 || value.Length > 100)
				{
					throw new Exception("Must be between 2 and 100 elements in coordinates array");
				}

				_coordinates = value;
			}
		}


		public uint[] Radiuses
		{
			get { return _radiuses; }
			set
			{
				if (null == _coordinates) { throw new Exception("Coordinates not set"); }
				if (value.Length != _coordinates.Length) { throw new Exception("There must be as many radiuses as there are coordinates in the request."); }
				if (value.Where(r => r == 0).Count() > 0) { throw new Exception("Radius must be greater than 0"); }

				_radiuses = value;
			}
		}


		public string Overview
		{
			get { return _overview; }
			set { _overview = value; }
		}


		public long[] Timestamps
		{
			get { return _timestamps; }
			set
			{
				if (null == _coordinates) { throw new Exception("Coordinates not set"); }
				if (value.Length != _coordinates.Length) { throw new Exception("There must be as many timestapms as there are coordinates in the request."); }

				_timestamps = value;
			}
		}


		public string[] Annotations
		{
			get { return _annotations; }
			set { _annotations = value; }
		}


		public bool? Tidy
		{
			get { return _tidy; }
			set { _tidy = value; }
		}


		public string Language
		{
			get { return _language; }
			set { _language = value; }
		}

		public override string GetUrl()
		{


			Dictionary<string, string> options = new Dictionary<string, string>();

			return
				Constants.BaseAPI
				+ _apiEndpoint
				+ _profile + "/"
				+ GetUrlQueryFromArray<Vector2d>(_coordinates, ";")
				+ ".json"
				+ EncodeQueryString(options);
		}
	}
}
