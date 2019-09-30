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
    private Vector2d ll1;
    private Vector2d ll2;

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
                ll1 = AbstractMap.WorldToGeoPosition(_positions[i]);
                ll2 = AbstractMap.WorldToGeoPosition(_positions[i - 1]);

                midLength += (float)Conversions.GeoDistance(ll1.y, ll1.x, ll2.y, ll2.x) * 1000;
            }

            midLength /= 2;

            var midPoint = _positions[0];
            for (int i = 1; i < _positions.Length; i++)
            {
                ll1 = AbstractMap.WorldToGeoPosition(_positions[i]);
                ll2 = AbstractMap.WorldToGeoPosition(_positions[i - 1]);
                var dist = (float)Conversions.GeoDistance(ll1.y, ll1.x, ll2.y, ll2.x) * 1000;
                if (midLength > dist)
                {
                    midLength -= dist;
                }
                else
                {
                    midPoint = Vector3.Lerp(_positions[i - 1], _positions[i], (float)midLength / dist);
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
