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

		protected IMap _map;

		protected Dictionary<UnwrappedTileId, byte> _activeTiles = new Dictionary<UnwrappedTileId, byte>();

		public void Initialize(IMap map)
		{
			_activeTiles.Clear();
			_map = map;
			OnInitialized();
		}

		protected void AddTile(UnwrappedTileId tile)
		{
			if (_activeTiles.ContainsKey(tile))
			{
				return;
			}

			_activeTiles.Add(tile, 0);
			OnTileAdded(tile);
		}

		protected void RemoveTile(UnwrappedTileId tile)
		{
			if (!_activeTiles.ContainsKey(tile))
			{
				return;
			}

			_activeTiles.Remove(tile);
			OnTileRemoved(tile);
		}

		internal abstract void OnInitialized();
	}
}
