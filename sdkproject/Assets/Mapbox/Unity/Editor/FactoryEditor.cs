using UnityEngine;
using System.Collections;
using UnityEditor;
using Mapbox.Unity.MeshGeneration.Factories;

[CustomEditor(typeof(AbstractTileFactory))]
public class FactoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
    }
}
