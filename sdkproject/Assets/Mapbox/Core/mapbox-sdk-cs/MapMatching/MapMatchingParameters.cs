//-----------------------------------------------------------------------
// <copyright file="MapMatchingParameters.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapMatching
{

	/// <summary>Directions profile id</summary>
	public struct Profile
	{
		public const string MapboxDriving = "mapbox/driving";
		public const string MapboxDrivingTraffic = "mapbox/driving-traffic";
		public const string MapboxWalking = "mapbox/walking";
		public const string MapboxCycling = "mapbox/cycling";
	}


	/// <summary>Format of the returned geometry. Default value 'Polyline' with precision 5.</summary>
	public struct Geometries
	{
		/// <summary>Default, precision 5.</summary>
		public const string Polyline = "polyline";
		/// <summary>Precision 6.</summary>
		public const string Polyline6 = "polyline6";
	}


	/// <summary>Type of returned overview geometry. </summary>
	public struct Overview
	{
		/// <summary>The most detailed geometry available </summary>
		public const string Full= "full";
		/// <summary>A simplified version of the full geometry</summary>
		public const string Simplified = "simplified";
		/// <summary>No overview geometry </summary>
		public const string None = "false";

	}


	/// <summary>Whether or not to return additional metadata along the route. Several annotations can be used.</summary>
	public struct Annotations
	{
		/// <summary></summary>
		public const string Duration = "duration";
		/// <summary></summary>
		public const string Distance = "distance";
		/// <summary></summary>
		public const string Speed = "speed";
	}


	/// <summary>
	/// https://www.mapbox.com/api-documentation/#retrieve-directions
	/// </summary>
	public struct InstructionLanguages
	{
		public const string German= "de";
		public const string English= "en";
		public const string Esperanto= "eo";
		public const string Spanish= "es";
		public const string SpanishSpain= "es-ES";
		public const string French= "fr";
		public const string Indonesian= "id";
		public const string Italian= "it";
		public const string Dutch= "nl";
		public const string Polish= "pl";
		public const string PortugueseBrazil= "pt-BR";
		public const string Romanian= "ro";
		public const string Russian= "ru";
		public const string Swedish= "sv";
		public const string Turkish= "tr";
		public const string Ukrainian = "uk";
		public const string Vietnamese= "vi";
		public const string ChineseSimplified= "zh-Hans";
	}

}