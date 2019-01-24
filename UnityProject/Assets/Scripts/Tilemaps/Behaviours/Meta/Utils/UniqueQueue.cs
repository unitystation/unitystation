using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

		public int Count => queue.Count;

		public bool IsEmpty => queue.IsEmpty;

		public bool Contains(T item)
		{
			lock (hashSet)
			{
				return hashSet.Contains(item);
			}
		}

		public void Enqueue(T item)
		{
			lock (hashSet)
			{
				if (hashSet.Add(item))
				{
					queue.Enqueue(item);
				}
			}
		}

		public void EnqueueAll(List<T> elements)
		{
			for (int i = 0; i < elements.Count; i++)
			{
				Enqueue(elements[i]);
			}
		}

		public bool TryDequeue(out T item)
		{
			if (queue.TryDequeue(out item))
			{
				lock (hashSet)
				{
					hashSet.Remove(item);
				}

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
	}
}