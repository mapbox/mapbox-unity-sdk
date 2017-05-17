namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Map;

	public interface ITileProvider
	{
		event Action<UnwrappedTileId> OnTileAdded;
		event Action<UnwrappedTileId> OnTileRemoved;

		void Initialize(IMap map);

		// TODO: add reset/clear method?
	}

	public class TileStateChangedEventArgs : EventArgs
	{
		public UnwrappedTileId TileId;
	}
}
