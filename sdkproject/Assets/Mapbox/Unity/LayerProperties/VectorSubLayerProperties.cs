namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using UnityEngine;

	[Serializable]
	public class VectorSubLayerProperties : LayerProperties
	{
		public virtual string Key
		{
			get
			{
				return coreOptions.layerName;
			}
		}
		public CoreVectorLayerProperties coreOptions = new CoreVectorLayerProperties();
		public LineGeometryOptions lineGeometryOptions = new LineGeometryOptions();
		public VectorFilterOptions filterOptions = new VectorFilterOptions();
		public GeometryExtrusionOptions extrusionOptions = new GeometryExtrusionOptions
		{
			extrusionType = ExtrusionType.None,
			propertyName = "height",
			extrusionGeometryType = ExtrusionGeometryType.RoofAndSide,

		};

		public ColliderOptions colliderOptions = new ColliderOptions
		{
			colliderType = ColliderType.None,
		};

		public GeometryMaterialOptions materialOptions = new GeometryMaterialOptions();

		public LayerPerformanceOptions performanceOptions;

		//HACK : workaround to avoid users accidentaly leaving the buildingsWithUniqueIds settign on and have missing buildings. 
		public bool honorBuildingIdSetting = true;
		public bool buildingsWithUniqueIds = false;

		public PositionTargetType moveFeaturePositionTo;
		[NodeEditorElement("Mesh Modifiers")]
		public List<MeshModifier> MeshModifiers;
		[NodeEditorElement("Game Object Modifiers")]
		public List<GameObjectModifier> GoModifiers;
		public PresetFeatureType presetFeatureType = PresetFeatureType.Custom;

		//public string roadLayer = "road";
		//public string landuseLayer = "landuse";
		//public string roadLayer_TypeProperty = "class";
		//public string landuseLayer_TypeProperty = "class";

		[SerializeField]
		private int _maskValue;

		public string selectedTypes;

		public bool SubLayerNameMatchesExact(string layerName)
		{
			return coreOptions.sublayerName == layerName;
		}
		public bool SubLayerNameContains(string layerName)
		{
			return coreOptions.sublayerName.Contains(layerName);
		}

		public bool SubLayerUsesStyleType(StyleTypes style)
		{
			return materialOptions.style == style;
		}

		public void SetActive(bool active)
		{
			coreOptions.isActive = active;
			coreOptions.HasChanged = true;
		}

		public void SetStyleType(StyleTypes style)
		{
			materialOptions.style = style;
			materialOptions.HasChanged = true;
		}

		public void SetRealisticStyle()
		{
			materialOptions.style = StyleTypes.Realistic;
			materialOptions.HasChanged = true;
		}

		public void SetFantasyStyle()
		{
			materialOptions.style = StyleTypes.Fantasy;
			materialOptions.HasChanged = true;
		}

		public void SetSimpleStylePaletteType(SamplePalettes palette)
		{
			materialOptions.samplePalettes = palette;
			materialOptions.HasChanged = true;
		}

		public void SetLightStyleOpacity(float opacity)
		{
			materialOptions.lightStyleOpacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
			materialOptions.HasChanged = true;
		}

		public void SetDarkStyleOpacity(float opacity)
		{
			materialOptions.darkStyleOpacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
			materialOptions.HasChanged = true;
		}

		public void SetColorStyleColor(Color color)
		{
			materialOptions.colorStyleColor = color;
			materialOptions.HasChanged = true;
		}

		public void SetCustomTexturingType(UvMapType uvMapType)
		{
			materialOptions.texturingType = uvMapType;
			materialOptions.HasChanged = true;
		}

		public void SetCustomTopMaterial(Material material)
		{
			materialOptions.materials[0].Materials[0] = new Material(material);
			materialOptions.HasChanged = true;
		}

		public void SetCustomSideMaterial(Material material)
		{
			materialOptions.materials[1].Materials[0] = new Material(material);
			materialOptions.HasChanged = true;
		}

		public void SetCustomMaterials(Material topMaterial, Material sideMaterial)
		{
			materialOptions.materials[0].Materials[0] = new Material(topMaterial);
			materialOptions.materials[1].Materials[0] = new Material(sideMaterial);
			materialOptions.HasChanged = true;
		}

		public void SetCustomUvAtlas(AtlasInfo atlas)
		{
			materialOptions.atlasInfo = atlas;
			materialOptions.HasChanged = true;
		}

		public void SetCustomColorPalette(ScriptablePalette palette)
		{
			materialOptions.colorPalette = palette;
			materialOptions.HasChanged = true;
		}

		public void SetCustomStyleAssets(CustomStyleBundle customStyleBundle)
		{
			materialOptions.materials[0].Materials[0] = (customStyleBundle.sideMaterial != null) ? customStyleBundle.sideMaterial : materialOptions.materials[0].Materials[0];
			materialOptions.materials[1].Materials[0] = (customStyleBundle.topMaterial != null) ? customStyleBundle.topMaterial : materialOptions.materials[1].Materials[0];
			materialOptions.atlasInfo = (customStyleBundle.atlasInfo != null) ? customStyleBundle.atlasInfo : materialOptions.atlasInfo;
			materialOptions.colorPalette = (customStyleBundle.colorPalette != null) ? customStyleBundle.colorPalette : materialOptions.colorPalette;
			materialOptions.HasChanged = true;
		}

		public StyleTypes GetStyleType()
		{
			return materialOptions.style;
		}

		public SamplePalettes GetSimpleStylePaletteType()
		{
			return materialOptions.samplePalettes;
		}

		public float GetLightStyleOpacity()
		{
			return materialOptions.lightStyleOpacity;
		}

		public float GetDarkStyleOpacity()
		{
			return materialOptions.darkStyleOpacity;
		}

		public Color GetColorStyleColor()
		{
			return materialOptions.colorStyleColor;
		}

		public UvMapType GetCustomTexturingType()
		{
			return materialOptions.texturingType;
		}

		public Material GetCustomTopMaterial()
		{
			return materialOptions.materials[0].Materials[0];
		}

		public Material GetCustomSideMaterial()
		{
			return materialOptions.materials[1].Materials[0];
		}

		public AtlasInfo GetCustomUvAtlas()
		{
			return materialOptions.atlasInfo;
		}

		public ScriptablePalette GetCustomColorPalette()
		{
			return materialOptions.colorPalette;
		}
	}
}
