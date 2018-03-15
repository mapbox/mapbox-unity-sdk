namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;

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
		public ExtrusionType extrusionType = ExtrusionType.None;
		public ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
		public string propertyName = "height";
		public float minimumHeight = 0f;
		public float maximumHeight = 0f;
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
		public float minimumHeight = 0f;
		public float maximumHeight = 0f;

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

			texturingType = uvOptions.texturingType;
			atlasInfo = uvOptions.atlasInfo;
		}
	}
}
