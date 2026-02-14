using System;

namespace Mapbox.BaseModule.Utilities.Attributes
{
	/// <summary>
	/// Attribute to specify the URL string representation of an enum value.
	/// Used for converting enum values to their API URL parameter equivalents.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class MapboxRequestStringAttribute : Attribute
	{
		private readonly string urlString;

		/// <summary>Gets the URL string value.</summary>
		public string Value { get { return urlString; } }

		/// <summary>
		/// Initializes a new instance of the MapboxRequestStringAttribute.
		/// </summary>
		/// <param name="urlString">The URL string representation of the enum value.</param>
		public MapboxRequestStringAttribute(string urlString)
		{
			this.urlString = urlString;
		}
	}
}
