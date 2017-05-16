//-----------------------------------------------------------------------
// <copyright file="RawPngRasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Map
{
	/// <summary>
	///    A raster tile containing an encoded RGBA PNG.
	/// <see href="https://www.mapbox.com/blog/terrain-rgb/"> Read about global elevation data. </see>
	/// </summary>
	/// <example>
	/// Print the real world height, in meters, for each pixel:
	/// <code>
	/// var texture = new Texture2D(0, 0);
	/// texture.LoadImage(tile.Data);
	/// for (int i = 0; i &lt; texture.width; i++)
	/// {
	///     for (int j = 0; j &lt; texture.height; j++)
	///     {
	///         var color = texture.GetPixel(i, j);
	/// 		var height = Conversions.GetAbsoluteHeightFromColor(color);
	/// 		Console.Write("Height: " + height);
	///     }
	/// }
	/// </code>
	/// </example>
	public sealed class RawPngRasterTile : RasterTile
	{
		internal override TileResource MakeTileResource(string mapId)
		{
			return TileResource.MakeRawPngRaster(Id, mapId);
		}
	}
}
