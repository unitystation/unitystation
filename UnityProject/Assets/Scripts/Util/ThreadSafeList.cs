using System.Collections.Generic;

public delegate void GenericDelegate<in T>(T item);

public class ThreadSafeList<T>
{
	private List<T> list = new List<T>();
	public event GenericDelegate<T> Event;

	public void Iterate(GenericDelegate<T> thingToCall)
	{
		lock (list)
		{
			Event += thingToCall;
			for (int i = list.Count -1; i >= 0; i--)
			{
				Event(list[i]);
			}
			Event -= thingToCall;
		}
	}

	public void Add(T item)
	{
		lock (list)
		{
			list.Add(item);
		}
	}

	public bool Remove(T item)
	{
		lock (list)
		{
			return list.Remove(item);
		}
	}

	public void AddIfMissing(T item)
	{
		lock (list)
		{
			if (list.Contains(item) == false)
			{
				list.Add(item);
			}
		}
	}

	public void Clear()
	{
		lock (list)
		{
			list.Clear();
		}
	}


}