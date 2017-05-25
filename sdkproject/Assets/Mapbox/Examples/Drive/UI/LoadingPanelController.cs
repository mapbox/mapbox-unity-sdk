using Mapbox.Unity.MeshGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingPanelController : MonoBehaviour
{
    public MapVisualizer MapVisualizer;
    public GameObject Content;

    void Awake()
    {
        MapVisualizer.OnMapVisualizerStateChanged += (s) =>
        {
            if (s == ModuleState.Finished)
            {
                Content.SetActive(false);
            }
            else if(s == ModuleState.Working)
            {
                Content.SetActive(true);
            }

        };
    }
}
