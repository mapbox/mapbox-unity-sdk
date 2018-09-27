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

		[SerializeField]
		private int _maskValue;

		public string selectedTypes;

		/// <summary>
		/// Returns true if the layer name matches a given string.
		/// </summary>
		/// <returns><c>true</c>, if layer name matches exact was subed, <c>false</c> otherwise.</returns>
		/// <param name="layerName">Layer name.</param>
		public virtual bool SubLayerNameMatchesExact(string layerName)
		{
			return coreOptions.sublayerName == layerName;
		}
		/// <summary>
		/// Returns true if the layer name contains a given string.
		/// </summary>
		/// <returns><c>true</c>, if layer name contains was subed, <c>false</c> otherwise.</returns>
		/// <param name="layerName">Layer name.</param>
		public virtual bool SubLayerNameContains(string layerName)
		{
			return coreOptions.sublayerName.Contains(layerName);
		}
		/// <summary>
		/// Returns true if the layer uses a given style.
		/// </summary>
		/// <returns><c>true</c>, if layer uses style type was subed, <c>false</c> otherwise.</returns>
		/// <param name="style">Style.</param>
		public virtual bool SubLayerUsesStyleType(StyleTypes style)
		{
			return materialOptions.style == style;
		}

		#region Setters

		/// <summary>
		/// Sets the active.
		/// </summary>
		/// <param name="active">If set to <c>true</c> active.</param>
		public virtual void SetActive(bool active)
		{
			coreOptions.isActive = active;
			coreOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the type of the style.
		/// </summary>
		/// <param name="style">Style.</param>
		public virtual void SetStyleType(StyleTypes style)
		{
			materialOptions.style = style;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the layer to use the realistic style.
		/// </summary>
		public virtual void SetRealisticStyle()
		{
			materialOptions.style = StyleTypes.Realistic;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the layer to use the fantasy style.
		/// </summary>
		public virtual void SetFantasyStyle()
		{
			materialOptions.style = StyleTypes.Fantasy;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the type of the simple style palette.
		/// </summary>
		/// <param name="palette">Palette.</param>
		public virtual void SetSimpleStylePaletteType(SamplePalettes palette)
		{
			materialOptions.samplePalettes = palette;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the light style opacity.
		/// </summary>
		/// <param name="opacity">Opacity.</param>
		public virtual void SetLightStyleOpacity(float opacity)
		{
			materialOptions.lightStyleOpacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the dark style opacity.
		/// </summary>
		/// <param name="opacity">Opacity.</param>
		public virtual void SetDarkStyleOpacity(float opacity)
		{
			materialOptions.darkStyleOpacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the color of the color style.
		/// </summary>
		/// <param name="color">Color.</param>
		public virtual void SetColorStyleColor(Color color)
		{
			materialOptions.colorStyleColor = color;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the texturing (UV) type of the custom style.
		/// </summary>
		/// <param name="uvMapType">Uv map type.</param>
		public virtual void SetCustomTexturingType(UvMapType uvMapType)
		{
			materialOptions.texturingType = uvMapType;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style top material.
		/// </summary>
		/// <param name="material">Material.</param>
		public virtual void SetCustomTopMaterial(Material material)
		{
			materialOptions.materials[0].Materials[0] = new Material(material);
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style side material.
		/// </summary>
		/// <param name="material">Material.</param>
		public virtual void SetCustomSideMaterial(Material material)
		{
			materialOptions.materials[1].Materials[0] = new Material(material);
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style top and side materials.
		/// </summary>
		/// <param name="topMaterial">Top material.</param>
		/// <param name="sideMaterial">Side material.</param>
		public virtual void SetCustomMaterials(Material topMaterial, Material sideMaterial)
		{
			materialOptions.materials[0].Materials[0] = new Material(topMaterial);
			materialOptions.materials[1].Materials[0] = new Material(sideMaterial);
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style uv atlas.
		/// </summary>
		/// <param name="atlas">Atlas.</param>
		public virtual void SetCustomUvAtlas(AtlasInfo atlas)
		{
			materialOptions.atlasInfo = atlas;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style color palette.
		/// </summary>
		/// <param name="palette">Palette.</param>
		public virtual void SetCustomColorPalette(ScriptablePalette palette)
		{
			materialOptions.colorPalette = palette;
			materialOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the custom style assets using a CustomStyleBundle object.
		/// </summary>
		/// <param name="customStyleBundle">Custom style bundle.</param>
		public virtual void SetCustomStyleAssets(CustomStyleBundle customStyleBundle)
		{
			materialOptions.materials[0].Materials[0] = (customStyleBundle.sideMaterial != null) ? customStyleBundle.sideMaterial : materialOptions.materials[0].Materials[0];
			materialOptions.materials[1].Materials[0] = (customStyleBundle.topMaterial != null) ? customStyleBundle.topMaterial : materialOptions.materials[1].Materials[0];
			materialOptions.atlasInfo = (customStyleBundle.atlasInfo != null) ? customStyleBundle.atlasInfo : materialOptions.atlasInfo;
			materialOptions.colorPalette = (customStyleBundle.colorPalette != null) ? customStyleBundle.colorPalette : materialOptions.colorPalette;
			materialOptions.HasChanged = true;
		}

		#endregion

		#region Getters

		/// <summary>
		/// Gets the type of style used in the layer.
		/// </summary>
		/// <returns>The style type.</returns>
		public virtual StyleTypes GetStyleType()
		{
			return materialOptions.style;
		}

		/// <summary>
		/// Gets the type of simple style palette used in the layer.
		/// </summary>
		/// <returns>The simple style palette type.</returns>
		public virtual SamplePalettes GetSimpleStylePaletteType()
		{
			return materialOptions.samplePalettes;
		}

		/// <summary>
		/// Gets the light style opacity.
		/// </summary>
		/// <returns>The light style opacity.</returns>
		public virtual float GetLightStyleOpacity()
		{
			return materialOptions.lightStyleOpacity;
		}

		/// <summary>
		/// Gets the dark style opacity.
		/// </summary>
		/// <returns>The dark style opacity.</returns>
		public virtual float GetDarkStyleOpacity()
		{
			return materialOptions.darkStyleOpacity;
		}

		/// <summary>
		/// Gets the color of the color style.
		/// </summary>
		/// <returns>The color style color.</returns>
		public virtual Color GetColorStyleColor()
		{
			return materialOptions.colorStyleColor;
		}

		/// <summary>
		/// Gets the type of the custom style texturing.
		/// </summary>
		/// <returns>The custom texturing type.</returns>
		public virtual UvMapType GetCustomTexturingType()
		{
			return materialOptions.texturingType;
		}

		/// <summary>
		/// Gets the custom top material.
		/// </summary>
		/// <returns>The custom top material.</returns>
		public virtual Material GetCustomTopMaterial()
		{
			return materialOptions.materials[0].Materials[0];
		}

		/// <summary>
		/// Gets the custom side material.
		/// </summary>
		/// <returns>The custom side material.</returns>
		public virtual Material GetCustomSideMaterial()
		{
			return materialOptions.materials[1].Materials[0];
		}

		/// <summary>
		/// Gets the custom uv atlas.
		/// </summary>
		/// <returns>The custom uv atlas.</returns>
		public virtual AtlasInfo GetCustomUvAtlas()
		{
			return materialOptions.atlasInfo;
		}

		/// <summary>
		/// Gets the custom color palette.
		/// </summary>
		/// <returns>The custom color palette.</returns>
		public virtual ScriptablePalette GetCustomColorPalette()
		{
			return materialOptions.colorPalette;
		}

		#endregion
	}
}
