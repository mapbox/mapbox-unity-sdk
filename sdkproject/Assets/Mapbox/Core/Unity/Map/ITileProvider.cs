namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration;
	using Mapbox.Map;

	public interface ITileProvider
	{
		event EventHandler<TileStateChangedEventArgs> OnTileAdded;
		event EventHandler<TileStateChangedEventArgs> OnTileRemoved;

		void Initialize(MapController mapController);

		// TODO: add reset method?
	}

	public class TileStateChangedEventArgs : EventArgs
	{
		public UnwrappedTileId TileId;
	}
}
