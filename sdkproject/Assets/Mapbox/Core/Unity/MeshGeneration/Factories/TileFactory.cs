namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	public abstract class TileFactory : Factory
	{
		protected Dictionary<Vector2, UnityTile> _tiles;

		public override void Initialize(Platform.IFileSource fileSource)
		{
			base.Initialize(fileSource);
			_tiles = new Dictionary<Vector2, UnityTile>();
		}

		public override void Register(UnityTile tile)
		{
			base.Register(tile);
			_tiles.Add(tile.TileCoordinate, tile);
			OnRegistered(tile);
		}

		public override void Unregister(UnityTile tile)
		{
			base.Unregister(tile);
			_tiles.Remove(tile.TileCoordinate);
			// TODO: cancel tile requests!
			OnUnregistered(tile);
		}

		internal abstract void OnRegistered(UnityTile tile);

		// TODO: needed?
		internal abstract void OnUnregistered(UnityTile tile);
	}
}
