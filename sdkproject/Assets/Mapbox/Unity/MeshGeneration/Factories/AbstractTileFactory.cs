namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using Mapbox.Platform;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	public abstract class AbstractTileFactory : ScriptableObject
	{
		protected IFileSource FileSource;
		protected Dictionary<Vector2, UnityTile> _tiles;

		public void Initialize(IFileSource fileSource)
		{
			FileSource = fileSource;
			_tiles = new Dictionary<Vector2, UnityTile>();
			OnInitialized();
		}

		public void Register(UnityTile tile)
		{
			_tiles.Add(tile.TileCoordinate, tile);
			OnRegistered(tile);
		}

		public void Unregister(UnityTile tile)
		{
			_tiles.Remove(tile.TileCoordinate);

			// TODO: cancel tile requests!
			OnUnregistered(tile);
		}

		internal abstract void OnInitialized();

		internal abstract void OnRegistered(UnityTile tile);

		internal abstract void OnUnregistered(UnityTile tile);
	}
}
