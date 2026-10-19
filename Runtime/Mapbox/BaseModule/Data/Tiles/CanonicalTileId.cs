//-----------------------------------------------------------------------
// <copyright file="CanonicalTileId.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using UnityEngine;

namespace Mapbox.BaseModule.Data.Tiles
{
	/// <summary>
	/// Data type to store  <see href="https://en.wikipedia.org/wiki/Web_Mercator"> Web Mercator</see> tile scheme.
	/// <see href="http://www.maptiler.org/google-maps-coordinates-tile-bounds-projection/"> See tile IDs in action. </see>
	/// </summary>
	[Serializable]
	public struct CanonicalTileId : IEquatable<CanonicalTileId>
	{
		/// <summary> The zoom level. </summary>
		public int Z;

		/// <summary> The X coordinate in the tile grid. </summary>
		public int X;

		/// <summary> The Y coordinate in the tile grid. </summary>
		public int Y;

		/// <summary>
		///     Initializes a new instance of the <see cref="CanonicalTileId"/> struct,
		///     representing a tile coordinate in a slippy map.
		/// </summary>
		/// <param name="z"> The z coordinate or the zoom level. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		public CanonicalTileId(int z, int x, int y)
		{
			this.Z = z;
			this.X = x;
			this.Y = y;
		}

		internal CanonicalTileId(UnwrappedTileId unwrapped)
		{
			var z = unwrapped.Z;
			var x = unwrapped.X;
			var y = unwrapped.Y;

			var wrap = (x < 0 ? x - (1 << z) + 1 : x) / (1 << z);

			this.Z = z;
			this.X = x - wrap * (1 << z);
			this.Y = y < 0 ? 0 : Math.Min(y, (1 << z) - 1);
		}

		public static CanonicalTileId FromUnwrappedValues(int z, int x, int y)
		{
			var wrap = (x < 0 ? x - (1 << z) + 1 : x) / (1 << z);
			return new CanonicalTileId(z, x - wrap * (1 << z), y < 0 ? 0 : Math.Min(y, (1 << z) - 1));
		}

		/// <summary>
		///     Get the cordinate at the top left of corner of the tile.
		/// </summary>
		/// <returns> The coordinate. </returns>
		public Vector2d.Vector2d ToVector2d()
		{
			double n = Math.PI - ((2.0 * Math.PI * this.Y) / Math.Pow(2.0, this.Z));

			double lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
			double lng = (this.X / Math.Pow(2.0, this.Z) * 360.0) - 180.0;

			// FIXME: Super hack because of rounding issues.
			return new Vector2d.Vector2d(lat - 0.0001, lng + 0.0001);
		}

		/// <summary>
		///     Returns a <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.BaseModule.Data.Tiles.CanonicalTileId"/>.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.BaseModule.Data.Tiles.CanonicalTileId"/>.
		/// </returns>
		public override string ToString()
		{
			return string.Format("{0}/{1}/{2}", this.Z, this.X, this.Y);
		}

		public CanonicalTileId GetParentTileId
		{
			get
			{
				return new CanonicalTileId(Z - 1, X >> 1, Y >> 1);
			}
		}
		
		public void MoveToParent()
		{
			this.Z = this.Z - 1;
			this.X = this.X >> 1;
			this.Y = this.Y >> 1;
		}

		#region Equality 
		public bool Equals(CanonicalTileId other)
		{
			return this.X == other.X && this.Y == other.Y && this.Z == other.Z;
		}
		
		public override int GetHashCode()
		{
			//old hashcode
			//return X ^ Y ^ Z;

			int hash = X.GetHashCode();
			hash = (hash * 397) ^ Y.GetHashCode();
			hash = (hash * 397) ^ Z.GetHashCode();

			return hash;
		}

		public static bool operator ==(CanonicalTileId a, CanonicalTileId b)
		{
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}

