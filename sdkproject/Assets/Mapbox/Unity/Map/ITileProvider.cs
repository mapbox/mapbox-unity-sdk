namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Map;

	public interface ITileProvider
	{
		event Action<UnwrappedTileId> OnTileAdded;
		event Action<UnwrappedTileId> OnTileRemoved;
		event Action<UnwrappedTileId> OnTileRepositioned;

		ITileProviderOptions Options { get; }

		// TODO: add cancel event?
		// Alternatively, give mapvisualizer an object recycling strategy that can separately determine when to change gameobjects.
		// This removal would essentially lead to a cancel request and nothing more.

		void Initialize(IMap map);
		// TODO: Maybe combine both these methods. 
		void SetOptions(ITileProviderOptions options);

		// TODO: add reset/clear method?
	}

	public interface IUnifiedTileProvider
	{
		event Action<UnwrappedTileId> OnTileAdded;
		event Action<UnwrappedTileId> OnTileRemoved;

		// TODO: add cancel event?
		// Alternatively, give mapvisualizer an object recycling strategy that can separately determine when to change gameobjects.
		// This removal would essentially lead to a cancel request and nothing more.

		void Initialize(IUnifiedMap map);

		// TODO: add reset/clear method?
	}
	public class TileStateChangedEventArgs : EventArgs
	{
		public UnwrappedTileId TileId;
	}


}
