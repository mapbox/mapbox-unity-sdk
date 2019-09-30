using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Utilities;
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

    private Vector3[] _positions = new Vector3[2];

    private void Start()
    {
        _camera = Camera.main;
        DirectionsFactory.ArrangingWaypoints += () =>
        {
            LineRenderer.GetPositions(_positions);
            UiLabel.transform.position = _camera.WorldToScreenPoint((_positions[0] + _positions[1]) / 2);
            var ll1 = AbstractMap.WorldToGeoPosition(_positions[1]);
            var ll2 = AbstractMap.WorldToGeoPosition(_positions[0]);
            Text.text = (Conversions.GeoDistance(ll1.y, ll1.x, ll2.y, ll2.x) * 1000).ToString("F1") + "m";
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
