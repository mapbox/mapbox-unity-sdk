using Mapbox.Unity.SourceLayers;

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
		/// Switch layer to custom style using provided mesh and game object modifier
		/// </summary>
		/// <param name="meshModifiers">Mesh modifiers to be used in layer</param>
		/// <param name="gameObjectModifiers">Game object modifiers to be used in layer</param>
		public virtual void CreateCustomStyle(List<MeshModifier> meshModifiers, List<GameObjectModifier> gameObjectModifiers)
		{
			coreOptions.geometryType = VectorPrimitiveType.Custom;
			coreOptions.HasChanged = true;

			MeshModifiers.Clear();
			foreach (var meshModifier in meshModifiers)
			{
				MeshModifiers.Add(meshModifier);
			}
			foreach (var goModifier in gameObjectModifiers)
			{
				GoModifiers.Add(goModifier);
			}
			HasChanged = true;
		}
		#endregion
	}
}
