using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Examples;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using UnityEngine;

public class BackgroundGridParameters : MonoBehaviour
{
    public AbstractMap Map;
    public Material Material;
    public MapMovement Movement;

    private float _tileSize = 10;
    private float _tileSizeWithZoom;

    private void Start()
    {

    }

    private void Update()
    {
        var tile = Map.MapVisualizer.ActiveTiles.First().Value;
        var pos = tile.gameObject.transform.position;
        var size = Map.Options.scalingOptions.unityTileSize * tile.transform.lossyScale.x;
        pos -= new Vector3(size / 2, 0, size / 2);
        Material.SetVector("_Position", pos);
        Material.SetFloat("_Size", size);
    }
}
