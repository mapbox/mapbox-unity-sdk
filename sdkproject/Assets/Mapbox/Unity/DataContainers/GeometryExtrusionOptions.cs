namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	[Serializable]
	public class GeometryExtrusionOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(HeightModifier);
			}
		}

		[SerializeField]
		private string _selectedLayerName;
		public ExtrusionType extrusionType = ExtrusionType.None;
		public ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
		[Tooltip("Property name in feature layer to use for extrusion.")]
		public string propertyName = "height";
		public string propertyDescription = "";
		public float minimumHeight = 0f;
		public float maximumHeight = 0f;
		[Tooltip("Scale factor to multiply the extrusion value of the feature.")]
		public float extrusionScaleFactor = 1f;
	}

	[Serializable]
	public class GeometryExtrusionWithAtlasOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(TextureSideWallModifier);
			}
		}
		public UvMapType texturingType = UvMapType.Tiled;
		public AtlasInfo atlasInfo;
		public ExtrusionType extrusionType = ExtrusionType.None;
		public ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
		public string propertyName = "height";
		public string propertyDescription = "";
		public float minimumHeight = 0f;
		public float maximumHeight = 0f;
		public float extrusionScaleFactor = 1f;

		public GeometryExtrusionWithAtlasOptions()
		{

		}
		public GeometryExtrusionWithAtlasOptions(GeometryExtrusionOptions extrusionOptions, UVModifierOptions uvOptions)
		{
			extrusionType = extrusionOptions.extrusionType;
			extrusionGeometryType = extrusionOptions.extrusionGeometryType;
			propertyName = extrusionOptions.propertyName;
			minimumHeight = extrusionOptions.minimumHeight;
			maximumHeight = extrusionOptions.maximumHeight;
			extrusionScaleFactor = extrusionOptions.extrusionScaleFactor;

			texturingType = uvOptions.texturingType;
			atlasInfo = uvOptions.atlasInfo;
		}
	}
}
