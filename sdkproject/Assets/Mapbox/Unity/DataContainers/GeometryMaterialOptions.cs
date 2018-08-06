namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;

	[Serializable]
	public class GeometryMaterialOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(MaterialModifier);
			}
		}

		public StyleTypes style;

		public UvMapType texturingType = UvMapType.Tiled;
		public MaterialList[] materials = new MaterialList[2];
		public AtlasInfo atlasInfo;

		public float lightStyleOpacity = 1.0f;
		public float darkStyleOpacity = 1.0f;

		public Color colorStyleColor = Color.white;

		public SamplePalettes samplePalettes;

		public ScriptablePalette colorPalette;

		public GeometryMaterialOptions()
		{
			materials = new MaterialList[2];
			materials[0] = new MaterialList();
			materials[1] = new MaterialList();
		}
	}

	[Serializable]
	public class UVModifierOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(UvModifier);
			}
		}
		public StyleTypes style;
		public UvMapType texturingType = UvMapType.Tiled;
		public AtlasInfo atlasInfo;
	}

}
