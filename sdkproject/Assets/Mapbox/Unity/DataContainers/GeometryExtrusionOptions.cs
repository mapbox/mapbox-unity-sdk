namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Modifiers;
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
		public string propertyName;
		public float minimumHeight;
		public float maximumHeight;
	}
}
