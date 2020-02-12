using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.UI;

public class DirectionsDistanceLabel : MonoBehaviour
{
	public AbstractMap AbstractMap;
	public DirectionsFactory DirectionsFactory;
	public LineRenderer LineRenderer;
	public GameObject DistanceLabelWrapper;
	public Text DistanceText;
	private Camera _camera;

	private Vector2d _pos1LatLng;
	private Vector2d _pos2LatLng;
	private float _lineDistance;

	private void Start()
	{
		_camera = Camera.main;
		if (AbstractMap == null)
		{
			AbstractMap = FindObjectOfType<AbstractMap>();
		}

		DirectionsFactory.ArrangingWaypoints += (positions) =>
		{
			var midLength = 0f;
			for (int i = 1; i < positions.Length; i++)
			{
				_pos1LatLng = AbstractMap.WorldToGeoPosition(positions[i]);
				_pos2LatLng = AbstractMap.WorldToGeoPosition(positions[i - 1]);
				midLength += (float) Conversions.GeoDistance(_pos1LatLng.y, _pos1LatLng.x, _pos2LatLng.y, _pos2LatLng.x) * 1000;
			}

			midLength /= 2;

			var midPoint = positions[0];
			for (int i = 1; i < positions.Length; i++)
			{
				_pos1LatLng = AbstractMap.WorldToGeoPosition(positions[i]);
				_pos2LatLng = AbstractMap.WorldToGeoPosition(positions[i - 1]);
				_lineDistance = (float) Conversions.GeoDistance(_pos1LatLng.y, _pos1LatLng.x, _pos2LatLng.y, _pos2LatLng.x) * 1000;
				if (midLength > _lineDistance)
				{
					midLength -= _lineDistance;
				}
				else
				{
					midPoint = Vector3.Lerp(positions[i - 1], positions[i], (float) midLength / _lineDistance);
					break;
				}
			}

			if (DistanceLabelWrapper != null)
			{
				DistanceLabelWrapper.transform.position = _camera.WorldToScreenPoint(midPoint);
				DistanceText.text = (midLength * 2).ToString("F1") + "m";
			}
		};

		DirectionsFactory.ArrangingWaypointsStarted += () =>
		{
			if (DistanceLabelWrapper != null)
			{
				DistanceLabelWrapper.SetActive(true);
			}
		};

		DirectionsFactory.ArrangingWaypointsFinished += () =>
		{
			if (DistanceLabelWrapper != null)
			{
				DistanceLabelWrapper.SetActive(false);
			}
		};

		DirectionsFactory.RouteDrawn += (midPoint, totalLength) =>
		{
			if (DistanceLabelWrapper != null)
			{
				DistanceLabelWrapper.SetActive(true);
				DistanceLabelWrapper.transform.position = _camera.WorldToScreenPoint(midPoint);
				DistanceText.text = totalLength.ToString("F1") + "m";
			}
		};

		DirectionsFactory.QuerySent += () =>
		{
			if (DistanceLabelWrapper != null)
			{
				DistanceLabelWrapper.SetActive(true);
				DistanceText.text = "Loading";
			}
		};
	}
}