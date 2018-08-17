//-----------------------------------------------------------------------
// <copyright file="HeroBuildingSelectionUserInput.cs" company="Mapbox">
//     Copyright (c) 2018 Mapbox. All rights reserved.
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
	using Mapbox.Unity.Location;
	using Mapbox.Unity.Utilities;

	public class HeroBuildingSelectionUserInput : MonoBehaviour
	{

		[Geocode]
		public string location;

		public Vector3 _cameraPosition;

		public Vector3 _cameraRotation;

		Vector2d _coordinate;

		private Camera _camera;

		Button _button;

		ForwardGeocodeResource _resource;

		bool _hasResponse;
		public bool HasResponse
		{
			get
			{
				return _hasResponse;
			}
		}

		public ForwardGeocodeResponse Response { get; private set; }

		//public event Action<> OnGeocoderResponse = delegate { };
		public event Action<ForwardGeocodeResponse, bool> OnGeocoderResponse = delegate { };

		void Awake()
		{
			_button = GetComponent<Button>();
			_button.onClick.AddListener(HandleUserInput);
			_resource = new ForwardGeocodeResource("");
			_camera = Camera.main;
		}

		void TransformCamera()
		{
			_camera.transform.position = _cameraPosition;
			_camera.transform.localEulerAngles = _cameraRotation;	
		}

		void HandleUserInput()
		{
			_hasResponse = false;
			if (!string.IsNullOrEmpty(location))
			{
				_resource.Query = location;
				MapboxAccess.Instance.Geocoder.Geocode(_resource, HandleGeocoderResponse);

			}
		}

		void HandleGeocoderResponse(ForwardGeocodeResponse res)
		{
			_hasResponse = true;
			if (null == res)
			{
				//_button.text = "no geocode response";
			}
			else if (null != res.Features && res.Features.Count > 0)
			{
				var center = res.Features[0].Center;
				//_inputField.text = string.Format("{0},{1}", center.x, center.y);
				_coordinate = res.Features[0].Center;
			}
			Response = res;
			TransformCamera();
			OnGeocoderResponse(res, false);
		}

		public void BakeCameraTransform()
		{
			_cameraPosition = _camera.transform.position;
			_cameraRotation = _camera.transform.localEulerAngles;
		}
	}
}
