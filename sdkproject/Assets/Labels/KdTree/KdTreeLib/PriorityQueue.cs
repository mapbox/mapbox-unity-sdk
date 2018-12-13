using System;

struct ItemPriority<TItem, TPriority>
{
	public TItem Item;
	public TPriority Priority;
}

namespace KdTree
{
    public class PriorityQueue<TItem, TPriority> : IPriorityQueue<TItem, TPriority>
	{
		public PriorityQueue(int capacity, ITypeMath<TPriority> priorityMath)
		{
			if (capacity <= 0)
				throw new ArgumentException("Capacity must be greater than zero");

			this.capacity = capacity;
			queue = new ItemPriority<TItem, TPriority>[capacity];

			this.priorityMath = priorityMath;
		}
		
		///<remarks>
		///This constructor will use a default capacity of 4.
		///</remarks>
		public PriorityQueue(ITypeMath<TPriority> priorityMath)
		{
			this.capacity = 4;
			queue = new ItemPriority<TItem, TPriority>[capacity];

			this.priorityMath = priorityMath;
		}

		private ITypeMath<TPriority> priorityMath;

		private ItemPriority<TItem, TPriority>[] queue;

		private int capacity;

		private int count;
		public int Count { get { return count; } }

		// Try to avoid unnecessary slow memory reallocations by creating your queue with an ample capacity
		private void ExpandCapacity()
		{
			// Double our capacity
			capacity *= 2;

			// Create a new queue
			var newQueue = new ItemPriority<TItem, TPriority>[capacity];

			// Copy the contents of the original queue to the new one
			Array.Copy(queue, newQueue, queue.Length);

			// Copy the new queue over the original one
			queue = newQueue;
		}

		public void Enqueue(TItem item, TPriority priority)
		{
			if (++count > capacity)
				ExpandCapacity();

			int newItemIndex = count - 1;

			queue[newItemIndex] = new ItemPriority<TItem, TPriority> { Item = item, Priority = priority };

			ReorderItem(newItemIndex, -1); 
		}

		public TItem Dequeue()
		{
			TItem item = queue[0].Item;

			queue[0].Item = default(TItem);
			queue[0].Priority = priorityMath.MinValue;

			ReorderItem(0, 1);

			count--;

			return item;
		}

		private void ReorderItem(int index, int direction)
		{
			if ((direction != -1) && (direction != 1))
				throw new ArgumentException("Invalid Direction");

			var item = queue[index];

			int nextIndex = index + direction;

			while ((nextIndex >= 0) && (nextIndex < count))
			{
				var next = queue[nextIndex];

				int compare = priorityMath.Compare(item.Priority, next.Priority);

				// If we're moving up and our priority is higher than the next priority then swap
				// Or if we're moving down and our priority is lower than the next priority then swap
				if (
					((direction == -1) && (compare > 0))
					||
					((direction == 1) && (compare < 0))
					)
				{
					queue[index] = next;
					queue[nextIndex] = item;

					index += direction;
					nextIndex += direction;
				}
				else
					break;
			}
		}

		public TItem GetHighest()
		{
			if (count == 0)
				throw new Exception("Queue is empty");
			else
				return queue[0].Item;
		}

		public TPriority GetHighestPriority()
		{
			if (count == 0)
				throw new Exception("Queue is empty");
			else
				return queue[0].Priority;
		}
	}
}
