namespace Mapbox.Unity.Location
{


	using System;
	using System.Linq;


	/// <summary>
	/// Simple averaging latest 'n' values.
	/// </summary>
	public class AngleSmoothingAverage : AngleSmoothingAbstractBase
	{


		public override double Calculate()
		{

			// calc mean heading taking into account that eg 355° and 5° should result in 0° and not 180°
			// refs:
			// https://en.wikipedia.org/wiki/Mean_of_circular_quantities
			// https://rosettacode.org/wiki/Averages/Mean_angle
			// https://rosettacode.org/wiki/Averages/Mean_angle#C.23
			double cos = _angles.Sum(a => Math.Cos(a * DEG2RAD)) / _angles.Count;
			double sin = _angles.Sum(a => Math.Sin(a * DEG2RAD)) / _angles.Count;

			// round as we don't need super high precision
			double finalAngle = Math.Round(Math.Atan2(sin, cos) * RAD2DEG, 2);
			debugLogAngle(finalAngle, finalAngle);
			// stay within [0..<360]
			finalAngle = finalAngle < 0 ? finalAngle + 360 : finalAngle >= 360 ? finalAngle - 360 : finalAngle;
			debugLogAngle(finalAngle, finalAngle);

			return finalAngle;
		}



	}
}
