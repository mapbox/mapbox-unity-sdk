namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using Mapbox.Platform;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	public abstract class AbstractTileFactory : ScriptableObject
	{
		protected IFileSource FileSource;

		// TODO: can we change this dictionary? Do we need Vector2 key?
		protected Dictionary<Vector2, UnityTile> _unityTiles;

		public void Initialize(IFileSource fileSource)
		{
			FileSource = fileSource;
			_unityTiles = new Dictionary<Vector2, UnityTile>();
			OnInitialized();
		}

		public void Register(UnityTile tile)
		{
			_unityTiles.Add(tile.TileCoordinate, tile);
			OnRegistered(tile);
		}

		public void Unregister(UnityTile tile)
		{
			_unityTiles.Remove(tile.TileCoordinate);
			OnUnregistered(tile);
		}

		internal abstract void OnInitialized();

		internal abstract void OnRegistered(UnityTile tile);

		internal abstract void OnUnregistered(UnityTile tile);
	}
}
