using System;
using System.Reflection;
using Mapbox.BaseModule.Utilities.Attributes;

namespace Mapbox.BaseModule.Utilities
{
	/// <summary>
	/// Extension methods for Enum types.
	/// </summary>
	public static class EnumExtensions
	{
		/// <summary>
		/// Gets the URL string representation of an enum value.
		/// Looks for the UrlStringAttribute on the enum value.
		/// If no attribute is found, returns the enum's ToString() value.
		/// </summary>
		/// <param name="value">The enum value.</param>
		/// <returns>The URL string from the UrlStringAttribute, or ToString() if not found.</returns>
		public static string MapboxTypeDescription(this Enum value)
		{
			if (value == null)
			{
				return string.Empty;
			}

			Type type = value.GetType();
			string name = Enum.GetName(type, value);

			if (string.IsNullOrEmpty(name))
			{
				return value.ToString();
			}

			FieldInfo field = type.GetField(name);

			if (field == null)
			{
				return value.ToString();
			}

			// Try to get the UrlStringAttribute
			MapboxRequestStringAttribute attribute = Attribute.GetCustomAttribute(field, typeof(MapboxRequestStringAttribute)) as MapboxRequestStringAttribute;

			if (attribute != null)
			{
				return attribute.Value;
			}

			// Fallback to ToString if no attribute found
			return value.ToString();
		}
	}
}
