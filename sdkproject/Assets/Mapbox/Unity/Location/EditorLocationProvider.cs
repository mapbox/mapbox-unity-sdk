namespace Mapbox.Unity.Location
{
	using System;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using UnityEngine;
	using Mapbox.Unity.Map;

	/// <summary>
	/// The EditorLocationProvider is responsible for providing mock location and heading data
	/// for testing purposes in the Unity editor.
	/// </summary>
	public class EditorLocationProvider : AbstractEditorLocationProvider
	{
		/// <summary>
		/// The mock "latitude, longitude" location, respresented with a string.
		/// You can search for a place using the embedded "Search" button in the inspector.
		/// This value can be changed at runtime in the inspector.
		/// </summary>
		[SerializeField]
		[Geocode]
		string _latitudeLongitude;

		/// <summary>
		/// The transform that will be queried for location and heading data & ADDED to the mock latitude/longitude
		/// Can be changed at runtime to simulate moving within the map.
		/// </summary>
		[SerializeField]
		Transform _targetTransform;

		//[SerializeField]
		AbstractMap _map;

#if UNITY_EDITOR
		protected void Start()
		{
			LocationProviderFactory.Instance.mapManager.OnInitialized += Map_OnInitialized;
			//_map.OnInitialized += Map_OnInitialized;

			if (_targetTransform == null)
			{
				_targetTransform = transform;
			}
			else
			{
				Debug.Log(_targetTransform.gameObject.name);
			}

			base.Awake();
		}
#endif

		void Map_OnInitialized()
		{
			Debug.Log("Map Initialized");
			LocationProviderFactory.Instance.mapManager.OnInitialized -= Map_OnInitialized;
			//_map.OnInitialized -= Map_OnInitialized;
			LocationProviderFactory.Instance.IsMapInitialized = true;
			_map = LocationProviderFactory.Instance.mapManager;
		}

		Vector2d LatitudeLongitude
		{
			get
			{
				if (LocationProviderFactory.Instance.IsMapInitialized)
				{
					var startingLatLong = Conversions.StringToLatLon(_latitudeLongitude);
					var position = Conversions.GeoToWorldPosition(
						startingLatLong,
						_map.CenterMercator,
						_map.WorldRelativeScale
					).ToVector3xz();
					position += _targetTransform.position;
					Debug.Log("Position : " + position);
					var latLong = _map.WorldToGeoPosition(position);
					Debug.Log("Lat Long " + latLong.x + " , " + latLong.y);
					return _map.WorldToGeoPosition(position);// position.GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
				}

				return Conversions.StringToLatLon(_latitudeLongitude);
			}
		}

		protected override void SetLocation()
		{
			_currentLocation.Heading = _targetTransform.eulerAngles.y;
			_currentLocation.LatitudeLongitude = LatitudeLongitude;
			_currentLocation.Accuracy = _accuracy;
			_currentLocation.Timestamp = UnixTimestampUtils.To(DateTime.UtcNow);
			_currentLocation.IsLocationUpdated = true;
			_currentLocation.IsHeadingUpdated = true;
			_currentLocation.IsLocationServiceEnabled = true;
		}
	}
}
