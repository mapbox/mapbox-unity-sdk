namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using Mapbox.Map;
	using Mapbox.Platform;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	public abstract class AbstractTileFactory : ScriptableObject
	{
		protected IFileSource _fileSource;

		protected Dictionary<UnityTile, Tile> _tiles;

		public void Initialize(IFileSource fileSource)
		{
			_fileSource = fileSource;
			_tiles = new Dictionary<UnityTile, Tile>();
			OnInitialized();
		}

		public void Register(UnityTile tile)
		{
			OnRegistered(tile);
		}

		public void Unregister(UnityTile tile)
		{
			Cancel(tile);
			OnUnregistered(tile);
		}

		void Cancel(UnityTile tile)
		{
			if (_tiles.ContainsKey(tile))
			{
				_tiles[tile].Cancel();
				_tiles.Remove(tile);
			}
		}

		internal abstract void OnInitialized();

		internal abstract void OnRegistered(UnityTile tile);

		internal abstract void OnUnregistered(UnityTile tile);
	}
}