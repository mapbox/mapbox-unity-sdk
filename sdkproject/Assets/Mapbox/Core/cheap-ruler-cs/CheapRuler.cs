namespace Mapbox
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;


	public enum CheapRulerUnits
	{
		Kilometers,
		Miles,
		NauticalMiles,
		Meters,
		Yards,
		Feet,
		Inches
	}

	public class CheapRuler
	{


		private CheapRulerUnits _outputUnits;


		/// <summary>
		/// Creates a ruler object that will approximate measurements around the given latitude. Units are one of: kilometers
		/// </summary>
		/// <param name="outputUnits"></param>
		public CheapRuler(CheapRulerUnits outputUnits)
		{
			if (CheapRulerUnits.Kilometers != outputUnits)
			{
				throw new Exception(string.Format("{0} not implemented", outputUnits));
			}
			_outputUnits = outputUnits;
		}


		/// <summary>
		/// Creates a ruler object from tile coordinates.
		/// </summary>
		/// <param name="y">Y TileId</param>
		/// <param name="z">Zoom Level</param>
		/// <param name="units"></param>
		/// <returns></returns>
		public static CheapRuler FromTile(int y, int z, CheapRulerUnits units)
		{

		}


		/// <summary>
		/// Given two points returns the distance.
		/// </summary>
		/// <param name="longitudeFrom"></param>
		/// <param name="latitudeFrom"></param>
		/// <param name="longitudeTo"></param>
		/// <param name="latitudeTo"></param>
		/// <returns></returns>
		public double Distance(double longitudeFrom, double latitudeFrom, double longitudeTo, double latitudeTo)
		{

		}


		/// <summary>
		/// Returns the bearing between two points in angles.
		/// </summary>
		/// <param name="longitudeFrom"></param>
		/// <param name="latitudeFrom"></param>
		/// <param name="longitudeTo"></param>
		/// <param name="latitudeTo"></param>
		/// <returns></returns>
		public double Bearing(double longitudeFrom, double latitudeFrom, double longitudeTo, double latitudeTo)
		{

		}


		/// <summary>
		/// Returns a new point given distance and bearing from the starting point.
		/// </summary>
		/// <param name="longitude"></param>
		/// <param name="latitude"></param>
		/// <param name="distance"></param>
		/// <param name="bearing"></param>
		/// <returns></returns>
		public double Destination(double longitude, double latitude, double distance, double bearing)
		{

		}


	}
}