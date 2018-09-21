using UnityEngine;

namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using System;

	[Serializable]
	public class LineGeometryOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(LineMeshModifier);
			}
		}

		[Tooltip("Width of the line feature.")]
		public float Width = 1.0f;

	}
}
