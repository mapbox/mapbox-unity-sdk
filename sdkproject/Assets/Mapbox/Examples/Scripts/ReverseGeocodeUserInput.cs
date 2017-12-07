//-----------------------------------------------------------------------
// <copyright file="ReverseGeocodeUserInput.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Mapbox.Examples
{
    using Mapbox.Unity;
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using Mapbox.Geocoding;
    using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

    /// <summary>
    /// Peforms a reverse geocoder request (search by latitude, longitude) whenever the InputField on *this*
    /// gameObject is finished with an edit. 
    /// Expects input in the form of "latitude, longitude"
    /// </summary>
    [RequireComponent(typeof(InputField))]
	public class ReverseGeocodeUserInput : MonoBehaviour
	{
		InputField _inputField;

		ReverseGeocodeResource _resource;

		Geocoder _geocoder;

		Vector2d _coordinate;

		bool _hasResponse;
		public bool HasResponse
		{
			get
			{
				return _hasResponse;
			}
		}

		public ReverseGeocodeResponse Response { get; private set;}

		public event EventHandler<EventArgs> OnGeocoderResponse;

		void Awake()
		{
			_inputField = GetComponent<InputField>();
			_inputField.onEndEdit.AddListener(HandleUserInput);
			_resource = new ReverseGeocodeResource(_coordinate);
		}

		void Start()
		{
            _geocoder = MapboxAccess.Instance.Geocoder;
		}

		/// <summary>
		/// An edit was made to the InputField.
		/// Unity will send the string from _inputField.
		/// Make geocoder query.
		/// </summary>
		/// <param name="searchString">Search string.</param>
		void HandleUserInput(string searchString)
		{
			_hasResponse = false;
			if (!string.IsNullOrEmpty(searchString))
			{
				_coordinate = Conversions.StringToLatLon(searchString);
				_resource.Query = _coordinate;
				_geocoder.Geocode(_resource, HandleGeocoderResponse);
			}
		}

		/// <summary>
		/// Handles the geocoder response by updating coordinates and notifying observers.
		/// </summary>
		/// <param name="res">Res.</param>
		void HandleGeocoderResponse(ReverseGeocodeResponse res)
		{
			_hasResponse = true;
			Response = res;
			if (OnGeocoderResponse != null)
			{
				OnGeocoderResponse(this, EventArgs.Empty);
			}
		}
	}
}