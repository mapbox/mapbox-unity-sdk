using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
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
            Text.text = ((_positions[1] - _positions[0]).magnitude / AbstractMap.WorldRelativeScale).ToString("F1") + "m";
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
