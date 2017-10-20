namespace Mapbox.Examples
{
	using Mapbox.Unity;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;
	using Mapbox.MapMatching;
	using UnityEngine;

	public class MapMatchingExample : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		LineRenderer _lineRenderer;

		[SerializeField]
		PlotRoute _originalRoute;

		[SerializeField]
		Profile _profile;

		MapMatcher _mapMatcher;

		void Awake()
		{
			_mapMatcher = MapboxAccess.Instance.MapMatcher;
		}

		[ContextMenu("Test")]
		public void Match()
		{
			var resource = new MapMatchingResource();

			var coordinates = new List<Vector2d>();
			foreach (var position in _originalRoute.Positions)
			{
				var coord = position.GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
				coordinates.Add(coord);
				Debug.Log("MapMatchingExample: " + coord);
			}

			resource.Coordinates = coordinates.ToArray();
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

			_lineRenderer.positionCount = 0;
			for (int i = 0, responseTracepointsLength = response.Tracepoints.Length; i < responseTracepointsLength; i++)
			{
				var point = response.Tracepoints[i];

				// FIXME: why/how can a point be null?
				if (point == null)
				{
					continue;
				}

				_lineRenderer.positionCount++;
				Debug.Log("MapMatchingExample: " + point.Name);
				Debug.Log("MapMatchingExample: " + point.Location);
				Debug.Log("MapMatchingExample: " + point.MatchingsIndex);
				Debug.Log("MapMatchingExample: " + point.WaypointIndex);
				var position = Conversions.GeoToWorldPosition(point.Location, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz();
				_lineRenderer.SetPosition(_lineRenderer.positionCount - 1, position);
			}
		}
	}
}