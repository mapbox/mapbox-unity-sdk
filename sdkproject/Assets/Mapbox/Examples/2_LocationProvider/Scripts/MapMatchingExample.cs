namespace Mapbox.Examples
{
	using Mapbox.Unity;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;
	using Mapbox.MapMatching;
	using UnityEngine;
	using System.Linq;
	using Mapbox.Unity.Location;
	using System;

	public class MapMatchingExample : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		LineRenderer _originalRoute;

		[SerializeField]
		LineRenderer _mapMatchRoute;

		[SerializeField]
		bool _useTransformLocationProvider;

		[SerializeField]
		Profile _profile;

		[SerializeField]
		float _lineHeight = 1f;

		List<Location> _locations = new List<Location>();

		MapMatcher _mapMatcher;

		ILocationProvider _locationProvider;
		public ILocationProvider LocationProvider
		{
			private get
			{
				if (_locationProvider == null)
				{
					_locationProvider = _useTransformLocationProvider ?
						LocationProviderFactory.Instance.TransformLocationProvider : LocationProviderFactory.Instance.DefaultLocationProvider;
				}

				return _locationProvider;
			}
			set
			{
				if (_locationProvider != null)
				{
					_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;

				}
				_locationProvider = value;
				_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			}
		}

		void Awake()
		{
			_mapMatcher = MapboxAccess.Instance.MapMatcher;
		}

		void Start()
		{
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
		}

		void OnDestroy()
		{
			LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
		}

		void LocationProvider_OnLocationUpdated(Location location)
		{
			if (location.IsLocationUpdated)
			{
				_locations.Add(location);
				var position = Conversions.GeoToWorldPosition(
					location.LatitudeLongitude,
					_map.CenterMercator,
					_map.WorldRelativeScale).ToVector3xz();

				position.y = _lineHeight;
				var count = _locations.Count;
				_originalRoute.positionCount = count;
				_originalRoute.SetPosition(count - 1, position);
			}
		}

		[ContextMenu("Map Match")]
		public void Match()
		{
			if (_locations.Count < 2)
			{
				Debug.LogWarning("Need at least two coordinates for map matching.");
				return;
			}

			var resource = new MapMatchingResource();

			//API allows for max 100 coordinates, take newest.
			var locations = _locations.Skip(System.Math.Max(0, _locations.Count - 100)).ToArray();

			var coords = new List<Vector2d>();
			var radiuses = new List<uint>();
			var timestamps = new List<long>();
			foreach (var location in locations)
			{
				coords.Add(location.LatitudeLongitude);
				radiuses.Add((uint)Mathf.Min(location.Accuracy, 30));
				timestamps.Add((long)location.Timestamp);
			}

			resource.Coordinates = coords.ToArray();
			resource.Radiuses = radiuses.ToArray();
			resource.Timestamps = timestamps.ToArray();
			resource.Profile = _profile;

			_mapMatcher.Match(resource, HandleMapMatchResponse);
		}

		void HandleMapMatchResponse(MapMatching.MapMatchingResponse response)
		{
			if (response.HasMatchingError)
			{
				Debug.LogError("MapMatchingExample: " + response.MatchingError);
				return;
			}

			if (response.HasRequestError)
			{
				foreach (var exception in response.RequestExceptions)
				{
					Debug.LogError("MapMatchingExample: " + exception);
				}
				return;
			}

			var lineCount = 1;
			var tracepointsCount = response.Tracepoints.Length;
			foreach (var point in response.Tracepoints)
			{
				// Tracepoints can be null, so let's avoid trying to process those outliers.
				// see https://www.mapbox.com/api-documentation/#match-response-object
				if (point == null)
				{
					continue;
				}

				_mapMatchRoute.positionCount = lineCount;
				Debug.Log(string.Format(
					"MapMatchingExample: {1}{0}Location: {2}{0}MatchtingsIndex: {3}{0}WaypointIndex: {4}"
					, Environment.NewLine
					, point.Name
					, point.Location
					, point.MatchingsIndex
					, point.WaypointIndex
				));

				var position = Conversions.GeoToWorldPosition(
					point.Location,
					_map.CenterMercator,
					_map.WorldRelativeScale).ToVector3xz();

				position.y = _lineHeight = 1f;
				_mapMatchRoute.SetPosition(lineCount - 1, position);
				lineCount++;
			}
		}
	}
}