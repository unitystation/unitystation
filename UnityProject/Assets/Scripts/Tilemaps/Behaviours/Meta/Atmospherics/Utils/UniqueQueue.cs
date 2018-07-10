using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Tilemaps.Behaviours.Meta
{
	public class UniqueQueue<T> : IEnumerable<T>
	{
		private HashSet<T> hashSet;
		private ConcurrentQueue<T> queue;

		public UniqueQueue()
		{
			hashSet = new HashSet<T>();
			queue = new ConcurrentQueue<T>();
		}

		public int Count
		{
			get { return hashSet.Count; }
		}

		public bool Contains(T item)
		{
			return hashSet.Contains(item);
		}

		public void Enqueue(T item)
		{
			if (hashSet.Add(item))
			{
				queue.Enqueue(item);
			}
		}

		public bool TryDequeue(out T item)
		{
			if (queue.TryDequeue(out item))
			{
				hashSet.Remove(item);
				return true;
			}
			return false;
		}

		public T Peek()
		{
			T item;
			queue.TryPeek(out item);
			return item;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return queue.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return queue.GetEnumerator();
		}

		public bool IsEmpty => queue.IsEmpty;
	}
}