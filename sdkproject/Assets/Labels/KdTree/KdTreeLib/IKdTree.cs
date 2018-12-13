using System.Collections.Generic;

namespace KdTree
{
    public interface IKdTree<TKey, TValue> : IEnumerable<KdTreeNode<TKey, TValue>>
	{
		bool Add(TKey[] point, TValue value);

		bool TryFindValueAt(TKey[] point);

		TValue FindValueAt(TKey[] point);

		TKey[] TryFindValue(TValue value);

		TKey[] FindValue(TValue value);

		KdTreeNode<TKey, TValue>[] RadialSearch(TKey[] center, TKey radius, int count);

		void RemoveAt(TKey[] point);

		void Clear();

		KdTreeNode<TKey, TValue>[] GetNearestNeighbours(TKey[] point, int count = int.MaxValue);
		
		int Count { get; }
	}
}
