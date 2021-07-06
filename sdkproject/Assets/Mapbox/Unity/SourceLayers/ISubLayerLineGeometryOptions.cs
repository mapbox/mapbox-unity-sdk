using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using UnityEngine;

namespace Mapbox.Unity.SourceLayers
{
	public interface ISubLayerLineGeometryOptions
	{
		/// <summary>
		/// Sets the width of the mesh generated for line features.
		/// </summary>
		/// <param name="width">Width of the mesh generated for line features.</param>
		void SetLineWidth(AnimationCurve width);
		void SetJoinType(LineJoinType join);
		void SetCapType(LineCapType cap);
	}
}