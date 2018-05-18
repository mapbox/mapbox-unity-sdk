using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mapbox.Utils
{


	public interface ICircularBuffer<T>
	{
		int Count { get; }
		void Add(T item);
		T this[int index] { get; }
	}



	/// <summary>
	/// http://geekswithblogs.net/blackrob/archive/2014/09/01/circular-buffer-in-c.aspx
	/// https://social.msdn.microsoft.com/Forums/vstudio/en-US/416a2175-b05d-43b1-b99a-a01c56550dbe/circular-buffer-in-net?forum=netfxbcl
	/// https://en.wikipedia.org/wiki/Circular_buffer
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CircularBuffer<T> : ICircularBuffer<T>, IEnumerable<T>

	{
		private T[] _buffer;
		private int _head;
		private int _tail;


		public CircularBuffer(int capacity)
		{
			if (capacity < 0) { throw new ArgumentOutOfRangeException("capacity", "must be positive"); }
			_buffer = new T[capacity];
			_head = 0;
		}


		public int Count { get; private set; }


		public void Add(T item)
		{
			_head = (_head + 1) % _buffer.Length;
			_buffer[_head] = item;
			if (Count == _buffer.Length)
			{
				_tail = (_tail + 1) % _buffer.Length;
			}
			else
			{
				++Count;
			}
		}


		/// <summary>
		/// <para>ATTENTION!!! order is flipped like in rolling window</para>
		/// <para>[0] is newest value</para>
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index]
		{
			get
			{
				if (index < 0 || index >= _buffer.Length) { throw new ArgumentOutOfRangeException("index: " + index.ToString()); }

				return _buffer[mod((_head - index), _buffer.Length)];
			}
		}


		private int mod(int x, int m) // x mod m works for both positive and negative x (unlike x % m).
		{
			return (x % m + m) % m;
		}

		public IEnumerator<T> GetEnumerator()
		{
			if (Count == 0 || _buffer.Length == 0)
			{
				yield break;
			}

			for (var i = 0; i < Count; ++i) { yield return this[i]; }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		public IEnumerable<T> GetEnumerable()
		{
			IEnumerator<T> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
		}


	}
}
