using System;
using System.Collections.Generic;

namespace PathFinding
{
	public class PriorityQueue<T> where T : IComparable<T>
	{
		private List<T> data;

		public int Count { get { return data.Count; } }

		public PriorityQueue()
		{
			this.data = new List<T>();
		}

		public void Enqueue(T item)
		{
			data.Add(item);

			int childindex = data.Count - 1;

			while (childindex > 0)
			{
				int parentindex = (childindex - 1) / 2;

				if (data[childindex].CompareTo(data[parentindex]) >= 0)
				{
					break;
				}

				T tmp = data[childindex];
				data[childindex] = data[parentindex];
				data[parentindex] = tmp;

				childindex = parentindex;
			}
		}

		public T Dequeue()
		{
			int lastindex = data.Count - 1;

			T frontItem = data[0];

			data[0] = data[lastindex];

			data.RemoveAt(lastindex);

			lastindex--;

			int parentindex = 0;

			while (true)
			{
				int childindex = parentindex * 2 + 1;

				if (childindex > lastindex)
				{
					break;
				}

				int rightchild = childindex + 1;

				if (rightchild <= lastindex && data[rightchild].CompareTo(data[childindex]) < 0)
				{
					childindex = rightchild;
				}

				if (data[parentindex].CompareTo(data[childindex]) <= 0)
				{
					break;
				}

				T tmp = data[parentindex];
				data[parentindex] = data[childindex];
				data[childindex] = tmp;

				parentindex = childindex;
			}

			return frontItem;
		}

		public bool Remove(T item) {
			int parentindex = data.IndexOf(item);
			if (parentindex == -1)
				return false;

			int lastindex = data.Count - 1;

			data[parentindex] = data[lastindex];

			data.RemoveAt(lastindex);

			lastindex--;

			while (true) {
				int childindex = parentindex * 2 + 1;

				if (childindex > lastindex) {
					break;
				}

				int rightchild = childindex + 1;

				if (rightchild <= lastindex && data[rightchild].CompareTo(data[childindex]) < 0) {
					childindex = rightchild;
				}

				if (data[parentindex].CompareTo(data[childindex]) <= 0) {
					break;
				}

				T tmp = data[parentindex];
				data[parentindex] = data[childindex];
				data[childindex] = tmp;

				parentindex = childindex;
			}

			return true;
		}

		public T Peek()
		{
			T frontItem = data[0];
			return frontItem;
		}

		public bool Contains(T item)
		{
			return data.Contains(item);
		}

		public List<T> ToList()
		{
			return data;
		}
	}
}
