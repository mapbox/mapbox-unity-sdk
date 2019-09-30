using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.UI;
using Constants = Mapbox.Unity.Constants;

public class Scalebar : MonoBehaviour
{
    [SerializeField] private AbstractMap Map;
    [SerializeField] private Transform FirstPoint;
    [SerializeField] private Transform SecondPoint;
    [SerializeField] private Text ScaleText;

    private Camera _camera;
    private RaycastHit _hit;

    Vector2d _ll1 = new Vector2d();
    Vector2d _ll2 = new Vector2d();
    private Ray _ray;
    private float _distanceLineAngle;
    private float _distanceVectorMagnitude;

    private void Start()
    {
        _camera = Camera.main;
    }

    void Update()
    {
        MidBar();
        UpdateDistanceLabel();
    }

    private void UpdateDistanceLabel()
    {
        _ray = _camera.ScreenPointToRay(FirstPoint.position);
        if (Physics.Raycast(_ray, out _hit))
        {
            _ll1 = Map.WorldToGeoPosition(_hit.point);
        }

        _ray = _camera.ScreenPointToRay(SecondPoint.position);
        if (Physics.Raycast(_ray, out _hit))
        {
            _ll2 = Map.WorldToGeoPosition(_hit.point);
        }

        ScaleText.text = (Conversions.GeoDistance(_ll1.y, _ll1.x, _ll2.y, _ll2.x) * 1000).ToString("F2") + "m";
    }

    private void MidBar()
    {
        _distanceLineAngle = Vector3.SignedAngle(SecondPoint.position - FirstPoint.position, new Vector3(1, 0, 0), new Vector3(0, 0, 1));
        _distanceVectorMagnitude = (SecondPoint.position - FirstPoint.position).magnitude / 10;
        transform.localScale = new Vector3(_distanceVectorMagnitude, 1, 1);
        transform.rotation = Quaternion.Euler(0, 0, -_distanceLineAngle);
        transform.position = FirstPoint.position;
    }
}