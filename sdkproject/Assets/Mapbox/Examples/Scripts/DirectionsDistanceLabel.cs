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
    public GameObject UiLabel;
    public Text Text;
    private Camera _camera;

    private Vector3[] _positions;
    private Vector2d _pos1LatLng;
    private Vector2d _pos2LatLng;
    private float _lineDistance;

    private void Start()
    {
        _camera = Camera.main;
        _positions = new Vector3[DirectionsFactory.WaypointCount];
        DirectionsFactory.ArrangingWaypoints += () =>
        {
            LineRenderer.GetPositions(_positions);

            var midLength = 0f;
            for (int i = 1; i < _positions.Length; i++)
            {
                _pos1LatLng = AbstractMap.WorldToGeoPosition(_positions[i]);
                _pos2LatLng = AbstractMap.WorldToGeoPosition(_positions[i - 1]);
                midLength += (float)Conversions.GeoDistance(_pos1LatLng.y, _pos1LatLng.x, _pos2LatLng.y, _pos2LatLng.x) * 1000;
            }

            midLength /= 2;

            var midPoint = _positions[0];
            for (int i = 1; i < _positions.Length; i++)
            {
                _pos1LatLng = AbstractMap.WorldToGeoPosition(_positions[i]);
                _pos2LatLng = AbstractMap.WorldToGeoPosition(_positions[i - 1]);
                _lineDistance = (float)Conversions.GeoDistance(_pos1LatLng.y, _pos1LatLng.x, _pos2LatLng.y, _pos2LatLng.x) * 1000;
                if (midLength > _lineDistance)
                {
                    midLength -= _lineDistance;
                }
                else
                {
                    midPoint = Vector3.Lerp(_positions[i - 1], _positions[i], (float)midLength / _lineDistance);
                    break;
                }
            }

            UiLabel.transform.position = _camera.WorldToScreenPoint(midPoint);
            Text.text = (midLength * 2).ToString("F1") + "m";
        };

        DirectionsFactory.ArrangingWaypointsStarted += () =>
        {
            UiLabel.SetActive(true);
        };

        DirectionsFactory.ArrangingWaypointsFinished += () =>
        {
            UiLabel.SetActive(false);
        };

        DirectionsFactory.RouteDrawn += (midPoint, totalLength) =>
        {
            UiLabel.SetActive(true);
            UiLabel.transform.position = _camera.WorldToScreenPoint(midPoint);
            Text.text = totalLength.ToString("F1") + "m";
        };

        DirectionsFactory.QuerySent += () =>
        {
            UiLabel.SetActive(true);
            Text.text = "Loading";
        };
    }
}
