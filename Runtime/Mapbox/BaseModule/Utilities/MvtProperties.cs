using System.Collections.Generic;

namespace Mapbox.BaseModule.Utilities
{
	/// <summary>
	/// Fixes up feature properties as they leave the vector-tile decoder.
	/// </summary>
	public static class MvtProperties
	{
		/// <summary>
		/// Rewrites every <c>ulong</c> property value as a <c>long</c>, preserving the bits.
		///
		/// The vector-tile-cs decoder reads all varints as unsigned, so an MVT <c>int_value</c> - a
		/// signed int64 - arrives boxed as <c>ulong</c>. A negative one therefore reads as an enormous
		/// positive number: a contour band 200 m below sea level comes back as
		/// 18446744073709551416 instead of -200, and both San Francisco and the Amazon ship those
		/// bands. Any consumer doing <c>Convert.ToInt32(properties["ele"])</c> gets garbage.
		///
		/// Reinterpreting the bits is lossless for values that really were unsigned and below
		/// <see cref="long.MaxValue"/>, and restores the sign for the rest. A genuine unsigned value
		/// above long.MaxValue would be misread, but no map tileset carries one.
		///
		/// Nothing to do for the reader in <c>Mapbox.VectorModule.ComponentSystem.Data</c>, which
		/// already boxes these as <c>long</c>.
		/// </summary>
		public static void NormalizeIntegers(Dictionary<string, object> properties)
		{
			if (properties == null) return;

			// Two passes so the common case - no unsigned value in the dictionary - allocates
			// nothing. A dictionary cannot be modified while it is being enumerated, so the keys to
			// rewrite are collected first, and only when there is at least one.
			var hasUnsigned = false;
			foreach (var property in properties)
			{
				if (property.Value is ulong)
				{
					hasUnsigned = true;
					break;
				}
			}

			if (!hasUnsigned) return;

			List<string> keys = null;
			foreach (var property in properties)
			{
				if (property.Value is ulong)
				{
					keys ??= new List<string>(2);
					keys.Add(property.Key);
				}
			}

			foreach (var key in keys)
			{
				properties[key] = unchecked((long)(ulong)properties[key]);
			}
		}
	}
}
