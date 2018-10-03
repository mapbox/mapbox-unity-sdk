﻿namespace Mapbox.Unity.SourceLayers
{
	public interface ISubLayerLineGeometryOptions
	{
		/// <summary>
		/// Sets the width of the mesh generated for line features.
		/// </summary>
		/// <param name="width">Width of the mesh generated for line features.</param>
		void SetLineWidth(float width);
	}
}