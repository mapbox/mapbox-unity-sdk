using Mapbox.Unity.SourceLayers;
using UnityEngine;

namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using System;

	[Serializable]
	public class LineGeometryOptions : ModifierProperties, ISubLayerLineGeometryOptions
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

		/// <summary>
		/// Sets the width of the mesh generated for line features.
		/// </summary>
		/// <param name="width">Width of the mesh generated for line features.</param>
		public void SetLineWidth(float width)
		{
			if (Width != width)
			{
				Width = width;
				HasChanged = true;
			}
		}
	}
}
