using System.Collections;
using System.Collections.Generic;

namespace Collections.Hybrid.Generic
{
	/// <summary>
	/// LinkedList/Dictionary combo for constant time add/remove/contains. Memory usage is higher as expected.
	/// -kazoo
	/// </summary>
	/// <typeparam name="TK">key value type. It is recomended that this be "int", for speed purposes.</typeparam>
	/// <typeparam name="TV">The value type. Can be anything you like.</typeparam>
	public class LinkedListDictionary<TK, TV>
	{
		private readonly Dictionary<TK, LLEntry> dictionary = new Dictionary<TK, LLEntry>();
		private readonly LinkedList<TV> list = new LinkedList<TV>();

		/// <summary>
		/// Get The count.
		/// </summary>
		/// <returns></returns>
		public int Count
		{
			get { return list.Count; }
		}

		/// <summary>
		/// Is the key in the dictionary?
		/// </summary>
		/// <param name="k">key</param>
		/// <returns>true if key is present, false otherwise.</returns>
		public bool ContainsKey(TK k)
		{
			return dictionary.ContainsKey(k);
		}

		/// <summary>
		/// Remove a key/value from the dictionary if present.
		/// </summary>
		/// <param name="k">key</param>
		/// <returns>True if removal worked. False if removal is not possible.</returns>
		public bool Remove(TK k)
		{
			if (!ContainsKey(k))
			{
				return false;
			}

			LLEntry entry = dictionary[k];
			list.Remove(entry.vNode);
			return dictionary.Remove(k);
		}

		/// <summary>
		/// Add an item. Replacement is allowed.
		/// </summary>
		/// <param name="k">key</param>
		/// <param name="v">value</param>
		public void Add(TK k, TV v)
		{
			Remove(k);
			dictionary[k] = new LLEntry(v, list.AddLast(v));
		}

		/// <summary>
		/// Retrieve an element by key.
		/// </summary>
		/// <param name="k">key</param>
		/// <returns>Value. If element is not present, default(V) will be returned.</returns>
		public TV GetValue(TK k)
		{
			if (ContainsKey(k))
			{
				return dictionary[k].v;
			}
			return default(TV);
		}

		public TV this[TK k]
		{
			get { return GetValue(k); }
			set { Add(k, value); }
		}

		/// <summary>
		/// Raw list of Values for garbage-free iteration. Do not modify.
		/// </summary>
		/// <value>The values</value>
		public LinkedList<TV> Values
		{
			get
			{
				return list;
			}
		}

		public void Clear()
		{
			dictionary.Clear();
			list.Clear();
		}

		private struct LLEntry
		{
			public readonly TV v;
			public readonly LinkedListNode<TV> vNode;

			public LLEntry(TV v, LinkedListNode<TV> vNode)
			{
				this.v = v;
				this.vNode = vNode;
			}
		}
	}
}
