using System;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.SourceLayers;
using UnityEngine;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class LineGeometryOptions : ModifierProperties, ISubLayerLineGeometryOptions
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(LineMeshForPolygonsModifier);
			}
		}

		[Tooltip("Width of the line feature.")]
		public AnimationCurve Width;

		[Tooltip("Miter Limit")]
		public float MiterLimit = 0.2f;

		[Tooltip("Round Limit")]
		public float RoundLimit = 1.05f;

		[Tooltip("Join type of the line feature")]
		public JoinType JoinType = JoinType.Round;

		[Tooltip("Cap type of the line feature")]
		public JoinType CapType = JoinType.Round;

		/// <summary>
		/// Sets the width of the mesh generated for line features.
		/// </summary>
		/// <param name="width">Width of the mesh generated for line features.</param>
		public void SetLineWidth(AnimationCurve width)
		{
			if (Width != width)
			{
				Width = width;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the type of line joints
		/// </summary>
		/// <param name="join">Type of the joint</param>
		public void SetJoinType(LineJoinType join)
		{
			if ((int)JoinType != (int)join)
			{
				JoinType = (JoinType)join;
				HasChanged = true;
			}
		}

		/// <summary>
		/// Sets the type of line beginging and ending caps
		/// </summary>
		/// <param name="join">Type of the line begin and end caps</param>
		public void SetCapType(LineCapType cap)
		{
			if ((int)CapType != (int)cap)
			{
				CapType = (JoinType)cap;
				HasChanged = true;
			}
		}
	}
}
