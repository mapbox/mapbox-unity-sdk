
namespace Mapbox.Unity.UnitTest
{

	using System.Collections.Generic;
	using System.Linq;
	using NUnit.Framework;
	using Mapbox.Utils;


	[TestFixture]
	internal class CircularBufferTest
	{

		[Test]
		public void SettingAndGettingValues()
		{
			CircularBuffer<int> cb = new CircularBuffer<int>(5);

			Assert.AreEqual(0, cb.Count, "initializes to 0");

			cb.Add(10);
			Assert.AreEqual(1, cb.Count, "added one value. count == 3");

			cb.Add(20);
			cb.Add(30);
			Assert.AreEqual(3, cb.Count, "added three values. count == 3");
			// newest value is at [0], oldest at cb[cb.Count-1]
			Assert.AreEqual(30, cb[0], "circularbuffer[0] == 30");
			Assert.AreEqual(10, cb[cb.Count - 1], "circularbuffer[cb.Count-1] == 10");

			cb.Add(40);
			cb.Add(50);
			Assert.AreEqual(5, cb.Count, "added five values. count == 5");

			cb.Add(60);
			Assert.AreEqual(5, cb.Count, "added six values. count == 5");
			Assert.AreEqual(60, cb[0], "circularbuffer[0] == 60");
			Assert.AreEqual(20, cb[cb.Count - 1], "circularbuffer[cb.Count-1] == 20");

			cb.Add(70);
			cb.Add(80);


			// test getting values via different methods

			int[] expected = new int[] { 80, 70, 60, 50, 40 };

			// test values via indexer
			for (int i = 0; i < cb.Count; i++)
			{
				Assert.AreEqual(expected[i], cb[i], "indexer returned correct value");
			}

			// test values via Enumerator
			int[] actual = new int[cb.Count];
			int idx = 0;
			IEnumerator<int> enumerator = cb.GetEnumerator();
			while (enumerator.MoveNext())
			{
				actual[idx] = enumerator.Current;
				idx++;
			}
			Assert.AreEqual(expected, actual, "IEnumerator returned correct sequence");

			// test values via Enumerable
			actual = cb.GetEnumerable().ToArray();
			Assert.AreEqual(expected, actual, "IEnumerable returned correct sequence");
		}


	}
}

