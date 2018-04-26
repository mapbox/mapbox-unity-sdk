namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	/// <summary>
	/// MapFeatureStyleOptions wraps public reference variables relavent to map feature styling. 
	/// </summary>
	[Serializable]
	public class MapFeatureStyleOptions : ModifierProperties
	{

		public override Type ModifierType
		{
			get
			{
				return typeof(StyleModifier);
			}
		}

		public StyleTypes style;

		public ScriptableStyle scriptableStyle;

		public MapFeatureStyleOptions()
		{

		}
	}
}