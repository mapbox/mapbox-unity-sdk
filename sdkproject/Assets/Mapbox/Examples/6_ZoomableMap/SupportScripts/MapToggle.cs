using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using UnityEngine;

public class MapToggle : MonoBehaviour
{
    public AbstractMap Map2D;
    public AbstractMap Map3D;

    private void Start()
    {

    }

    public void Enable3D(bool show3d)
    {
        if (show3d)
        {
            Disable2D();
            Enable3D();
        }
        else
        {
            Disable3D();
            Enable2D();
        }
    }

    private void Enable2D()
    {
        Map2D.gameObject.SetActive(true);
        Map2D.UpdateMap();
    }

    private void Disable2D()
    {
        Map2D.MapVisualizer.UnregisterAllTiles();
        Map2D.gameObject.SetActive(false);
    }

    private void Enable3D()
    {
        Map3D.gameObject.SetActive(true);

        if (!Map3D.IsInitialized)
        {
            Map3D.SetUpMap();
        }

        Map3D.UpdateMap(Map2D.CenterLatitudeLongitude, Map2D.Zoom);
    }

    private void Disable3D()
    {
        Map3D.MapVisualizer.UnregisterAllTiles();
        Map3D.gameObject.SetActive(false);
    }

}
