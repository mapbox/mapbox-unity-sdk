namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Modifiers;
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
		public MaterialList[] materials = new MaterialList[2];

		public GeometryMaterialOptions()
		{
			materials = new MaterialList[2];
			materials[0] = new MaterialList();
			materials[1] = new MaterialList();
		}
	}
}
