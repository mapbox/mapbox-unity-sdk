using System;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;

namespace Mapbox.Core.VectorModule
{
	public class VectorProcessorDataFetcher
	{
		public Action<string> ErrorRecieved = (s) => { };
		public Action<CanonicalTileId, Map.VectorTile> DataRecieved = (tileId, vectorTile) => { };

		protected MapboxAccess _fileSource;

		public void FetchData(bool useOptimizedStyle, Style style, CanonicalTileId tileId, string tilesetId)
		{
			if (_fileSource == null)
				_fileSource = MapboxAccess.Instance;

			var vectorTile = (useOptimizedStyle) ? new Map.VectorTile(style.Id, style.Modified) : new Map.VectorTile();
			vectorTile.Initialize(_fileSource, tileId, tilesetId, () =>
			{
				if (vectorTile.HasError)
				{
					//FetchingError(vectorDaraParameters.tile, vectorTile, new TileErrorEventArgs(vectorDaraParameters.tile.CanonicalTileId, vectorTile.GetType(), vectorDaraParameters.tile, vectorTile.Exceptions));
					ErrorRecieved(vectorTile.ExceptionsAsString);
				}
				else
				{
					DataRecieved(tileId, vectorTile);
				}
			});
		}
	}
}