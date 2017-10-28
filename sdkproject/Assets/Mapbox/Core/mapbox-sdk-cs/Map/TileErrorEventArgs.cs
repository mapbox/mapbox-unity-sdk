namespace Mapbox.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Collections.Generic;

	public class TileErrorEventArgs {
		public CanonicalTileId TileId { get; private set; }
		public List<Exception> Exceptions { get; private set; }
		public UnityTile UnityTileInstance { get; private set; }


		public TileErrorEventArgs(CanonicalTileId TileId, UnityTile UnityTileInstance, List<Exception> Exceptions)
		{
			this.TileId = TileId;
			this.Exceptions = Exceptions;
			this.UnityTileInstance = UnityTileInstance;
		}
	}
}