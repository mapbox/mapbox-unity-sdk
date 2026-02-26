//-----------------------------------------------------------------------
// <copyright file="MapMatchingParameters.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


using Mapbox.BaseModule.Utilities.Attributes;

namespace Mapbox.DirectionsApi.MapMatching
{
	/// <summary>Directions profile id</summary>
	public enum Profile
	{
		[MapboxRequestString("mapbox/driving")]
		MapboxDriving,
		[MapboxRequestString("mapbox/driving-traffic")]
		MapboxDrivingTraffic,
		[MapboxRequestString("mapbox/walking")]
		MapboxWalking,
		[MapboxRequestString("mapbox/cycling")]
		MapboxCycling
	}


	/// <summary>Format of the returned geometry. Default value 'Polyline' with precision 5.</summary>
	public enum Geometries
	{
		/// <summary>Default, precision 5.</summary>
		[MapboxRequestString("polyline")]
		Polyline,
		/// <summary>Precision 6.</summary>
		[MapboxRequestString("polyline6")]
		Polyline6,
		/// <summary>Geojson.</summary>
		[MapboxRequestString("geojson")]
		GeoJson
	}


	/// <summary>Type of returned overview geometry. </summary>
	public enum Overview
	{
		/// <summary>The most detailed geometry available </summary>
		[MapboxRequestString("full")]
		Full,
		/// <summary>A simplified version of the full geometry</summary>
		[MapboxRequestString("simplified")]
		Simplified,
		/// <summary>No overview geometry </summary>
		[MapboxRequestString("false")]
		None
	}


	/// <summary>Whether or not to return additional metadata along the route. Several annotations can be used.</summary>
	[System.Flags]
	public enum Annotations
	{
		[MapboxRequestString("duration")]
		Duration = 1,
		[MapboxRequestString("distance")]
		Distance = 2,
		[MapboxRequestString("speed")]
		Speed = 4,
		[MapboxRequestString("congestion")]
		Congestion = 8
	}


	/// <summary>
	/// https://www.mapbox.com/api-documentation/navigation/#retrieve-directions
	/// </summary>
	public enum InstructionLanguages
	{
		[MapboxRequestString("de")]
		German,
		[MapboxRequestString("en")]
		English,
		[MapboxRequestString("eo")]
		Esperanto,
		[MapboxRequestString("es")]
		Spanish,
		[MapboxRequestString("es-ES")]
		SpanishSpain,
		[MapboxRequestString("fr")]
		French,
		[MapboxRequestString("id")]
		Indonesian,
		[MapboxRequestString("it")]
		Italian,
		[MapboxRequestString("nl")]
		Dutch,
		[MapboxRequestString("pl")]
		Polish,
		[MapboxRequestString("pt-BR")]
		PortugueseBrazil,
		[MapboxRequestString("ro")]
		Romanian,
		[MapboxRequestString("ru")]
		Russian,
		[MapboxRequestString("sv")]
		Swedish,
		[MapboxRequestString("tr")]
		Turkish,
		[MapboxRequestString("uk")]
		Ukrainian,
		[MapboxRequestString("vi")]
		Vietnamese,
		[MapboxRequestString("zh-Hans")]
		ChineseSimplified
	}


}
