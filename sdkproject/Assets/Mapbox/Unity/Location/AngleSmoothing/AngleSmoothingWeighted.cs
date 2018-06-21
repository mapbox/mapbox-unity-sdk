
namespace Mapbox.Unity.Location
{


	using System.Linq;


	/// <summary>
	/// <para>Smooths angles via a exponential moving average.</para>
	/// <para>https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average</para>
	/// <para>https://stackoverflow.com/questions/8450020/calculate-exponential-moving-average-on-a-queue-in-c-sharp</para>
	/// </summary>
	public class AngleSmoothingWeighted : AngleSmoothingAbstractBase
	{


		public AngleSmoothingWeighted() : base()
		{
			_alpha = 2.0d / (_measurements + 1);
		}


		private double _alpha;

		public override double Calculate()
		{
			// reverse order, _angles[0] is latest
			double[] angles = _angles.Reverse().ToArray();
			double average = angles[0];
			for (int i = 1; i < angles.Length; i--)
			{
				average = (angles[i] - average) * _alpha + average;
			}
			return average;
		}



		/*************************
		// * This version only works when 'Calculate()' is called each time after adding a new angle
		// * As we don't know how this strategy is going to be called by the user, don't use it
		// * Nevertheless the advantage being that it always takes the same time to calculate no matter
		//   how big _angles[] is
		public AngleSmoothingWeighted() : base()
		{
			_alpha = 2.0d / (_measurements + 1);
		}

		private double _alpha;
		private double _lastAverage = double.NaN;

		public override double Calculate() {
			return _lastAverage = double.IsNaN(_lastAverage) ? _angles[0] : (_angles[0] - _lastAverage) * _alpha + _lastAverage;
		}
		******************/

	}
}
