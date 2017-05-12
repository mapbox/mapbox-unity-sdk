namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration;

	public abstract class AbstractTileProvider : MonoBehaviour, ITileProvider
	{
		public event EventHandler<TileStateChangedEventArgs> OnTileAdded;
		public event EventHandler<TileStateChangedEventArgs> OnTileRemoved;

		protected MapController _mapController;

		protected List<UnwrappedTileId> _activeTiles;

		// HACK: decide dependency relationships! Right now it is cyclic.
		public void Initialize(MapController mapController)
		{
			_activeTiles = new List<UnwrappedTileId>();
			_mapController = mapController;
			OnInitialized();
		}

		protected void AddTile(UnwrappedTileId tile)
		{
			if (_activeTiles.Contains(tile))
			{
				return;
			}

			_activeTiles.Add(tile);
			if (OnTileAdded != null)
			{
				OnTileAdded(this, new TileStateChangedEventArgs() { TileId = tile });
			}
		}

		protected void RemoveTile(UnwrappedTileId tile)
		{
			if (!_activeTiles.Contains(tile))
			{
				return;
			}

			_activeTiles.Remove(tile);
			if (OnTileRemoved != null)
			{
                OnTileRemoved(this, new TileStateChangedEventArgs() { TileId = tile });
			}
		}

		internal abstract void OnInitialized();
	}
}
