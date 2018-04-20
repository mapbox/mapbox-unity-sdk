using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;

[System.Serializable]
public class MapFeatureStyleBundle
{
	public MapFeatureType m_featureType;

	public List<GeometryMaterialOptions> m_themes = new List<GeometryMaterialOptions>();
	public List<OrnamentBundle> m_ornaments = new List<OrnamentBundle>();

	public MapFeatureStyleBundle(MapFeatureType mapFeatureType)
	{
		m_featureType = mapFeatureType;
	}
}

[CreateAssetMenu(menuName = "Mapbox/ScriptableStyle")]
public class ScriptableStyle : ScriptableObject 
{

	//public List<MapFeatureStyleBundle> m_features = new List<MapFeatureStyleBundle>();

	public GeometryMaterialOptions geometryMaterialOptions = new GeometryMaterialOptions();

	/*
	public void AddFeature(MapFeatureType featureType)
	{
		MapFeatureStyleBundle mapFeatureStyleBundle = new MapFeatureStyleBundle(featureType);

		MapFeatureDefinitions mapFeatureDefinitions = MapFeatureTypes.GetMapFeatureDefinitions(featureType);

		if (mapFeatureDefinitions != null)
		{
			int numFeatureComponents = mapFeatureDefinitions.GetFeatureLabels().Length;
			for (int j = 0; j < numFeatureComponents; j++)
			{
				string labelName = mapFeatureDefinitions.GetFeatureLabels()[j];
				GeometryMaterialOptions materialAtlasPaletteBundle = new GeometryMaterialOptions();
				mapFeatureStyleBundle.m_themes.Add(materialAtlasPaletteBundle);

				OrnamentBundle ornamentBundle = new OrnamentBundle(labelName);
				mapFeatureStyleBundle.m_ornaments.Add(ornamentBundle);
			}
		}

		m_features.Add(mapFeatureStyleBundle);
	}

	public void RemoveFeature(MapFeatureStyleBundle mapFeatureStyleBundle)
	{
		m_features.Remove(mapFeatureStyleBundle);
	}
	
	public void ClearFeatures()
	{
		m_features.Clear();
	}
	*/
}
