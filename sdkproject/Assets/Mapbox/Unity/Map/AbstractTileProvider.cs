namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using System.Linq;

	public abstract class AbstractTileProvider : MonoBehaviour, ITileProvider
	{
		public event Action<UnwrappedTileId> OnTileAdded = delegate { };
		public event Action<UnwrappedTileId> OnTileRemoved = delegate { };
		public event Action<UnwrappedTileId> OnTileRepositioned = delegate { };

		protected IMap _map;

		protected Dictionary<UnwrappedTileId, byte> _activeTiles = new Dictionary<UnwrappedTileId, byte>();

		public virtual void Initialize(IMap map)
		{
			_activeTiles.Clear();
			_map = map;
			OnInitialized();
		}

		protected virtual void AddTile(UnwrappedTileId tile)
		{
			if (_activeTiles.ContainsKey(tile))
			{
				return;
			}

			_activeTiles.Add(tile, 0);
			OnTileAdded(tile);
		}

		protected virtual void RemoveTile(UnwrappedTileId tile)
		{
			if (!_activeTiles.ContainsKey(tile))
			{
				return;
			}

			_activeTiles.Remove(tile);
			OnTileRemoved(tile);
		}

		protected virtual void RepositionTile(UnwrappedTileId tile)
		{
			if (!_activeTiles.ContainsKey(tile))
			{
				//TODO : Only active tiles should be repositioned ?
				return;
			}

			OnTileRepositioned(tile);
		}

		public abstract void OnInitialized();
	}
}