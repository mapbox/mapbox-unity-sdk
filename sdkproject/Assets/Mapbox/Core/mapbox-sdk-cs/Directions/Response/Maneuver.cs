//-----------------------------------------------------------------------
// <copyright file="Maneuver.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Directions
{
    using Mapbox.Json;
    using Mapbox.Utils;

    /// <summary>
    /// A Maneuver from a directions API call.
    /// </summary>
    public class Maneuver
	{
		/// <summary>
		/// Gets or sets the bearing after.
		/// </summary>
		/// <value>The bearing after.</value>
		[JsonProperty("bearing_after")]
		public int BearingAfter { get; set; }

		/// <summary>
		/// Gets or sets the type.
		/// </summary>
		/// <value>The type.</value>
		[JsonProperty("type")]
		public string Type { get; set; }

		/// <summary>
		/// Gets or sets the modifier.
		/// </summary>
		/// <value>The modifier.</value>
		[JsonProperty("modifier")]
		public string Modifier { get; set; }

		/// <summary>
		/// Gets or sets the bearing before.
		/// </summary>
		/// <value>The bearing before.</value>
		[JsonProperty("bearing_before")]
		public int BearingBefore { get; set; }

		/// <summary>
		/// Gets or sets the location.
		/// </summary>
		/// <value>The location.</value>
		[JsonProperty("Location")]
		public Vector2d Location { get; set; }

		/// <summary>
		/// Gets or sets the instruction.
		/// </summary>
		/// <value>The instruction.</value>
		[JsonProperty("instruction")]
		public string Instruction { get; set; }
	}
}
