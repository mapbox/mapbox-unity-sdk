//-----------------------------------------------------------------------
// <copyright file="MapMatchingParameters.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.ComponentModel;
using Mapbox.VectorTile.Geometry;

namespace Mapbox.MapMatching
{
	/// <summary>Directions profile id</summary>
	public enum Profile
	{
		[Description("mapbox/driving")]
		MapboxDriving,
		[Description("mapbox/driving-traffic")]
		MapboxDrivingTraffic,
		[Description("mapbox/walking")]
		MapboxWalking,
		[Description("mapbox/cycling")]
		MapboxCycling
	}


	/// <summary>Format of the returned geometry. Default value 'Polyline' with precision 5.</summary>
	public enum Geometries
	{
		/// <summary>Default, precision 5.</summary>
		[Description("polyline")]
		Polyline,
		/// <summary>Precision 6.</summary>
		[Description("polyline6")]
		Polyline6,
		/// <summary>Geojson.</summary>
		[Description("geojson")]
		GeoJson
	}


	/// <summary>Type of returned overview geometry. </summary>
	public enum Overview
	{
		/// <summary>The most detailed geometry available </summary>
		[Description("full")]
		Full,
		/// <summary>A simplified version of the full geometry</summary>
		[Description("simplified")]
		Simplified,
		/// <summary>No overview geometry </summary>
		[Description("false")]
		None
	}


	/// <summary>Whether or not to return additional metadata along the route. Several annotations can be used.</summary>
	[System.Flags]
	public enum Annotations
	{
		[Description("duration")]
		Duration,
		[Description("distance")]
		Distance,
		[Description("speed")]
		Speed,
		[Description("congestion")]
		Congestion
	}


	/// <summary>
	/// https://www.mapbox.com/api-documentation/navigation/#retrieve-directions
	/// </summary>
	public enum InstructionLanguages
	{
		[Description("de")]
		German,
		[Description("en")]
		English,
		[Description("eo")]
		Esperanto,
		[Description("es")]
		Spanish,
		[Description("es-ES")]
		SpanishSpain,
		[Description("fr")]
		French,
		[Description("id")]
		Indonesian,
		[Description("it")]
		Italian,
		[Description("nl")]
		Dutch,
		[Description("pl")]
		Polish,
		[Description("pt-BR")]
		PortugueseBrazil,
		[Description("ro")]
		Romanian,
		[Description("ru")]
		Russian,
		[Description("sv")]
		Swedish,
		[Description("tr")]
		Turkish,
		[Description("uk")]
		Ukrainian,
		[Description("vi")]
		Vietnamese,
		[Description("zh-Hans")]
		ChineseSimplified
	}


}
