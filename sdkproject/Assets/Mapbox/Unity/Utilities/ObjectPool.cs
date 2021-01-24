namespace Mapbox.Unity.MeshGeneration.Data
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public class ObjectPool<T>
	{
		private Queue<T> _objects;
		private Func<T> _objectGenerator;

		public ObjectPool(Func<T> objectGenerator, int initialItemCount = 0)
		{
			if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
			_objects = new Queue<T>();
			_objectGenerator = objectGenerator;

			for (int i = 0; i < initialItemCount; i++)
			{
				_objects.Enqueue(_objectGenerator());
			}
		}

		public T GetObject()
		{
			if (_objects.Count > 0)
				return _objects.Dequeue();
			return _objectGenerator();
		}

		public void Put(T item)
		{
			_objects.Enqueue(item);
		}

		public void Clear()
		{
			_objects.Clear();
		}

		public IEnumerable<T> GetQueue()
		{
			return _objects;
		}
	}
}