		public static bool operator !=(CanonicalTileId a, CanonicalTileId b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj is CanonicalTileId)
			{
				return this.Equals((CanonicalTileId)obj);
			}
			else
			{
				return false;
			}
		}

		#endregion

		/// <summary>
		/// The ancestor of this tile at zoom level <paramref name="i"/>, or this tile itself if it is
		/// already at or above that level. Does not modify this tile - use <see cref="MoveToParent"/>
		/// when the mutation is what you want.
		/// </summary>
		public readonly CanonicalTileId ParentAt(int i)
		{
			if (Z <= i)
			{
				return this;
			}

			var delta = Z - i; //zoom level diff
			return new CanonicalTileId(i, X >> delta, Y >> delta);
		}

		public readonly bool IsParentOf(CanonicalTileId canonicalTileId)
		{
			return (this == canonicalTileId.ParentAt(this.Z));
		}
	}

	public static class TileIdExtensions
	{
		public static Vector4 CalculateScaleOffsetAtZoom(this CanonicalTileId current, int zoomDiff)
		{
			var tileZoom = current.Z;

			var scale = 1f;
			var offsetX = 0f;
			var offsetY = 0f;

			var currentParent = current.GetParentTileId;

			for (int i = tileZoom - 1; i >= zoomDiff; i--)
			{
				scale /= 2;

				var bottomLeftChildX = currentParent.X * 2;
				var bottomLeftChildY = currentParent.Y * 2;

				//top left
				if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY)
				{
					offsetX = offsetX / 2;
					offsetY = 0.5f + (offsetY / 2);
				}
				//top right
				else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY)
				{
					offsetX = 0.5f + (offsetX / 2);
					offsetY = 0.5f + (offsetY / 2);
				}
				//bottom left
				else if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY + 1)
				{
					offsetX = offsetX / 2;
					offsetY = offsetY / 2;
				}
				//bottom right
				else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY + 1)
				{
					offsetX = 0.5f + (offsetX / 2);
					offsetY = offsetY / 2;
				}

				current = currentParent;
				currentParent.MoveToParent();
			}

			return new Vector4(scale, scale, offsetX, offsetY);
		}
		
		public static Vector4 CalculateTopLeftScaleOffsetAtZoom(this CanonicalTileId current, int zoomDiff)
		{
			var tileZoom = current.Z;

			var scale = 1f;
			var offsetX = 0f;
			var offsetY = 0f;

			var currentParent = current.GetParentTileId;

			for (int i = tileZoom - 1; i >= zoomDiff; i--)
			{
				scale /= 2;

				var bottomLeftChildX = currentParent.X * 2;
				var bottomLeftChildY = currentParent.Y * 2;

				//top left
				if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY)
				{
					offsetX = offsetX / 2;
					offsetY = offsetY / 2;
					
					
				}
				//top right
				else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY)
				{
					offsetX = 0.5f + (offsetX / 2);
					offsetY = offsetY / 2;
				}
				//bottom left
				else if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY + 1)
				{
					offsetX = offsetX / 2;
					offsetY = 0.5f + (offsetY / 2);
				}
				//bottom right
				else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY + 1)
				{
					offsetX = 0.5f + (offsetX / 2);
					offsetY = 0.5f + (offsetY / 2);
				}

				current = currentParent;
				currentParent.MoveToParent();
			}

			return new Vector4(scale, scale, offsetX, offsetY);
		}

		public static CanonicalTileId Quadrant(this CanonicalTileId id, int i)
		{
			var childX  = (id.X << 1) + (i % 2);
			var childY  = (id.Y << 1) + (i >> 1);
			return new CanonicalTileId(id.Z + 1, childX, childY);
		}
		
		public static UnwrappedTileId Quadrant(this UnwrappedTileId id, int i)
		{
			var childX  = (id.X << 1) + (i % 2);
			var childY  = (id.Y << 1) + (i >> 1);
			return new UnwrappedTileId(id.Z + 1, childX, childY);
		}
	}
}
