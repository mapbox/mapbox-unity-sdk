using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;

public class FakeAbstractMap : MonoBehaviour 
{
	
	public StyleTypes m_styles;

	public ScriptableStyle m_style;

	public MaterialAtlasPaletteBundle m_displayBundle = new MaterialAtlasPaletteBundle("display");

	private Dictionary<MapFeatureType, MaterialAtlasPaletteBundle> m_materialAtlasPaletteBundles;



	private void CacheMaterialAtlasPaletteBundles()
	{
		if(m_materialAtlasPaletteBundles != null)
		{
			return;
		}
		m_materialAtlasPaletteBundles = new Dictionary<MapFeatureType, MaterialAtlasPaletteBundle>();
		foreach (var key in MapFeatureTypes.MapStyleLookup.Keys)
		{
			MaterialAtlasPaletteBundleLookup lookup = MapFeatureTypes.MapStyleLookup[key];

			MaterialAtlasPaletteBundle mapBundle = new MaterialAtlasPaletteBundle(lookup.m_styleType.ToString());

			mapBundle.m_material = Resources.Load(lookup.m_materialPath, typeof(Material)) as Material;
			mapBundle.m_atlasInfo = Resources.Load(lookup.m_atlasPath, typeof(AtlasInfo)) as AtlasInfo;
			mapBundle.m_palette = Resources.Load(lookup.m_palettePath, typeof(ScriptablePalette)) as ScriptablePalette;
		}
	}

	public void AddFeature()
	{
		
	}
}
