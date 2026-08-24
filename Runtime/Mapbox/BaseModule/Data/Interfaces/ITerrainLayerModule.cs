using System;
using System.Collections;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Data.Interfaces
{
	public interface ITerrainLayerModule : ILayerModule
	{
		/// <summary>
		/// Elevation at a point, with 0 meaning both "sea level" and "no data". Prefer
		/// <see cref="TryQueryElevation"/> anywhere those two need telling apart.
		/// </summary>
		float QueryElevation(CanonicalTileId tileId, float x, float y);

		/// <summary>
		/// Elevation at a point, and whether it was actually available. False means no loaded data
		/// covers the point, so <paramref name="elevation"/> is 0 as a placeholder rather than a
		/// reading.
		/// </summary>
		bool TryQueryElevation(CanonicalTileId tileId, float x, float y, out float elevation);
		IEnumerator LoadTileData(CanonicalTileId tileId, Action<TerrainData> callback = null);
	}
}