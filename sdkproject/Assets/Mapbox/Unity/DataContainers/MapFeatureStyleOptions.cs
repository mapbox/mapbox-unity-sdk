namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	[Serializable]
	public class MapFeatureStyleOptions : ModifierProperties
	{

		public override Type ModifierType
		{
			get
			{
				//this should be style modifier?
				return typeof(StyleModifier);
			}
		}

		public StyleTypes m_style;

		public ScriptableStyle m_scriptableStyle;

		public MapFeatureStyleOptions()
		{

		}
	}
}