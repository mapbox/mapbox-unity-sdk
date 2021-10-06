using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Map;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeImageLayerDropbox : MonoBehaviour
{
    public AbstractMap Map;
    public TMP_Dropdown Dropdown;
    // Start is called before the first frame update
    void Start()
    {
        Map = FindObjectOfType<AbstractMap>();
        Dropdown = GetComponent<TMP_Dropdown>();
        Dropdown.options = Enum.GetNames(typeof(ImagerySourceType)).Select(x => new TMP_Dropdown.OptionData(x)).ToList();
    }

    public void UpdateImageSource(int i)
    {
        var type = (ImagerySourceType) i;

        Map.DisposeAllTiles();
        Map.MapVisualizer.ImageryLayer.SetLayerSource(type);
        Map.UpdateMap();
    }
}
