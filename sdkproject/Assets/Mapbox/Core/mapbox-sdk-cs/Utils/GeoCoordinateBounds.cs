//-----------------------------------------------------------------------
// <copyright file="Vector2dBounds.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils
{
	/// <summary> Represents a bounding box derived from a southwest corner and a northeast corner. </summary>
	public struct Vector2dBounds
	{
		/// <summary> Southwest corner of bounding box. </summary>
		public Vector2d SouthWest;

		/// <summary> Northeast corner of bounding box. </summary>
		public Vector2d NorthEast;

		/// <summary> Initializes a new instance of the <see cref="Vector2dBounds" /> struct. </summary>
		/// <param name="sw"> Geographic coordinate representing southwest corner of bounding box. </param>
		/// <param name="ne"> Geographic coordinate representing northeast corner of bounding box. </param>
		public Vector2dBounds(Vector2d sw, Vector2d ne)
		{
			this.SouthWest = sw;
			this.NorthEast = ne;
		}

		/// <summary> Gets the south latitude. </summary>
		/// <value> The south latitude. </value>
		public double South {
			get {
				return this.SouthWest.x;
			}
		}

		/// <summary> Gets the west longitude. </summary>
		/// <value> The west longitude. </value>
		public double West {
			get {
				return this.SouthWest.y;
			}
		}

		/// <summary> Gets the north latitude. </summary>
		/// <value> The north latitude. </value>
		public double North {
			get {
				return this.NorthEast.x;
			}
		}

		/// <summary> Gets the east longitude. </summary>
		/// <value> The east longitude. </value>
		public double East {
			get {
				return this.NorthEast.y;
			}
		}

		/// <summary>
		///     Gets or sets the central coordinate of the bounding box. When
		///     setting a new center, the bounding box will retain its original size.
		/// </summary>
		/// <value> The central coordinate. </value>
		public Vector2d Center {
			get {
				var lat = (this.SouthWest.x + this.NorthEast.x) / 2;
				var lng = (this.SouthWest.y + this.NorthEast.y) / 2;

				return new Vector2d(lat, lng);
			}

			set {
				var lat = (this.NorthEast.x - this.SouthWest.x) / 2;
				this.SouthWest.x = value.x - lat;
				this.NorthEast.x = value.x + lat;

				var lng = (this.NorthEast.y - this.SouthWest.y) / 2;
				this.SouthWest.y = value.y - lng;
				this.NorthEast.y = value.y + lng;
			}
		}

		/// <summary>
		///     Creates a bound from two arbitrary points. Contrary to the constructor,
		///     this method always creates a non-empty box.
		/// </summary>
		/// <param name="a"> The first point. </param>
		/// <param name="b"> The second point. </param>
		/// <returns> The convex hull. </returns>
		public static Vector2dBounds FromCoordinates(Vector2d a, Vector2d b)
		{
			var bounds = new Vector2dBounds(a, a);
			bounds.Extend(b);

			return bounds;
		}

		/// <summary> A bounding box containing the world. </summary>
		/// <returns> The world bounding box. </returns>
		public static Vector2dBounds World()
		{
			var sw = new Vector2d(-90, -180);
			var ne = new Vector2d(90, 180);

			return new Vector2dBounds(sw, ne);
		}

		/// <summary> Extend the bounding box to contain the point. </summary>
		/// <param name="point"> A geographic coordinate. </param>
		public void Extend(Vector2d point)
		{
			if (point.x < this.SouthWest.x)
			{
				this.SouthWest.x = point.x;
			}

			if (point.x > this.NorthEast.x)
			{
				this.NorthEast.x = point.x;
			}

			if (point.y < this.SouthWest.y)
			{
				this.SouthWest.y = point.y;
			}

			if (point.y > this.NorthEast.y)
			{
				this.NorthEast.y = point.y;
			}
		}

		/// <summary> Extend the bounding box to contain the bounding box. </summary>
		/// <param name="bounds"> A bounding box. </param>
		public void Extend(Vector2dBounds bounds)
		{
			this.Extend(bounds.SouthWest);
			this.Extend(bounds.NorthEast);
		}

		/// <summary> Whenever the geographic bounding box is empty. </summary>
		/// <returns> <c>true</c>, if empty, <c>false</c> otherwise. </returns>
		public bool IsEmpty()
		{
			return this.SouthWest.x > this.NorthEast.x ||
					   this.SouthWest.y > this.NorthEast.y;
		}

		/// <summary>
		/// Converts to an array of doubles.
		/// </summary>
		/// <returns>An array of coordinates.</returns>
		public double[] ToArray()
		{
			double[] array =
			{
				this.SouthWest.x,
				this.SouthWest.y,
				this.NorthEast.x,
				this.NorthEast.y
			};

			return array;
		}

		/// <summary> Converts the Bbox to a URL snippet. </summary>
		/// <returns> Returns a string for use in a Mapbox query URL. </returns>
		public override string ToString()
		{
			return string.Format("{0},{1}", this.SouthWest.ToString(), this.NorthEast.ToString());
		}
	}
}
