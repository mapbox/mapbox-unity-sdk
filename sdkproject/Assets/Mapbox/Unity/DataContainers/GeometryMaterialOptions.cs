namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

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
		[Tooltip("Use image texture from the Imagery source as texture for roofs. ")]
		public bool projectMapImagery;
		public UvMapType texturingType = UvMapType.Tiled;
		public MaterialList[] materials = new MaterialList[2];
		public AtlasInfo atlasInfo;
		public ScriptablePalette colorPallete;

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
		public UvMapType texturingType = UvMapType.Tiled;
		public AtlasInfo atlasInfo;
	}
}
