using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using UnityEngine;

public class ToggleVectorLayer : MonoBehaviour
{
    public AbstractMap Map;

    public void ToggleTerrain(bool isEnabled)
    {
        //Map.MapVisualizer.VectorLayer.SetLayerSource(isEnabled ? VectorSourceType.MapboxStreetsV8 : VectorSourceType.None);
        if (isEnabled)
        {
            Map.MapVisualizer.TerrainLayer.Enable();
        }
        else
        {
            Map.MapVisualizer.TerrainLayer.Disable();
        }
    }

    public void ToggleImage(bool isEnabled)
    {
        //Map.MapVisualizer.VectorLayer.SetLayerSource(isEnabled ? VectorSourceType.MapboxStreetsV8 : VectorSourceType.None);
        if (isEnabled)
        {
            Map.MapVisualizer.ImageryLayer.Enable();
        }
        else
        {
            Map.MapVisualizer.ImageryLayer.Disable();
        }
    }
    public void ToggleVector(bool isEnabled)
    {
        //Map.MapVisualizer.VectorLayer.SetLayerSource(isEnabled ? VectorSourceType.MapboxStreetsV8 : VectorSourceType.None);
        if (isEnabled)
        {
            Map.MapVisualizer.VectorLayer.Enable();
        }
        else
        {
            Map.MapVisualizer.VectorLayer.Disable();
        }
    }

    public void ToggleBuildings(bool isEnabled)
    {
        if (isEnabled)
        {
            Map.MapVisualizer.VectorLayer.EnableSublayerType("building");
        }
        else
        {
            Map.MapVisualizer.VectorLayer.DisableSublayerType("building");
        }
    }

    public void ToggleAirspace(bool isEnabled)
    {
        if (isEnabled)
        {
            Map.MapVisualizer.VectorLayer.EnableSublayerType("Class_Airspace-7palx6");
        }
        else
        {
            Map.MapVisualizer.VectorLayer.DisableSublayerType("Class_Airspace-7palx6");
        }
    }


}
