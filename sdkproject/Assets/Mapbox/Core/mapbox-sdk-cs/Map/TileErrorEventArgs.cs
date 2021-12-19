namespace Mapbox.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	public class TileErrorEventArgs : EventArgs
	{

		/// <summary>
		/// The tile identifier.
		/// </summary>
		public CanonicalTileId TileId;
		/// <summary>
		/// The exceptions.
		/// </summary>
		public List<Exception> Exceptions;
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
		public TileErrorEventArgs(CanonicalTileId TileId, Type TileType, List<System.Exception> Exceptions)
		{
			this.TileId = TileId;
			this.Exceptions = Exceptions;
			this.TileType = TileType;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mapbox.Map.TileErrorEventArgs"/> class.
		/// </summary>
		/// <param name="TileId">Tile identifier.</param>
		/// <param name="TileType">Tile type.</param>
		/// <param name="UnityTileInstance">Unity tile instance.</param>
		/// <param name="Exceptions">Exceptions as a ReadOnlyCollection</param>
		public TileErrorEventArgs(CanonicalTileId TileId, Type TileType, ReadOnlyCollection<Exception> Exceptions)
		{
			this.TileId = TileId;
			if (Exceptions != null)
			{
				List<Exception> _exceptions = new List<Exception>();
				foreach (var exception in Exceptions)
				{
					_exceptions.Add(exception);
				}
				this.Exceptions = _exceptions;
			}
			this.TileType = TileType;
		}
	}
}
