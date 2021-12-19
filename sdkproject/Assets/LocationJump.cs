using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.QuadTree;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using EasyButtons;

public class LocationJump : MonoBehaviour
{
    public float Up = 2;
    public float UpMultiplier = 10;
    public float AnimationDuration = 1;
    public QuadTreeMap Map;
    public QuadTreeCameraController CameraController;
    public AnimationCurve LocationCurve;
    public AnimationCurve ZoomCurve;
    public AnimationCurve CameraCurve;

    public string Location1;
    public string Location2;

    [Geocode]
    public string DestinationLatLng;

    private Vector3 _v1, _v2, _v11, _v21;
    private float _timer;

    public void Run()
    {
        var destLatLng = Conversions.StringToLatLon(DestinationLatLng);

        var currentLatLng = Map.CenterLatitudeLongitude;
        _v1 = new Vector3((float) currentLatLng.x, Map.Zoom, (float) Map.CenterLatitudeLongitude.y);
        _v2 = new Vector3((float) destLatLng.x, Map.Zoom, (float) destLatLng.y);
        _v11 = new Vector3((float) currentLatLng.x, Up * UpMultiplier, (float) Map.CenterLatitudeLongitude.y);
        _v21 = new Vector3((float) destLatLng.x, Up * UpMultiplier, (float) destLatLng.y);

        StartCoroutine(Animate());

    }

    [Button]
    public void GoLocation1()
    {
        DestinationLatLng = Location1;
        Run();
    }

    [Button]
    public void GoLocation2()
    {
        DestinationLatLng = Location2;
        Run();
    }

    public IEnumerator Animate()
    {
        _timer = 0;
        var pitchStart = CameraController.Pitch;
        var bearingStart = CameraController.Bearing;
        var startZoom = Map.Zoom;

        while (_timer <= AnimationDuration)
        {
            _timer = Mathf.Min(_timer, AnimationDuration);
            //CalculateCubicBezierPoint(_timer/AnimationDuration, _v1, _v11, _v21, _v2);
            var currentLocation = Vector3.Lerp(_v1, _v2, LocationCurve.Evaluate(_timer / AnimationDuration));
            var currentZoom = Mathf.Lerp(startZoom, 2, ZoomCurve.Evaluate(_timer / AnimationDuration));

            var val = CameraCurve.Evaluate(_timer / AnimationDuration);
            CameraController.Pitch = Mathf.Lerp(pitchStart, 90, val);
            CameraController.Bearing = Mathf.Lerp(bearingStart, 0, val);

            //Map.UpdateMap(new Vector2d(currentLocation.x, currentLocation.z), Mathf.Min(16, currentZoom));
            Map.SetCenterLatitudeLongitude(new Vector2d(currentLocation.x, currentLocation.z));
            Map.SetZoom(Mathf.Min(16, currentZoom));
            Map.RedrawMap();

            _timer += Time.deltaTime;

            yield return null;
        }
    }

    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }
}
