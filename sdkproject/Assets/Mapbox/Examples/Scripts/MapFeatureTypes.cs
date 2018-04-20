using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;



public class MaterialAtlasPaletteBundleLookup
{
	public StyleTypes m_styleType;
	public string m_materialPath;
	public string m_atlasPath;
	public string m_palettePath;

	public MaterialAtlasPaletteBundleLookup(StyleTypes st, string matPath, string atlasPath, string palettePath)
	{
		m_styleType = st;
		m_materialPath = matPath;
		m_atlasPath = atlasPath;
		m_palettePath = palettePath;
	}
}

public class MapFeatureDefinitions
{
	private MapFeatureType _mapFeatureType;
	private VectorPrimitiveType _vectorPrimitiveType;
	private string _layerName;
	private string[] _featureLabelNames;

	public string[] GetFeatureLabels()
	{
		return _featureLabelNames;
	}

	public MapFeatureDefinitions(MapFeatureType mapFeatureType, VectorPrimitiveType vectorPrimitiveType, string layerName, string[] labels)
	{
		_mapFeatureType = mapFeatureType;
		_vectorPrimitiveType = vectorPrimitiveType;
		_layerName = layerName;
		_featureLabelNames = labels;
	}
}

public static class MapFeatureTypes  
{
	private static string[] _buildingDefLabels = new string[] { "Building Wall", "Building Roof" };
	private static string[] _roadDefLabels = new string[] { "Road Border", "Road Surface" };


	public static Dictionary<StyleTypes, MaterialAtlasPaletteBundleLookup> MapStyleLookup = new Dictionary<StyleTypes, MaterialAtlasPaletteBundleLookup>()
	{
		{ 
			StyleTypes.Simple, 
			new MaterialAtlasPaletteBundleLookup(StyleTypes.Simple,"SimpleMaterial", "SimpleAtlasInfo", "SimplePalette") 
		},
		{ 
			StyleTypes.Realistic, 
			new MaterialAtlasPaletteBundleLookup(StyleTypes.Realistic,"RealisticMaterial", "RealisticAtlasInfo", "RealisticPalette") 
		}
	};

	public static Dictionary<MapFeatureType, MapFeatureDefinitions> MapFeatureLookup = new Dictionary<MapFeatureType, MapFeatureDefinitions>()
	{
		{
			MapFeatureType.Building,
			new MapFeatureDefinitions(MapFeatureType.Building, VectorPrimitiveType.Polygon, "building", _buildingDefLabels)
		},
		{
			MapFeatureType.Road,
			new MapFeatureDefinitions(MapFeatureType.Road, VectorPrimitiveType.Line, "road", _roadDefLabels)
		}
	};


	public static MapFeatureDefinitions GetMapFeatureDefinitions(MapFeatureType mapFeatureType)
	{
		if(MapFeatureLookup.ContainsKey(mapFeatureType))
		{
			return MapFeatureLookup[mapFeatureType];
		}
		return null;
	}
}
