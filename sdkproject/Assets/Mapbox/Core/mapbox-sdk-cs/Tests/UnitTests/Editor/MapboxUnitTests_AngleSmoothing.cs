namespace Mapbox.MapboxSdkCs.UnitTest
{


	using NUnit.Framework;
	using Mapbox.Unity.Location;


	[TestFixture]
	internal class AngleSmoothingTest
	{


		[Test]
		public void NoOp()
		{
			//NoOp does nothing. should always return latest value
			AngleSmoothingNoOp noop = new AngleSmoothingNoOp();
			string opName = noop.GetType().Name + " ";
			noop.Add(23);
			Assert.AreEqual(23d, noop.Calculate(), opName + "did modify data");
			noop.Add(29);
			Assert.AreEqual(29d, noop.Calculate(), opName + "did modify data");
			noop.Add(178.5);
			Assert.AreEqual(178.5d, noop.Calculate(), opName + "did modify data");
			noop.Add(359.99);
			Assert.AreEqual(359.99d, noop.Calculate(), opName + "did modify data");
		}


		[Test]
		public void Average()
		{
			// simple average
			AngleSmoothingAverage avg = new AngleSmoothingAverage();
			string opName = avg.GetType().Name + " ";
			avg.Add(355);
			avg.Add(15);
			Assert.AreEqual(5d, avg.Calculate(), opName + "did not properly calculate across 0");

			avg.Add(335);
			Assert.AreEqual(355d, avg.Calculate(), opName + "did not properly calculate back across 0");

			// add more angles to check if internal buffer rolls over correctly
			avg.Add(180);
			avg.Add(160);
			avg.Add(140);
			avg.Add(120);
			avg.Add(100);
			Assert.AreEqual(140d, avg.Calculate(), opName + "internal default buffer of 5 did not roll over correctly");
		}


		[Test]
		public void LowPass()
		{
			//simple low pass filter
			// parameter 1.0 => no smoothing: despite calucations going on last value should be returned
			AngleSmoothingLowPass lp = new AngleSmoothingLowPass(1.0d);
			string opName = lp.GetType().Name + " ";
			lp.Add(45);
			lp.Add(90);
			lp.Add(135);
			lp.Add(180);
			lp.Add(225);
			Assert.AreEqual(225d, lp.Calculate(), opName + "smoothed but shouldn't have");

			lp = new AngleSmoothingLowPass(0.5d);
			lp.Add(45);
			lp.Add(90);
			lp.Add(135);
			lp.Add(180);
			lp.Add(225);
			Assert.AreEqual(193.75d, lp.Calculate(), opName + "did not smooth as expected");

			// parameter 1.0 => no smoothing: despite calucations going on last value should be returned
			lp = new AngleSmoothingLowPass(1d);
			lp.Add(355);
			lp.Add(355);
			lp.Add(5);
			lp.Add(5);
			lp.Add(5);
			Assert.AreEqual(5d, lp.Calculate(), opName + "did not *not* smooth across '0' as expected");

			lp = new AngleSmoothingLowPass(0.5d);
			lp.Add(355);
			lp.Add(355);
			lp.Add(10);
			lp.Add(10);
			lp.Add(10);
			Assert.AreEqual(8.14d, lp.Calculate(), opName + "did not smooth across '0' as expected");
			lp.Add(320);
			Assert.AreEqual(344.02d, lp.Calculate(), opName + "did not smooth back across '0' as expected");
		}


		[Test]
		public void Weighted()
		{
			// exponential moving average
			// parameter 1.0 => no smoothing
			AngleSmoothingEMA ema = new AngleSmoothingEMA();
			string opName = ema.GetType().Name + " ";
			ema.Add(45);
			ema.Add(90);
			ema.Add(135);
			ema.Add(180);
			ema.Add(225);
			Assert.AreEqual(165.74d, ema.Calculate(), opName + "didn't smooth as expected");

			ema = new AngleSmoothingEMA();
			ema.Add(350);
			ema.Add(355);
			ema.Add(5);
			ema.Add(10);
			ema.Add(10);
			Assert.AreEqual(3.85d, ema.Calculate(), opName + "didn't smooth as expected across 0");

			ema = new AngleSmoothingEMA();
			ema.Add(20);
			ema.Add(15);
			ema.Add(350);
			ema.Add(345);
			ema.Add(330);
			Assert.AreEqual(350.43d, ema.Calculate(), opName + "didn't smooth as expected back across 0");
		}




	}
}
