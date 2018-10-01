namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using UnityEngine;

	[Serializable]
	public class VectorSubLayerProperties : LayerProperties, IVectorSubLayer
	{
		public override bool HasChanged
		{
			set
			{
				if (value == true)
				{
					OnPropertyHasChanged(new VectorLayerUpdateArgs { property = this });
				}
			}
		}

		public virtual string Key
		{
			get
			{
				return coreOptions.layerName;
			}
		}

		public ISubLayerTexturing Texturing
		{
			get
			{
				return materialOptions;
			}
		}
		public ISubLayerModeling Modeling
		{
			get
			{
				if (modeling == null)
				{
					modeling = new SubLayerModeling(this);
				}
				return modeling;
			}
		}
		public ISubLayerFiltering Filtering
		{
			get
			{
				return filterOptions;
			}
		}
		public ISubLayerBehaviorModifiers BehaviorModifiers
		{
			get
			{
				if (behaviorModifiers == null)
				{
					behaviorModifiers = new SubLayerBehaviorModifiers(this);
				}
				return behaviorModifiers;
			}
		}
		protected SubLayerModeling modeling;
		protected SubLayerBehaviorModifiers behaviorModifiers;
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

		#region Feature Model Api Methods

		/// <summary>
		/// Change the primtive type of the feature which will be used to decide
		/// what type of mesh operations features will require.
		/// In example, roads are generally visualized as lines and buildings are
		/// generally visualized as polygons.
		/// </summary>
		/// <param name="type">Primitive type of the featues in the layer.</param>
		public virtual void SetPrimitiveType(VectorPrimitiveType type)
		{
			if (coreOptions.geometryType != type)
			{
				coreOptions.geometryType = type;
				coreOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Disable mesh extrusion for the features in this layer.
		/// </summary>
		public virtual void DisableExtrusion()
		{
			if (extrusionOptions.extrusionType != ExtrusionType.None)
			{
				extrusionOptions.extrusionType = ExtrusionType.None;
				extrusionOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the height value to be used for Absolute Height extrusion type.
		/// Same field is used for the maximum height of Range Extrusion type so beware
		/// of possible side effects.
		/// </summary>
		/// <param name="absoluteHeight">Fixed height value for all features in the layer.</param>
		public virtual void SetAbsoluteHeight(float absoluteHeight)
		{
			if (extrusionOptions.maximumHeight != absoluteHeight)
			{
				extrusionOptions.maximumHeight = absoluteHeight;
				extrusionOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Change the minimum and maximum height values used for Range Height option.
		/// Maximum height is also used for Absolute Height option so beware of possible side
		/// effects.
		/// </summary>
		/// <param name="minHeight">Lower bound to be used for extrusion</param>
		/// <param name="maxHeight">Top bound to be used for extrusion</param>
		public virtual void SetHeightRange(float minHeight, float maxHeight)
		{
			if (extrusionOptions.minimumHeight != minHeight ||
				extrusionOptions.maximumHeight != maxHeight)
			{
				extrusionOptions.minimumHeight = minHeight;
				extrusionOptions.maximumHeight = maxHeight;
				extrusionOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the extrusion multiplier which will be used only in the Y axis (height).
		/// </summary>
		/// <param name="multiplier">Multiplier value.</param>
		public virtual void SetExtrusionMultiplier(float multiplier)
		{
			if (extrusionOptions.extrusionScaleFactor != multiplier)
			{
				extrusionOptions.extrusionScaleFactor = multiplier;
				extrusionOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Enable terrain snapping for features which sets vertices to terrain
		/// elevation before extrusion.
		/// </summary>
		/// <param name="isEnabled">Enabled terrain snapping</param>
		public virtual void EnableSnapingTerrain(bool isEnabled)
		{
			if (coreOptions.snapToTerrain != isEnabled)
			{
				coreOptions.snapToTerrain = isEnabled;
				coreOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Enable combining individual features meshes into one to minimize gameobject
		/// count and draw calls.
		/// </summary>
		/// <param name="isEnabled"></param>
		public virtual void EnableCombiningMeshes(bool isEnabled)
		{
			if (coreOptions.combineMeshes != isEnabled)
			{
				coreOptions.combineMeshes = isEnabled;
				coreOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Enable/Disable feature colliders and sets the type of colliders to use.
		/// </summary>
		/// <param name="colliderType">Type of the collider to use on features.</param>
		public virtual void SetFeatureCollider(ColliderType colliderType)
		{
			if (colliderOptions.colliderType != colliderType)
			{
				colliderOptions.colliderType = colliderType;
				colliderOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Absolute height" and extrudes all features by
		/// the given fixed value.
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="height">Extrusion value</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		public virtual void EnableAbsoluteExtrusion(ExtrusionGeometryType extrusionGeometryType, float height, float extrusionScaleFactor = 1)
		{
			if (extrusionOptions.extrusionType != ExtrusionType.AbsoluteHeight ||
				extrusionOptions.extrusionGeometryType != extrusionGeometryType ||
				extrusionOptions.maximumHeight != height ||
				extrusionOptions.extrusionScaleFactor != extrusionScaleFactor)
			{
				extrusionOptions.extrusionType = ExtrusionType.AbsoluteHeight;
				extrusionOptions.extrusionGeometryType = extrusionGeometryType;
				extrusionOptions.maximumHeight = height;
				extrusionOptions.extrusionScaleFactor = extrusionScaleFactor;
				extrusionOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Property" and extrudes all features by
		/// the choosen property's value.
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		public virtual void EnablePropertyExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName = "height", float extrusionScaleFactor = 1)
		{
			if (extrusionOptions.extrusionType != ExtrusionType.PropertyHeight ||
				extrusionOptions.extrusionGeometryType != extrusionGeometryType ||
				extrusionOptions.propertyName != propertyName ||
				extrusionOptions.extrusionScaleFactor != extrusionScaleFactor)
			{
				extrusionOptions.extrusionType = ExtrusionType.PropertyHeight;
				extrusionOptions.extrusionGeometryType = extrusionGeometryType;
				extrusionOptions.propertyName = propertyName;
				extrusionOptions.extrusionScaleFactor = extrusionScaleFactor;
				extrusionOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Minimum Height" and extrudes all features by
		/// the choosen property's value such that all vertices (roof) will be
		/// flat at the lowest vertex elevation (after terrain elevation taken into account).
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		public virtual void EnableMinExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName = "height", float extrusionScaleFactor = 1)
		{
			if (extrusionOptions.extrusionType != ExtrusionType.MinHeight ||
				extrusionOptions.extrusionGeometryType != extrusionGeometryType ||
				extrusionOptions.propertyName != propertyName ||
				extrusionOptions.extrusionScaleFactor != extrusionScaleFactor)
			{
				extrusionOptions.extrusionType = ExtrusionType.MinHeight;
				extrusionOptions.extrusionGeometryType = extrusionGeometryType;
				extrusionOptions.propertyName = propertyName;
				extrusionOptions.extrusionScaleFactor = extrusionScaleFactor;
				extrusionOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Changes extrusion type to "Range Height" and extrudes all features by
		/// the choosen property's value such that all vertices (roof) will be
		/// flat at the highest vertex elevation (after terrain elevation taken into account).
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="propertyName">Name of the property to use for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		public virtual void EnableMaxExtrusion(ExtrusionGeometryType extrusionGeometryType, string propertyName = "height", float extrusionScaleFactor = 1)
		{
			if (extrusionOptions.extrusionType != ExtrusionType.MaxHeight ||
				extrusionOptions.extrusionGeometryType != extrusionGeometryType ||
				extrusionOptions.propertyName != propertyName ||
				extrusionOptions.extrusionScaleFactor != extrusionScaleFactor)
			{
				extrusionOptions.extrusionType = ExtrusionType.MaxHeight;
				extrusionOptions.extrusionGeometryType = extrusionGeometryType;
				extrusionOptions.propertyName = propertyName;
				extrusionOptions.extrusionScaleFactor = extrusionScaleFactor;
				extrusionOptions.HasChanged = true;
			}
		}

		/// /// <summary>
		/// Changes extrusion type to "Minimum Height" and extrudes all features by
		/// the choosen property's value such that they'll be in provided range.
		/// Lower values will be increase to Minimum Height and higher values will
		/// be lowered to Maximum height.
		/// </summary>
		/// <param name="extrusionGeometryType">Option to create top and side polygons after extrusion.</param>
		/// <param name="minHeight">Lower bound to be used for extrusion</param>
		/// <param name="maxHeight">Top bound to be used for extrusion</param>
		/// <param name="extrusionScaleFactor">Height multiplier</param>
		public virtual void EnableRangeExtrusion(ExtrusionGeometryType extrusionGeometryType, float minHeight, float maxHeight, float extrusionScaleFactor = 1)
		{
			if (extrusionOptions.extrusionType != ExtrusionType.RangeHeight ||
				extrusionOptions.extrusionGeometryType != extrusionGeometryType ||
				extrusionOptions.minimumHeight != minHeight ||
				extrusionOptions.maximumHeight != maxHeight ||
				extrusionOptions.extrusionScaleFactor != extrusionScaleFactor)
			{
				extrusionOptions.extrusionType = ExtrusionType.RangeHeight;
				extrusionOptions.extrusionGeometryType = extrusionGeometryType;
				extrusionOptions.minimumHeight = minHeight;
				extrusionOptions.maximumHeight = maxHeight;
				extrusionOptions.extrusionScaleFactor = extrusionScaleFactor;
				extrusionOptions.HasChanged = true;
			}
		}
		#endregion
	}
}
