namespace KdTree
{
    public interface ITypeMath<T>
	{
		int Compare(T a, T b);

		T MinValue { get; }

		T MaxValue { get; }

		T Min(T a, T b);

		T Max(T a, T b);

		bool AreEqual(T a, T b);

		bool AreEqual(T[] a, T[] b);

		T Add(T a, T b);

		T Subtract(T a, T b);

		T Multiply(T a, T b);

		T Zero { get; }

		T NegativeInfinity { get; }

		T PositiveInfinity { get; }

		T DistanceSquaredBetweenPoints(T[] a, T[] b);
	}
}
