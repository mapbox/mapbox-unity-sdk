namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Data;

	[Serializable]
	public class LineGeometryOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get { return typeof(LineMeshModifier); }
		}

		public float Width = 3.0f;
	}
}