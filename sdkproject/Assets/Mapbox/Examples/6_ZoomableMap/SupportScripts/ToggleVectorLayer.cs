using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using UnityEngine;

public class ToggleVectorLayer : MonoBehaviour
{
    public AbstractMap Map;

    public void ToggleVector(bool isEnabled)
    {
        Map.MapVisualizer.VectorLayer.SetLayerSource(isEnabled ? VectorSourceType.MapboxStreetsV8 : VectorSourceType.None);
    }
}
