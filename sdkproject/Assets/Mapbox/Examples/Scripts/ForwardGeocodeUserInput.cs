//-----------------------------------------------------------------------
// <copyright file="ForwardGeocodeUserInput.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Examples
{
	using Mapbox.Unity;
	using UnityEngine;
	using UnityEngine.UI;
	using System;
	using Mapbox.Geocoding;
	using Mapbox.Utils;

	[RequireComponent(typeof(InputField))]
	public class ForwardGeocodeUserInput : MonoBehaviour
	{
		InputField _inputField;

		ForwardGeocodeResource _resource;

		Vector2d _coordinate;
		public Vector2d Coordinate
		{
			get
			{
				return _coordinate;
			}
		}

		bool _hasResponse;
		public bool HasResponse
		{
			get
			{
				return _hasResponse;
			}
		}

		public ForwardGeocodeResponse Response { get; private set; }

		public event Action<ForwardGeocodeResponse> OnGeocoderResponse = delegate { };

		void Awake()
		{
			_inputField = GetComponent<InputField>();
			_inputField.onEndEdit.AddListener(HandleUserInput);
			_resource = new ForwardGeocodeResource("");
		}

		void HandleUserInput(string searchString)
		{
			_hasResponse = false;
			if (!string.IsNullOrEmpty(searchString))
			{
				_resource.Query = searchString;
				MapboxAccess.Instance.Geocoder.Geocode(_resource, HandleGeocoderResponse);
			}
		}

		void HandleGeocoderResponse(ForwardGeocodeResponse res)
		{
			_hasResponse = true;
			if (null == res)
			{
				_inputField.text = "no geocode response";
			}
			else if (null != res.Features && res.Features.Count > 0)
			{
				var center = res.Features[0].Center;
				_coordinate = res.Features[0].Center;
			}
			Response = res;
			OnGeocoderResponse(res);
		}
	}
}
