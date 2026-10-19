using Mapbox.BaseModule.Data.DataFetchers;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    /// <summary>
    /// Tests use unique sizes per test method to avoid cross-test contamination from
    /// the pool's static state.
    /// </summary>
    public class ElevationArrayPoolTests
    {
        [Test]
        public void Rent_AllocatesNewArrayOfRequestedSize_WhenPoolIsEmpty()
        {
            var array = ElevationArrayPool.Rent(1001);

            Assert.IsNotNull(array);
            Assert.AreEqual(1001, array.Length);
        }

        [Test]
        public void ReturnThenRent_ReusesSameBuffer()
        {
            var first = ElevationArrayPool.Rent(1002);
            ElevationArrayPool.Return(first);

            var second = ElevationArrayPool.Rent(1002);

            Assert.AreSame(first, second, "Pool should hand back the same buffer it just received.");
        }

        [Test]
        public void Rent_DifferentSizes_ReturnsDistinctBuffers()
        {
            var a = ElevationArrayPool.Rent(1003);
            var b = ElevationArrayPool.Rent(1004);

            Assert.AreNotSame(a, b);
            Assert.AreEqual(1003, a.Length);
            Assert.AreEqual(1004, b.Length);
        }

        [Test]
        public void Return_NullArray_IsNoOp()
        {
            // Should not throw.
            Assert.DoesNotThrow(() => ElevationArrayPool.Return(null));
        }

        [Test]
        public void Pool_CapsAtMaxPerSizeDepth_DroppingExcessToGC()
        {
            // MaxPerSizeDepth is 32 (private const). Push 40 distinct buffers of the
            // same size and confirm at most 32 distinct buffers (by reference identity)
            // can ever be rented back. The remaining 8 must have been dropped to GC.
            const int size = 1005;
            const int pushCount = 40;

            var pushedSet = new System.Collections.Generic.HashSet<float[]>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < pushCount; i++)
            {
                var arr = new float[size];
                pushedSet.Add(arr);
                ElevationArrayPool.Return(arr);
            }

            // Drain the pool: keep renting until we get something that isn't in the
            // original pushed set (i.e. a freshly allocated array, meaning the pool
            // was empty for this size). Count distinct pushed buffers seen.
            var seen = new System.Collections.Generic.HashSet<float[]>(ReferenceEqualityComparer.Instance);
            // Bounded well above the pool's documented cap of 32: the loop ends when Rent hands back
            // something it was not given, and if that never happens the assertions below are the
            // wrong way to learn it.
            var drainGuard = 0;
            while (true)
            {
                Assert.Less(++drainGuard, 1000, "Pool never drained; Rent kept returning pushed buffers.");
                var rented = ElevationArrayPool.Rent(size);
                if (!pushedSet.Contains(rented))
                {
                    break; // freshly allocated → pool is drained
                }
                seen.Add(rented);
            }

            Assert.LessOrEqual(seen.Count, 32, "Pool retained more buffers than its documented cap.");
            Assert.Greater(seen.Count, 0, "Pool should have retained at least one buffer for reuse.");
        }

        // Reference-equality comparer for HashSet<float[]> — default equality
        // compares contents element-wise, which isn't what we want.
        private sealed class ReferenceEqualityComparer : System.Collections.Generic.IEqualityComparer<float[]>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public bool Equals(float[] x, float[] y) => ReferenceEquals(x, y);
            public int GetHashCode(float[] obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
