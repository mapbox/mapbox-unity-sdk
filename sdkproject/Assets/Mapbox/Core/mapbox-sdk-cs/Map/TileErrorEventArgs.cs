namespace Mapbox.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	public class TileErrorEventArgs:EventArgs {

		/// <summary>
		/// The tile identifier.
		/// </summary>
		public CanonicalTileId TileId;
		/// <summary>
		/// The exceptions.
		/// </summary>
		public List<Exception> Exceptions;
		/// <summary>
		/// The unity tile instance.
		/// </summary>
		public UnityTile UnityTileInstance;
		/// <summary>
		/// The type of the tile.
		/// </summary>
		public Type TileType;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mapbox.Map.TileErrorEventArgs"/> class.
		/// </summary>
		/// <param name="TileId">Tile identifier.</param>
		/// <param name="TileType">Tile type.</param>
		/// <param name="UnityTileInstance">Unity tile instance.</param>
		/// <param name="Exceptions">Exceptions as a List</param>
		public TileErrorEventArgs(CanonicalTileId TileId, Type TileType, UnityTile UnityTileInstance, List<System.Exception> Exceptions)
		{
			this.TileId = TileId;
			this.Exceptions = Exceptions;
			this.UnityTileInstance = UnityTileInstance;
			this.TileType = TileType;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mapbox.Map.TileErrorEventArgs"/> class.
		/// </summary>
		/// <param name="TileId">Tile identifier.</param>
		/// <param name="TileType">Tile type.</param>
		/// <param name="UnityTileInstance">Unity tile instance.</param>
		/// <param name="Exceptions">Exceptions as a ReadOnlyCollection</param>
		public TileErrorEventArgs(CanonicalTileId TileId, Type TileType, UnityTile UnityTileInstance, ReadOnlyCollection<Exception> Exceptions)
		{
			this.TileId = TileId;
			List<Exception> _exceptions = new List<Exception>();
			foreach(var exception in Exceptions)
			{
				_exceptions.Add(exception);
			}
			this.Exceptions = _exceptions;
			this.UnityTileInstance = UnityTileInstance;
			this.TileType = TileType;
		}
	}
}