using UnityEngine;
using Mapbox.Directions;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using Mapbox.Unity;
using System;

namespace Mapbox.Examples
{
	public class AstronautDirections : MonoBehaviour
	{
		AbstractMap _map;
		Directions.Directions _directions;
		Action<List<Vector3>> callback;

		void Awake()
		{
			_directions = MapboxAccess.Instance.Directions;
		}

		public void Query(Action<List<Vector3>> vecs, Transform start, Transform end, AbstractMap map)
		{
			if (callback == null)
				callback = vecs;

			_map = map;

			var wp = new Vector2d[2];
			wp[0] = start.GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
			wp[1] = end.GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
			var _directionResource = new DirectionResource(wp, RoutingProfile.Walking);
			_directionResource.Steps = true;
			_directions.Query(_directionResource, HandleDirectionsResponse);
		}

		void HandleDirectionsResponse(DirectionsResponse response)
		{
			if (null == response.Routes || response.Routes.Count < 1)
			{
				return;
			}

			var dat = new List<Vector3>();
			foreach (var point in response.Routes[0].Geometry)
			{
				dat.Add(Conversions.GeoToWorldPosition(point.x, point.y, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz());
			}

			callback(dat);
		}
	}
}