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
			// ref https://rosettacode.org/wiki/Averages/Mean_angle and https://rosettacode.org/wiki/Averages/Mean_angle#C.23
			double cos = _angles.Sum(a => Math.Cos(a * Math.PI / 180.0)) / _angles.Count;
			double sin = _angles.Sum(a => Math.Sin(a * Math.PI / 180.0)) / _angles.Count;

			double finalAngle = (Math.Atan2(sin, cos) * 180.0 / Math.PI);
			//stay positive ;-)
			if (finalAngle < 0) { finalAngle += 360.0; }

			return finalAngle;
		}



	}
}
