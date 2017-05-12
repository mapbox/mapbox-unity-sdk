namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Map;

	public interface ITileProvider
	{
		event EventHandler<TileStateChangedEventArgs> OnTileAdded;
		event EventHandler<TileStateChangedEventArgs> OnTileRemoved;
	}

	public class TileStateChangedEventArgs : EventArgs
	{
		public CanonicalTileId TileId;
	}
}
