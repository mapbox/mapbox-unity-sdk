namespace Mapbox.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Collections.Generic;

	public class TileErrorEventArgs {
		public CanonicalTileId TileId;
		public List<Exception> Exceptions;
		public UnityTile UnityTileInstance;
		public Type TileType;


		public TileErrorEventArgs(CanonicalTileId TileId, Type TileType, UnityTile UnityTileInstance, List<Exception> Exceptions)
		{
			this.TileId = TileId;
			this.Exceptions = Exceptions;
			this.UnityTileInstance = UnityTileInstance;
			this.TileType = TileType;
		}
	}
}